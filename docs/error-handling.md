# Error handling
[&lt; Docs index](index.md)

A real Sitecore site needs to handle two things gracefully: a request for a URL that doesn't resolve to any item, and an unhandled exception thrown while rendering a page. The SDK provides building blocks for both, and the Starter Kit wires them up so that 404 and 500 responses use Sitecore-authored content where it exists, and fall back to sensible defaults where it doesn't.

## The pipeline

`Program.cs` configures the standard ASP.NET status-code and exception-handling middleware to re-execute against a single error page:

```cs
if ( !app.Environment.IsDevelopment() )
{
    app.UseExceptionHandler( "/status-500", createScopeForErrors: true );
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute( "/status-{0}", createScopeForStatusCodePages: true );
```

This means any non-success status code (including 404 from the Sitecore router) and any unhandled exception in production are routed through a single `Status` page, parameterised on the original status code.

In development, the standard developer exception page is used instead so you get the full stack trace.

## The Status page

`UI/Pages/Status.razor` is a plain Blazor page with a parameterised route:

```razor
@page "/status-{StatusCode:int}"
@layout MainLayout

@switch ( StatusCode )
{
    case 404:
    case 500:
        <ScErrorContent StatusCode="@StatusCode" />
        break;
    case 403:
        <h1>Forbidden</h1>
        <p>You don't have permission to view that page.</p>
        break;
    default:
        <h1>Status @StatusCode</h1>
        <p>Something went wrong.</p>
        break;
}

@code {
    [Parameter] public int StatusCode { get; set; } = 404;
}
```

For 404 and 500 the page delegates to `<ScErrorContent />`, an SDK component that picks the best content to render. Any other status code is handled inline - 403 has its own message in the Starter Kit and everything else gets a generic fallback. Add new branches as your site grows.

## How `ScErrorContent` chooses what to render

`ScErrorContent` follows a three-step render order:

1. **Developer detail** - if the current request is from a developer (the host environment is `Development`, or `SitecoreSettings.EnableEditingMode` is `true`) and the captured exception is available, the SDK renders a stack-trace panel. End users never see this.
1. **A Sitecore-authored error page** - if the current site has an `Error404` or `Error500` path configured, `ScErrorContent` renders the page at that path through the SDK's `SitecorePage` component. This gives content editors full control over the error experience, including branding and links back to the rest of the site.
1. **Per-status fallback** - if no Sitecore error page is configured, or rendering the configured page itself fails, `ScErrorContent` renders the SDK's built-in `Error404Content` or `Error500Content` component. Both are intentionally plain.

You can override the fallback in your own application by passing a `RenderFragment`:

```razor
<ScErrorContent StatusCode="@StatusCode">
    <Fallback404Content>
        <h1>We couldn't find that page</h1>
        <p>Try the <a href="/">home page</a> or use the search above.</p>
    </Fallback404Content>
</ScErrorContent>
```

## Authoring error pages in Sitecore

To take advantage of step 2, set the `Error404` and `Error500` fields on your site definition item in Sitecore to the paths of the pages you want to render. The SDK looks up the site for the current host (and the current language) and reads those two values.

These pages are rendered just like any other Sitecore page, which means they have access to your full component library. Treat them as regular pages of type `Page` (or whatever you've set as `DefaultPageTemplate`) with templates and components mapped through the standard SDK resolution.

## Logging

`ScErrorContent` logs every captured 500 once, including the request path and the exception, before deciding how to render. You don't need to add any extra logging in your error page; the SDK has already taken care of it.

When the configured Sitecore error page itself fails to render (for example, a broken component on the 500 page), the failure is logged and the per-status fallback takes over so users still see a response.

## Setting the response status code

When `UseStatusCodePagesWithReExecute` runs, ASP.NET keeps the original status code on the response. For exceptions, `SitecorePage` sets the response status to 500 during the static SSR pass so the right status code is returned to the client. You don't normally need to set this yourself; if you have a custom error path that bypasses these middlewares, set `HttpContext.Response.StatusCode` manually.

## NotFound from the Sitecore router

The `<SitecoreRouter>` in `Routes.razor` accepts a `NotFoundPage` parameter. The Starter Kit points it at `Pages.Status` so that an unmatched Blazor route renders the same error page as everything else:

```razor
<SitecoreRouter AppAssembly="typeof( Program ).Assembly" NotFoundPage="typeof( Pages.Status )">
    ...
</SitecoreRouter>
```

If you'd rather render a dedicated component for "Blazor route not found" - distinct from "Sitecore content not found" - point `NotFoundPage` at your own component and leave `UseStatusCodePagesWithReExecute` to deal with HTTP 404 responses from elsewhere in the pipeline.
