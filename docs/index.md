# SitecoreAI with Blazor - Documentation

This documentation supersedes SitecoreAI's documentation when working with the Blazor SDK.

## Why the Blazor SDK?

The Blazor SDK is a full **C# / .NET 10** rendering host for SitecoreAI (aka XMCloud) - an alternative to Sitecore's official [Content SDK](https://github.com/Sitecore/content-sdk), which targets React / Next.js. Both consume the same Experience Edge content and both light up the Sitecore Pages visual editor; they differ in the stack you build on and in what each one gives you out of the box.

If your team lives in .NET, the Blazor SDK lets you build the entire site - UI, data binding, security, caching, background work - in one language and one type system, with no JavaScript/TypeScript build chain and no Node front-end host to operate. You also avoid coupling your delivery tier to a specific Jamstack host.

### What you get that the Content SDK does not

These are first-class features of the Blazor SDK with no direct equivalent (or only a host-/platform-provided one) in the Content SDK:

- **End-to-end C# / Blazor** - components, models, field binding and server logic in one strongly-typed language. No JS/TS, no separate Node rendering host, no dual build pipeline. The Content SDK is React/Next.js only.
- **Built-in two-layer content cache** - an in-process **memory + disk** cache of Edge layout responses, with publish-webhook-driven invalidation, optional on-disk compression, and start-up preloading. The site stays fast under load and keeps serving content even if Edge is briefly unavailable. The Content SDK leans on the hosting platform (e.g. Next.js/Vercel ISR and edge caching) for this. See [Caching and content sync](/docs/caching-sync.md).
- **Page-level security** - content authors mark any page with a named **authorization policy** that maps to a standard ASP.NET Core policy, evaluated per request against the signed-in user - fully compatible with the shared layout cache. See [Page-level security](/docs/page-security.md).
- **Built-in operational Tools page** - an in-app `/.tools` surface with a live **cache inspector** (browse, delete and clear memory/disk items, review a cache-clear event log), a **page layout viewer**, an **Edge webhook manager**, an **Edge settings editor**, and a **pluggable tab architecture** so you can add your own diagnostic tools.
- **Server-side form-submission handling** - an extensible pipeline (`IFormProcessor`) that runs your code when a Sitecore Forms submission arrives, fanning a single form out to multiple handlers, with ready-made processors for save-to-disk, email and Microsoft Teams. Both SDKs render Sitecore-authored forms, but Sitecore's headless SDKs leave what-to-do-with-the-data to Sitecore's Forms API or your own custom endpoint. See [Forms](/docs/forms.md).
- **Standard ASP.NET Core hosting** - runs as an ordinary .NET app on Azure App Service or Container Apps, with the full ASP.NET Core middleware ecosystem and no proprietary host lock-in.
- **Page by id** - for those circumstances when you only have an ItemId (e.g. search results) the SDK has a built-in resolver URL for loading pages by ID. Full security restrictions are still applied.

> [!TIP]
> **Hosting economics.** Because the Blazor SDK is a standard ASP.NET Core app, you can run it on **PING Works managed hosting on Azure**, which in most cases is at least **30% cheaper than Vercel** for comparable production environments - while keeping the self-healing cache and resilience described above.

### Not yet available in the Blazor SDK

The Content SDK has the following capabilities that the Blazor SDK does not yet match. If your project depends on these today, weigh them against the advantages above or reach out to PING Works for updates on the roadmap:

- **Personalization** - page-level personalized variants by audience. In development; see [Personalisation and testing](/docs/personalisation.md).
- **A/B/n testing** - component experiment variants with built-in analytics. In development; see [Personalisation and testing](/docs/personalisation.md).
- **Pre-built search UI components** - the search *clients* (querying, paging, sorting, plus recommendations, AI Q&A, suggestions, events and ingestion for Sitecore Search) are **available now** as companion packages; what's still planned is a worked search-results *component* in the Starter Kit. See [Search](/docs/search.md).
- **Static Site Generation (SSG)** - the Blazor SDK renders via SSR + interactive server components rather than pre-generating a static site the way Next.js does. TTFB for Blazor SSR is under 20ms right from the host - with performance this good there's little to be gained from SSG. Under SSR, content changes are instantly available after publishing and server-side execution provides a highly secure environment for hosting integration code, APIs and private data. There are no plans to implement SSG at this time.
- **Multi-site hosting** - the Content SDK can serve multiple independent sites from one app; the Blazor SDK currently targets a single configured site. Up to 64 Azure App Services can be hosted on a single App Service Plan, so this is not likely to be a significant limitation, but if this is a showstopper for your use-case please let us know. 

> [!NOTE]
> This comparison reflects the current state of the Blazor SDK and Sitecore's Content SDK at the time of writing. The "not yet" list is an active roadmap, not a permanent gap - check the linked pages for status.

## Contents

### Getting started

1. [Starter Kit anatomy](/docs/anatomy.md)
1. [What happened to Helix?](/docs/what-happened-to-helix.md)
1. [Installation](/docs/installation.md)
1. [Configuration settings](/docs/settings.md)
1. [Routing](/docs/routing.md)

### Building your site

1. [Pages, Templates and Components](/docs/components.md)
1. [Models and binding](/docs/models.md)
1. [Authoring-enabled components](/docs/authoring-enabled-components.md)
1. [Client-side script with ComponentScript](/docs/component-script.md)
1. [Multi-language support](/docs/multi-language.md)
1. [Error handling](/docs/error-handling.md)
1. [Forms](/docs/forms.md)

### The system in depth

1. [Editing in Sitecore Pages](/docs/editing.md)
1. [Working with GraphQL](/docs/graphql.md)
1. [Caching and content sync](/docs/caching-sync.md)
1. [Analytics and tracking](/docs/analytics-tracking.md)
1. [Search](/docs/search.md)

### Security

1. [IP and path access control](/docs/ip-path-security.md)
1. [Page-level security](/docs/page-security.md)

### Tools

1. [The Tools page](/docs/tools.md)
1. [Cache inspection](/docs/tools-cache.md)
1. [GraphQL Viewer](/docs/tools-graphql.md)
1. [Edge Webhooks](/docs/tools-webhooks.md)
1. [Edge Settings](/docs/tools-edge-settings.md)

### In development (placeholders)

1. [Personalisation and A/B/n testing](/docs/personalisation.md)
1. [Design Studio support](/docs/design-studio.md)
