# Edge Settings
[&lt; Docs index](index.md)

The **Edge Settings** tool on the [Tools page](tools.md) reads and updates the cache configuration of your Sitecore Experience Edge **tenant** — the server-side counterpart to the SDK's own [local cache](caching-sync.md). From here you can review Edge's content and media cache TTLs, toggle its auto-clear-on-publish behaviour, and clear the whole tenant cache on demand.

It talks to Sitecore through the Experience Edge **Admin SDK**.

## Credentials required

Like the [Edge Webhooks](tools-webhooks.md) tool, this needs Edge **administration credentials** — `SyncSettings.ChangeFeed.ClientId` and `ClientSecret` (set via [User Secrets](settings.md)). Without them the tab shows a **403 “Credentials required”** state instead of the settings form.

## The settings

The tool loads the tenant's current settings into a form:

| Setting | Type | Description |
| - | - | - |
| **Content cache — Time to live** | duration | How long Edge caches layout / item responses (e.g. `01:00:00` for an hour, `1.00:00:00` for a day). |
| **Content cache — Auto-clear on publish** | switch | Whether Edge clears the content cache automatically when a publish completes. |
| **Media cache — Time to live** | duration | How long Edge caches media (image / file) responses. |
| **Media cache — Auto-clear on publish** | switch | Whether Edge clears the media cache automatically when a publish completes. |
| **Auto-clear tenant cache on publish** | switch | Whether the whole tenant cache is cleared on publish. |

## Saving changes

When the Tools page is **not** read-only, a **Save** button commits your edits. It sends only the fields you actually changed, as a JSON Patch, via the Admin SDK — a **toast** confirms success or reports the failure. Save stays disabled until there's a real change, and a time-to-live that isn't a valid duration is flagged inline and blocks the save.

Set `SitecoreSettings.Tools.CacheToolsReadOnly` to `true` to make the tab view-only: the form is shown for reference, but Save and the Clear button are hidden and the fields are disabled. See [The Tools page](tools.md).

## Clear tenant cache

A **Clear tenant cache** button (shown when read-write, behind a confirmation — the same pattern as the [Cache tool](tools-cache.md)) empties the entire Experience Edge cache for the tenant. Edge then rebuilds it on demand, which briefly affects delivery performance, so the action is confirmed first. A toast reports the result.

> [!NOTE]
> This clears the **Edge (server-side) cache** for the whole tenant — distinct from the SDK's local memory/disk cache that the [Cache tool](tools-cache.md) manages for this host.

## Access

The Edge Settings tool is part of the Tools page and inherits its protection — keep `/.tools` restricted to trusted IPs via [IP and path access control](ip-path-security.md).

## Related

- [Caching and content sync](caching-sync.md) — the SDK's own cache, and how publishes drive invalidation.
- [Edge Webhooks](tools-webhooks.md) — the other Admin-SDK-backed tool.
- [The Tools page](tools.md) — the container and how `/.tools` is secured.
- [Configuration settings](settings.md) — the `SyncSettings.ChangeFeed` credentials.
