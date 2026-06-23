# The Tools page
[&lt; Docs index](index.md)

The SDK ships a built-in **Tools page** at `/.tools` — a container for developer and operations tools that run inside your live application, with access to its real services and caches. Today it hosts the [Cache inspection tool](tools-cache.md), the [GraphQL Viewer](tools-graphql.md), the [Edge Webhooks](tools-webhooks.md) manager and the [Edge Settings](tools-edge-settings.md) editor; more tools will be added here over time.

## Access

The Tools page is **not intended to be public-facing**. Guard it by [IP and path access control](ip-path-security.md), e.g. restrict `/.tools` to loopback (`127.0.0.1`, `::1`), so it works on `localhost` **during development** and returns `404` to everyone else. Grant access to other machines, or open it up, by editing the `/.tools` rule in `SitecoreSettings.PathAccessControl` — see [IP and path access control](ip-path-security.md).

Because that check uses the forwarded client address, the page stays protected even when the application runs behind a reverse proxy.

> [!CAUTION]
> When deployed to Azure, reverse proxies and CDNs can interfere with the correct tracking of request IPs. Be sure to thoroughly test IP-based access controls and only enable the narrowest possible range. IP address headers may be spoofed by determined attackers.

> [!TIP]
> Tools can be globally disabled by removing all allowed IP addresses, or made read-only using the settings below. Use `DeniedStatusCode` of 404 to hide the presence of the page.

## Settings

The `SitecoreSettings.Tools` section configures the Tools page:

| Property | Type | Default | Description |
| - | - | - | - |
| `TrackCacheStatistics` | bool | `true` | When `true`, the caching layer records a lightweight key + timestamp index so the Cache tool can list in-memory items. Set `false` on memory-sensitive hosts to switch the tracking off entirely. See [Cache inspection](tools-cache.md). |
| `CacheToolsReadOnly` | bool | `false` | When `true`, the Tools page is view-only: every destructive action (item **Delete**, cache **Clear**, **Clear log**) is hidden and disabled. Use this in shared or production-adjacent environments where you want visibility without the ability to mutate the cache. |

> [!NOTE]
> `CacheToolsReadOnly` is enforced both in the UI (the buttons don't render) and in the service layer (the operations no-op), so it can't be bypassed by crafting a request directly.

## How tools are structured

Each tool is a **tab** on the page. The page itself owns no tab list — it asks the DI container for every registered `IToolsTab`, groups them by their `Group` in the side nav, and renders the selected tab's component in the body. Adding a tool is therefore just registering one more `IToolsTab`; nothing in the page changes. The current line-up:

| Tool | Status | What it does |
| - | - | - |
| [Cache](tools-cache.md) | Available | Inspect, delete and clear the memory / disk cache; review the cache-clear event log. |
| [GraphQL](tools-graphql.md) | Available | Inspect the raw Edge layout JSON for any page, with a placeholder / component / item tree to navigate it. |
| [Edge Webhooks](tools-webhooks.md) | Available | List, inspect and remove the publish webhooks registered against the Sitecore environment. |
| [Edge Settings](tools-edge-settings.md) | Available | View and update the Edge tenant's cache settings; clear the tenant cache. |
| Design Studio | Planned | _placeholder_ |

## Writing your own tab

The Tools page is **pluggable**: you can add your own tabs from your host application, or from any library it references, without touching the SDK. A tab is any Blazor component that also advertises a small bit of metadata via the `IToolsTab` interface.

**1. Build the tab component.** It's an ordinary `ComponentBase` / `.razor` component — render whatever you like, inject whatever services you need. Have it implement the strongly-typed `IToolsTab<TComponent>` (where `TComponent` is the component itself); that interface supplies `TabComponent` for you, so you only declare the nav metadata:

```razor
@implements IToolsTab<DiagnosticsTab>

<h3>Diagnostics</h3>
@* …your tool UI… *@

@code {

    public string Group => "Tools";              // side-nav group heading
    public string Title => "Diagnostics";        // nav label
    public string Icon => AllIcons.IconSvg.DatabaseOutline; // nav icon (a Blok IconSvg member, or any SVG path)
    public string? ActiveIcon => AllIcons.IconSvg.Database;  // optional filled variant while active; falls back to Icon
}
```

| Member | Required | Purpose |
| - | - | - |
| `Group` | yes | Side-nav group the tab is listed under (e.g. `"Tools"`, `"Design"`). Tabs sharing a group are grouped together. |
| `Title` | yes | The tab's label in the nav. |
| `Icon` | yes | SVG path for the nav icon — typically a member of the Blok `AllIcons.IconSvg` class, but any SVG path string works. |
| `ActiveIcon` | no | Alternative icon shown while the tab is selected. Defaults to `Icon`. |
| `TabComponent` | — | Supplied automatically by `IToolsTab<TComponent>`; the component rendered in the body. |

> [!TIP]
> The Tools page layout already includes the wiring for the [Blazor BlokUI kit](https://blok-blazor-catalogue.ping-works.com.au/), so for visual consistency you might consider using its components in your own tabs as well. The layout also mounts Blok's `Popovers` and `Toaster` hosts, so components that portal an overlay (`Select`, `DatePicker`) and toast notifications work in your tabs without extra setup. The wiring for the [TTT.AspNet.Blazor.JsonViewer](https://github.com/Texnomic/JsonViewer) package is included too, so its `JsonViewer` component is available out of the box.

**2. Register it in DI.** Add it as an `IToolsTab` anywhere in your service configuration (after `AddSitecoreBlazorSDK`):

```csharp
builder.Services.AddSingleton<IToolsTab, DiagnosticsTab>();
```

It now appears on `/.tools` automatically, in nav order of registration, grouped by `Group`. Inactive tabs stay rendered but hidden, so each tab keeps its state as you switch between them.

> [!NOTE]
> The built-in tabs (Cache, GraphQL, Edge Webhooks and Edge Settings, plus the planned Design Studio placeholder) are registered by the SDK exactly this way — there's no privileged code path. Your tabs are first-class citizens alongside them.

## Related

- [Cache inspection](tools-cache.md) — the first tool.
- [GraphQL Viewer](tools-graphql.md) — inspect the raw layout JSON for any page.
- [Edge Webhooks](tools-webhooks.md) — list and remove publish webhooks.
- [Edge Settings](tools-edge-settings.md) — view/update Edge tenant cache settings.
- [IP and path access control](ip-path-security.md) — how `/.tools` is protected.
- [Configuration settings](settings.md) — the full settings reference.

## Next steps

Continue with [Cache inspection](tools-cache.md).
