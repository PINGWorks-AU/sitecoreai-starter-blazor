# Caching and content sync
[&lt; Docs index](index.md)

A headless Sitecore site fetches its content from Sitecore Experience Edge over GraphQL. Hitting Edge for every request would be slow and would couple your site's availability to Edge's, and risks hitting the rate limiter when your site is under load. The Blazor SDK keeps a local cache of the content it fetches, and listens for publishing notifications so it knows when that cache is stale. This page explains the model so the knobs in [Configuration settings](settings.md) make sense.

## The two-layer cache

The SDK caches layout responses in two layers:

- **Memory** - the fastest layer, held in the application's `IMemoryCache`. Entries expire after `SyncSettings.MemoryCacheTimeout` (default one hour).
- **Disk** - a durable layer under `SyncSettings.DataPath` (default `./App_Data`). Entries expire after `SyncSettings.StorageCacheTimeout` (default one day) and can be compressed with `StoreCompressed`.

`SyncSettings.CachingMode` chooses how the layers are used:

| Mode | Behaviour |
| - | - |
| `MemoryAndDisk` | Both layers active. The default and the right choice for production - the disk layer means a restarted or scaled-out instance warms up from disk rather than hammering Edge. |
| `MemoryOnly` | In-memory cache only; nothing is written to disk. |
| `Disabled` | No caching. Every request goes to Edge. Useful while debugging content issues, not for production. |

## Warm on startup: preload

When the application starts, the SDK's content sync engine runs before the site begins serving pages. It:

1. Loads the **site map** - the list of routes, the dictionary (translation) entries, redirects, robots and sitemap data, and the configured error-page paths - for every language in your effective language set (`DefaultLanguage` + `AllowedLanguages`).
2. Kicks off a background preload of the pages listed in `SyncSettings.PreloadPaths`, plus the site's 404 and 500 error pages, so they are cached before the first visitor arrives.

`PreloadPaths` accepts exact paths (`/`, `/about`) and simple prefix wildcards (`/products/*`). The engine exposes a `LoadingTask` that the page renderer awaits, so the first request is held briefly until the site map is ready rather than rendering against empty data.

The site map load is the reason `AllowedLanguages` matters for performance: each language is loaded and preloaded independently, so listing only the languages you actually serve keeps startup lean.

Until the site map and initial sync are loaded, the readiness endpoint (`/api/health/ready`) returns `503`, so a container platform can keep a starting instance out of rotation. See [Health and readiness](health.md).

## Redirects, robots.txt and sitemap.xml

Redirects and robots data come from the site map, so they are cached with it and refresh whenever the site map is rebuilt after a publish. The redirect lookup for each site is built once per site map load. `/sitemap.xml` is fetched from Edge on demand and held in memory for 5 minutes.

## Uncached page requests

Only paths that are routes of the site reach Experience Edge; any other path is rejected with a single lookup. Pages not in the cache are fetched with at most `SyncSettings.MaxConcurrentLayoutRequests` (default `8`) Edge requests at once. A request that waits longer than `SyncSettings.LayoutRequestQueueTimeout` (default 10 seconds) for a slot is served as not found.

When Edge returns no layout for a route or item id, the miss is remembered for 2 minutes (up to 10,000 misses) and forgotten on the next cache clear, so repeat requests for it don't reach Edge.

## Staying fresh: the publish webhook

Content changes in Sitecore are published to Experience Edge. To learn about those publishes, the SDK registers a **webhook** with Edge and exposes a receiver endpoint (default `/api/webhooks/publish`). The flow is:

1. An editor publishes in Sitecore; Edge calls the SDK's webhook receiver.
2. The receiver asks the cache evictor to clear.
3. The evictor **debounces** - a burst of publish notifications (common during a large publish) is collapsed into a single clear after `SyncSettings.ChangeFeed.CacheClearDebounceSecs` of quiet (default 30 seconds).
4. When the debounce window elapses, the evictor clears the disk cache, rebuilds the site map from Edge, clears the memory cache, and raises an `OnCacheCleared` event.

### Registering the webhook automatically

If `SyncSettings.ChangeFeed.AutoCreateEdgeWebhook` is `true`, the SDK uses the `ChangeFeed.ClientId` / `ClientSecret` credentials to register the webhook with Edge on startup, pointing it at `ChangeFeed.WebhookReceiverBase` + the publish path. On startup it first removes every existing webhook that targets that same receiver URL, then creates one, so duplicates can't accumulate - including webhooks left behind by instances that have since been recycled. Each webhook is labelled from `ChangeFeed.UniqueId` (or the machine name when that's not set) for identification; environments that share one Edge tenant stay distinct because each has its own `WebhookReceiverBase`.

