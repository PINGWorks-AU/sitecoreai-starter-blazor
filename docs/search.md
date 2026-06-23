# Search
[&lt; Docs index](index.md)

Sitecore offers **two distinct search products**, and a site may use either - or both. The Blazor SDK doesn't replace them; instead, two companion NuGet packages give you typed, server-side .NET clients for each, designed to drop into the same Blazor host alongside the rest of the SDK.

| | **Experience Search** | **Sitecore Search** (Discover) |
| - | - | - |
| Package | [`PINGWorks.SitecoreSearch.ExperienceSearch`](https://www.nuget.org/packages/PINGWorks.SitecoreSearch.ExperienceSearch/) | [`PINGWorks.SitecoreSearch.DiscoverSearch`](https://www.nuget.org/packages/PINGWorks.SitecoreSearch.DiscoverSearch/) |
| What it searches | The content already in **your SitecoreAI / Experience Edge** | A **separate, standalone** search product with its own crawler/feeds and console (CEC) |
| Setup | Just your `EdgeContextId` - no extra product | A separate Sitecore Search subscription, customer key and API keys |
| Capabilities | Keyphrase search, paging, sorting for a single Template per source | Search, **recommendations**, **AI Q&A**, **type-ahead suggestions**, **events/analytics**, **ingestion** |
| Best for | Searching your own site's content elements with no extra moving parts | Rich merchandising, personalisation and recommendations |
| Credential | `x-sitecore-contextid` header (the context id) | API key with the appropriate scope (`discover` / `event` / `ingestion`) |


Both packages share a common design: a `sitecore-search.json` file describes your search sources, a build-time **source generator** emits a strongly-typed document record per source, and every call returns an `ApiResponse<T>` you check before reading. The SDK gives you the **typed client and the data layer**; the search-results UI (the list, facets, pager) is yours to build as ordinary Blazor components.

> [!NOTE]
> **What the Starter Kit ships.** The id-resolver below is built into the Blazor SDK and works out of the box. The two search packages are production-ready and live-verified, but the Starter Kit doesn't include pre-built search-results components - see [the roadmap note](#roadmap) at the foot of this page.

## Rendering a result by id

This capability is built into the Blazor SDK itself and works regardless of which search product you use. Search indexes return the **id** of a matching page, but often not its URL. Rather than resolve every URL yourself, link a result straight to:

```
/?pageId={id}
```

The SDK loads that page in a **single** request to Experience Edge, renders it in place, and rewrites the address bar to the page's friendly URL - with **no HTTP redirect**, under both static SSR and interactive rendering. Full route allow-listing and [page-level authorization](page-security.md) still apply. See [Loading a page by id](components.md#loading-a-page-by-id) for the complete behaviour and the rules that govern it.

This means a results list can render a hit with nothing more than its id:

```razor
@foreach ( var hit in Results.Items )
{
    <a href="@($"/?pageId={hit.ItemId}")">@hit.PageTitle</a>
}
```

## Experience Search

Searches the content you already publish through Experience Edge - the same content the Blazor SDK renders. There's no separate product to buy: the only credential is your environment's **context id** (`EdgeContextId`), sent as the `x-sitecore-contextid` header.

**1. Install**

```bash
dotnet add package PINGWorks.SitecoreSearch.ExperienceSearch
```

**2. Configure and register** - bind `ExperienceSearchSdkOptions` and register the client:

```csharp
builder.Services.AddSitecoreExperienceSearchSdk( opt =>
    builder.Configuration.GetSection( "SitecoreSearch:ExperienceSearch" ).Bind( opt ) );
```

```json
{
  "SitecoreSearch": {
    "ExperienceSearch": {
      "EdgeContextId": "[from SitecoreAI Deploy → Developer Settings]"
    }
  }
}
```

**3. Describe your sources** in `sitecore-search.json` (under `experienceSources`). The source generator emits one typed document record per source at build time:

```json
{
  "experienceSources": [
    {
      "id": "8c5e839b-997b-4fdd-8c5a-5e638a4f9bbe",
      "name": "Blogs",
      "fields": [
        { "name": "PageTitle", "type": "string",   "search": true, "retrieve": true },
        { "name": "Date",      "type": "datetime", "sort": true,   "retrieve": true }
      ]
    }
  ]
}
```

**4. Query** - inject `ISitecoreExperienceSearchSdk` and search with the generated `BlogsDocument` type:

```csharp
public class BlogSearch( ISitecoreExperienceSearchSdk search )
{
    public async Task<SearchResult<BlogsDocument>?> Find( string query, int page = 0 )
    {
        var resp = await search.Search<BlogsDocument>( query, new SearchQuery<BlogsDocument>
        {
            Limit  = 10,
            Offset = page * 10,
            Sort   = [ new SearchSort( BlogsDocument.Fields.Sortable.Date, SortDirection.Desc ) ]
        } );

        return resp.IsSuccessful ? resp.Result : null;
    }
}
```

Read `Result.Items` for hits and `Result.Total` for the count. Each hit carries the page's item id - pair it with `/?pageId={id}` above to render the result.

> [!NOTE]
> **Scope today.** The `/v1/search` endpoint supports `keyphrase`, `Limit`, `Offset` and `Sort`. The shared `SearchQuery<TDoc>` also exposes `Filters` / `Facets` / `RetrieveFields`, but these are **currently ignored** by Experience Search - its filter/facet wire format isn't yet confirmed against a live tenant, so `Result.Facets` is always `null`. Use Sitecore Search (below) when you need faceting.

Full configuration, the `ApiResponse` contract and the options reference live in the [package README](https://www.nuget.org/packages/PINGWorks.SitecoreSearch.ExperienceSearch/).

## Sitecore Search (Discover)

The standalone Sitecore Search product (formerly **Discover** / Reflektion). It's a separate subscription with its own console, customer key and API keys, but in return it offers a much richer surface than Experience Search - all behind one package and one DI call.

A single `AddSitecoreDiscoverSearchSdk(...)` registers three typed clients:

- **`ISitecoreDiscoverSearchSdk`** - keyphrase search, **semantic search**, **recommendations** (with recipe/rule overrides), **AI Q&A**, **type-ahead suggestions**, and **multi-widget batching** (one HTTP call for a whole page of widgets).
- **`ISitecoreDiscoverEventsSdk`** - the full analytics/event surface (widget views and clicks, identity, commerce, navigation). `view_widget` fires automatically after every search/recommend; you fire `click_widget` from your UI to drive recommendation quality.
- **`ISitecoreDiscoverIngestionSdk`** - push content into the index (create-or-update / delete) for sources with incremental updates enabled.

**1. Install**

```bash
dotnet add package PINGWorks.SitecoreSearch.DiscoverSearch
```

**2. Configure and register:**

```csharp
builder.Services.AddSitecoreDiscoverSearchSdk( opt =>
    builder.Configuration.GetSection( "SitecoreSearch:Discover" ).Bind( opt ) );
```

```json
{
  "SitecoreSearch": {
    "Discover": {
      "CustomerKey":       "{accountId}-{domainId}",
      "ApiKey":            "[secret]",
      "SearchEndpoint":    "https://discover-apse2.sitecorecloud.io",
      "EventsEndpoint":    "https://events-apse2.sitecorecloud.io",
      "IngestionEndpoint": "https://ingestion-apse2.sitecorecloud.io",
      "DefaultLocale":     "en_us"
    }
  }
}
```

> [!IMPORTANT]
> Copy the **customer key** verbatim from the CEC - don't split it. The endpoint URLs are **regional** (the example shows APSE2); use the values from your console's Developer Resources page.

**3. Describe your sources** in `sitecore-search.json` (under `discoverSources`). Each source **must** carry its `rfkId` (the widget identifier):

```json
{
  "discoverSources": [
    {
      "id": "abcd1234-aaaa-bbbb-cccc-1111deadbeef",
      "name": "Products",
      "rfkId": "rfkid_products_search",
      "fields": [
        { "name": "product_id", "type": "string", "key": true, "retrieve": true },
        { "name": "title",      "type": "string", "search": true, "retrieve": true }
      ]
    }
  ]
}
```

**4. Query / recommend / suggest** - inject `ISitecoreDiscoverSearchSdk`:

```csharp
public class ProductSearch( ISitecoreDiscoverSearchSdk search )
{
    public Task<ApiResponse<DiscoverSearchResult<ProductsDocument>>> Find( string query )
        => search.Search<ProductsDocument>( query, new SearchQuery<ProductsDocument> { Limit = 20 } );

    public Task<ApiResponse<DiscoverSearchResult<ProductsDocument>>> Related()
        => search.Recommend<ProductsDocument>();
}
```

### Visitor identity, consent and analytics

Discover personalisation depends on a stable visitor id. The SDK manages the `__ruid` cookie automatically in the **exact format the Sitecore JS SDK uses**, so a visitor is tracked under one id whether a request comes from the browser SDK or this server-side SDK. For authenticated users you can surface a known-user id, and **event publishing can be gated on cookie consent** - wire it to the Blazor SDK's tracking service so it honours your existing cookie banner. See [Analytics and tracking](analytics-tracking.md) for how consent flows through the host, and the [package README](https://www.nuget.org/packages/PINGWorks.SitecoreSearch.DiscoverSearch/) for the identity model, the consent callback, recommendations/click-attribution, Q&A, suggestions and ingestion in full.

## What the SDK provides vs. what you wire up

| The SDK gives you | You build |
| - | - |
| Typed clients (`ISitecore…SearchSdk`) and `ApiResponse<T>` results | The search-box and results-list Blazor components |
| Strongly-typed document records from `sitecore-search.json` | Field-by-field rendering of each hit |
| `__ruid` identity, auto-fired `view_widget`, consent gating | `click_widget` calls from your result/recommendation click handlers |
| The `/?pageId={id}` resolver for rendering a hit by id | The links/markup that point at it |

## Roadmap

- [ ] A worked **search-results component** in the Starter Kit (search box - results - pager) for each product.
- [ ] Confirm and wire the Experience Search filter/facet format once verified against a live tenant.

For everything available today, the two package READMEs are the complete reference: [Experience Search](https://www.nuget.org/packages/PINGWorks.SitecoreSearch.ExperienceSearch/) · [Sitecore Search / Discover](https://www.nuget.org/packages/PINGWorks.SitecoreSearch.DiscoverSearch/).
