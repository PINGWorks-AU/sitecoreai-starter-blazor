# Page URLs
[&lt; Docs index](index.md)

The **Page URLs** tool on the [Tools page](tools.md) shows the routing data the SDK loaded from Experience Edge: every page URL the site serves and every redirect, per language. Use it to confirm a newly published page or redirect has reached the site, or to find out why a URL returns not found.

## Language

A language selector (top right) lists each language in the effective language set (`DefaultLanguage` + `AllowedLanguages`). The lists below show the data for the selected language.

## Redirects

Each redirect row shows:

- **Pattern → Target** — the source pattern and where it redirects to.
- **Type** — `301`, `302` or `transfer` (a server transfer, which renders the target without changing the address bar).
- **Q** — shown when the query string is preserved.
- **L** — shown when the language is preserved.
- **Locale** — the locale the redirect applies to, or `all`.

## Page URLs

The list of routes the site serves. Each URL opens the page in a new browser tab. Only these paths are sent to Experience Edge; any other path is served as not found without an Edge request (see [Caching and content sync](caching-sync.md#uncached-page-requests)).

## Filtering

Each list has a filter box and a count badge. The filter matches the whole value, case-insensitively: `*` matches any run of characters and `_` matches any single character, so `/blog/*` lists every blog page. The first 50 matches are shown; **Show all** lists the rest.

## Reload from Sitecore

**Reload from Sitecore** (behind a confirmation) re-fetches the site data for every language from Edge, replaces the cached files and reloads the in-memory site map. Page content is not re-warmed. Use it when a redirect or new page has been published but the site map has not yet refreshed.

The button is hidden when `SitecoreSettings.Tools.CacheToolsReadOnly` is `true` — see [The Tools page](tools.md).

## Related

- [Routing](routing.md) — how Sitecore routes reach your components.
- [Caching and content sync](caching-sync.md) — when the site map is loaded and rebuilt.
- [The Tools page](tools.md) — access control and the `Tools` settings.