Set `AutoCreateEdgeWebhook` to `false` if you'd rather manage webhook registration yourself (for example through infrastructure-as-code).

> [!NOTE]
> `ChangeFeed.WebhookReceiverBase` must be a URL Edge can actually reach. In local development this means a public tunnel (a dev tunnel URL works well); in production it's your site's public base URL.

### Securing the receiver

Set `ChangeFeed.WebhookApiKey` to require an `x-apikey` header on incoming publish notifications. When `AutoCreateEdgeWebhook` is on, the SDK configures Edge to send this header, so the two stay in step automatically. If `AutoCreateEdgeWebhook` is on and no key is set, the SDK generates a random key at startup and registers the webhook with it.

## Multi-instance cache coordination

Behind a load balancer, a publish webhook reaches only the one instance the balancer routes it to. That instance clears its cache, but the others would keep serving stale in-memory content until their `MemoryCacheTimeout` elapses. `SyncSettings.CrossInstance` closes that gap with a small signal file on a volume every instance shares (on Linux App Service `/home` is already an Azure Files share mounted across all instances):

1. The instance that handles a clear - from the publish webhook, the cache-reset endpoint, or the Tools page - rebuilds its cache, stamps the new token into its own `DataPath`, and writes the same token to the signal file in `CrossInstance.SharedPath`.
2. Every instance polls that file every `CrossInstance.PollInterval` (default 10 seconds).
3. When an instance sees a token it hasn't seen before it refreshes, recorded with a `CrossInstance` source in the cache-clear log, and does not re-write the signal, so it doesn't bounce between instances. How it refreshes adapts automatically: if the token stamped in the instance's own `DataPath` already matches the signal, that `DataPath` is the same shared volume a peer has already rebuilt, so the instance only drops its in-memory caches and reloads the rebuilt site map from it - no Edge queries. If the tokens differ, the instance's cache does not hold the publish (a local `DataPath`), so it rebuilds from Edge. On a large site, where the route list is many GraphQL requests, only one instance rebuilds from Edge per publish when the cache is shared.

```json
{
  "SyncSettings": {
    "CrossInstance": {
      "SharedPath": "/home/site/wwwroot/appdata/Sync",
      "PollInterval": "00:00:10"
    }
  }
}
```

Only the signal and the generation token are written to coordinate; the cache itself is written atomically (a temp file renamed into place) and its clear tolerates files other instances hold open, so a cache shared between instances never yields a torn read. On startup an instance adopts the current token without clearing, since it has just loaded fresh content. Coordination is opt-in: omit the `CrossInstance` section, or set `PollInterval` to `00:00:00`, to leave each instance independent.

`SharedPath` is set per environment - for example `/home/site/wwwroot/appdata/Sync` on Linux App Service and `c:\home\appdata\Sync` on Windows. Give each app its own `SignalFileName` when several share one storage account. See [Configuration settings](settings.md#syncsettingscrossinstance) for every property.

## Reacting to a cache clear in your own code

If your application caches its own data derived from Sitecore (for example, navigation), subscribe to the evictor so you rebuild when content is republished:

```csharp
public SitecoreData( ISitecoreGraphQL scGql, ICacheEvictor cacheEvictor /* … */ )
{
    // ...
    cacheEvictor.OnCacheCleared += () => _ = Initialise();
}
```

Inject `ICacheEvictor` and hook `OnCacheCleared`. Your handler runs after the SDK has rebuilt its own site map, so re-querying Edge inside it returns fresh content.

## Clearing the cache on demand

For cases where you need to force a refresh without a publish (a deployment, a manual fix), the SDK exposes a remote cache-reset endpoint (default `/api/cache-reset`, `POST`). Protect it by setting `SitecoreSettings.CacheResetApiKey`; callers must then present that value in the `x-apikey` header. If you leave the key unset the endpoint accepts all callers, so set it in any environment where the endpoint is reachable.

You can also clear the cache — and inspect what's in it — interactively from the Cache inspection tool on the built-in Tools page (at `/.tools`). Every clear (publish, API call, or Tools-page reset) is recorded in that tool's event log.

## Extending the cache

The storage layers sit behind interfaces - `ICacheStorageAdapter` for layout responses and `ISitesStorageAdapter` for the site map - and eviction behind `ICacheEvictor`. The defaults (file-backed storage, a global evictor) suit most sites, but you can register your own implementations in DI if you need a different store, for example a distributed cache.

## Next steps

Continue with [Analytics and tracking](analytics-tracking.md).
