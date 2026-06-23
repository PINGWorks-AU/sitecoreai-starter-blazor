# GraphQL Viewer
[&lt; Docs index](index.md)

The **GraphQL** tool on the [Tools page](tools.md) lets you inspect the raw layout response Sitecore Experience Edge returns for any page on your site. You pick a page, click **Load**, and the tool fetches the verbatim GraphQL JSON and presents it two ways at once: a navigable **component hierarchy** on the left and the **raw JSON** on the right. It's the in-app, live-data equivalent of running the layout query by hand — useful for confirming what content, placeholders, datasources and fields a page actually delivers.

The tool is **read-only**. It calls the same `IPageProvider.GetPageRaw` the SDK uses internally, so what you see is exactly what the rendering pipeline sees.

## Choosing a page

A control bar across the top selects the page to load:

- **Host** — the site hostname. Hidden when only one host is configured.
- **Language** — the content language. Hidden when only one language is configured.
- **Page path** — an autocomplete sourced from the resolved site's routes (e.g. `/`, `/about`). You can type to filter or pick from the list.
- **Load** — fetches the layout JSON for the chosen host / language / path.

Only paths that are **known routes** for the selected host and language resolve. If a path isn't a known route, the viewer shows a short "No content returned" notice rather than an error.

## Component hierarchy (left)

The left pane is a tree built from the layout JSON — a fast way to navigate a large response:

- **Route** (the root) is labelled with the page's **template name**. Once a page is loaded, the panel heading shows the route's **display name**.
- **Placeholders** list under the route, each showing its component count.
- **Components** list under their placeholder, labelled by `componentName`.
- **Items** — when a component has a `fields.items` array (a multi-item datasource), each item appears as a child labelled by its **display name**, nesting further if items themselves contain item lists.

> [!TIP]
> If a route is gated by an [access policy](page-security.md), its tree icon becomes a **padlock**. Hover it to see `Access policy: <name>` — a quick way to spot which pages are protected.

Clicking a node drives the JSON viewer on the right: it collapses the whole document down to just the path to that node, **highlights** the node's JSON block, scrolls it into view, and expands the node's direct children (plus its `fields` object, if present) for an immediately useful one-level-deep view. From there you're free to expand and collapse as you like.

## Raw JSON (right)

The right pane renders the full response with a syntax-highlighted, collapsible JSON viewer. Its header shows the payload **size** and three actions:

- **Collapse all** / **Expand all** — toggle every node.
- **Copy** — copies the raw JSON to your clipboard; a brief **"Copied"** toast confirms.

## Access

The GraphQL tool is part of the Tools page, so it inherits the same protection: `/.tools` should be restricted to trusted IPs via [IP and path access control](ip-path-security.md). Because the raw layout response can include unpublished or otherwise sensitive field data, keep the page closed to the public.

## Related

- [The Tools page](tools.md) — the container and how `/.tools` is secured.
- [Working with GraphQL](graphql.md) — using GraphQL from your components.
- [Page-level security](page-security.md) — the access policies the padlock surfaces.
- [Cache inspection](tools-cache.md) — the other built-in tool.
