# Application Insights
[&lt; Docs index](index.md)

The SDK can add Azure Application Insights browser telemetry (page views, client exceptions, dependency timings) to your site. Telemetry is sent to your own site, which forwards it to Application Insights, so the browser never talks to Azure directly and the instrumentation key is not exposed in the page.

## Enable it

1. Set the standard `APPLICATIONINSIGHTS_CONNECTION_STRING` configuration value. On Azure App Service and Container Apps this is the app setting Azure adds when you connect an Application Insights resource; locally, put it in User Secrets.
1. Add the component to the `<head>` of `App.razor`:

    ```html
    <ResourcePreloader />
    <AppInsights />
    ```

The Starter Kit already includes `<AppInsights />`. Until the connection string is set the component renders nothing and no proxy traffic is accepted.

## How it works

- `<AppInsights />` renders the Application Insights JavaScript loader snippet. The SDK script is served from the SDK's own static assets (`_content/PINGWorks.SitecoreAI.BlazorSDK/appInsights.3.gbl.min.js`), not a CDN.
- The browser is given your site's base URL as its ingestion endpoint and a placeholder in place of the instrumentation key.
- The SDK handles `POST /v2/track` and forwards it to the `IngestionEndpoint` from the connection string. On the way through it:
    - swaps the placeholder for the real instrumentation key,
    - tags each telemetry item with the client IP (`ai.location.ip`) so Application Insights geolocation still works,
    - strips `Cookie`, `Authorization` and `X-Forwarded-*` / `X-Original-*` headers,
    - rejects request bodies over 128 KB with `413`.

Because telemetry goes to your own origin, it is not blocked by ad blockers that filter Azure telemetry hosts, and your Content Security Policy needs no extra `connect-src` entry.

## Running code on initialisation

Pass a JavaScript function expression to `OnInitJs` to run it once the SDK has loaded, for example to add a telemetry initializer:

```html
<AppInsights OnInitJs="function (sdk) { sdk.addTelemetryInitializer(function (env) { env.tags['ai.cloud.role'] = 'website'; }); }" />
```

## Consent

`<AppInsights />` does not check the [analytics consent cookies](analytics-tracking.md#consent-first). If your consent policy covers operational telemetry, render the component conditionally, or use `OnInitJs` to configure the SDK (e.g. `disableCookiesUsage`) to suit.

## Related

- [Analytics and tracking](analytics-tracking.md) — Sitecore event tracking, which is separate from Application Insights.
- [Hosting behind a proxy](hosting.md) — how the client IP is resolved.
