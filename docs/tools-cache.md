# Cache inspection
[&lt; Docs index](index.md)

The **Cache** tool on the [Tools page](tools.md) gives you a live window into the SDK's two-layer cache — what's in memory, what's on disk, how big it is, how old it is — and lets you evict individual items or reset the whole cache. It also keeps a log of every cache-clear event. For the model behind the cache itself, read [Caching and content sync](caching-sync.md) first.

The tool has three tabs: **Usage**, **Data** and **Events**.

## Usage

The Usage tab shows the current cache configuration — the [`SyncSettings.CachingMode`](settings.md) and the memory / disk TTLs — alongside two ring meters:

- **Memory** — the process memory load against the memory available to the host (read from the GC), so you can see how much headroom the container has.
- **Disk** — the size of the on-disk cache directory against the free space on its drive.

A **Refresh** button (top right, next to **Clear**) re-reads everything.

## Data

The Data tab is a master/detail view of what's currently cached, split into **Memory** and **Disk** lists. Each list heading shows a badge with the **item count and total size**.

Each row shows:

- **Key** — the cache key. Page entries are reverse-engineered back to a readable `language|path` (e.g. `en|/about`) from the loaded site map; query-cache entries that can't be resolved keep their numeric key.
- **Size** — bytes stored. Disk sizes come from the file; memory sizes are computed by serialising the cached object.
- **Age / TTL** — memory rows show the time until expiry; disk rows show how long ago they were stored, because disk entries remain readable past their TTL.

Selecting a row loads its contents into the JSON viewer on the right — syntax-highlighted, with collapsible nodes and **Collapse all** / **Expand all** controls. The payload is read **on demand**: the tool never holds a second copy of your cached data.

### Deleting and clearing

- **Delete** — the bin icon on each row evicts that single item.
- **Clear** — the red button (top right, behind a confirmation) performs a full cache reset. This is exactly the same eviction a publish webhook or the `/api/cache-reset` endpoint triggers: it clears the disk cache, rebuilds the site map from Edge, clears memory, and fires `ICacheEvictor.OnCacheCleared`. The confirmation exists because a reset briefly impacts site performance while the cache re-warms.

Both actions are hidden when `SitecoreSettings.Tools.CacheToolsReadOnly` is `true` — see [The Tools page](tools.md).

> [!NOTE]
> The Memory list depends on `SitecoreSettings.Tools.TrackCacheStatistics`. With it off, the SDK keeps no in-memory index, so the Memory list is empty (a notice explains why); the Disk list still works, enumerated on demand from the cache directory.

## Events

Every cache clear and item delete is recorded to an **event log**, so you can see *why* the cache emptied — a publish, a manual reset, an API call. The Events tab renders the log as a table of:

- **Time** — when it happened. A **GMT / Local time** switch toggles between UTC and your browser's local zone (rendered with the Blok `LocalTime` component, which formats in the visitor's time zone rather than the server's).
- **Source** — what triggered it: `Webhook` (a publish notification), `Api` (the cache-reset endpoint), `Tools` (this page), or `External` (a direct `ICacheEvictor` call in your own code).
- **Scope** — whether it cleared all items or a single one.
- **Item key** — recorded for single-item deletes.

The log is persisted to **`{SyncSettings.DataPath}/cache-clear-log.json`** — directly under the data path, *not* the `Cache/` subfolder, so clearing the cache never erases its own history. It retains the 500 most recent events.

A **Clear log** button (gated by `CacheToolsReadOnly`, like the other destructive actions) empties **only the log** — it does not touch the cache.

## Related

- [Caching and content sync](caching-sync.md) — the cache model and how invalidation works.
- [The Tools page](tools.md) — access control and the `Tools` settings.
- [Configuration settings](settings.md) — `SyncSettings` and `SitecoreSettings.Tools`.
