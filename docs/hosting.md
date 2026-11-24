# Hosting behind a proxy
[&lt; Docs index](index.md)

Container Apps, App Service on Linux, Front Door and Kubernetes ingress all put a reverse proxy in front of your site. The proxy terminates the visitor's connection, so your app sees the proxy's address, scheme and host unless it reads the `X-Forwarded-*` headers the proxy adds.

## What the SDK does

`AddSitecoreBlazorSDK()` registers ASP.NET Core's forwarded-headers middleware as a startup filter, so it runs at the very start of the request pipeline, ahead of your own middleware and the SDK's short-circuited endpoints. It honours:

- `X-Forwarded-For` — the client IP, used by [IP and path access control](ip-path-security.md), analytics and [Application Insights](app-insights.md) geolocation.
- `X-Forwarded-Proto` — the original scheme, so redirects and absolute URLs use `https`.
- `X-Forwarded-Host` — the original host name, used when building absolute URLs.

The forwarded headers are trusted from any caller, because the container ingress address can't be known ahead of time.

> [!IMPORTANT]
> Do not also call `app.UseForwardedHeaders()` in `Program.cs`. A second pass consumes an extra `X-Forwarded-For` entry and resolves the wrong client IP.

## Restricting forwarded host names

Because any caller can send `X-Forwarded-Host`, list the host names your site answers to in `SitecoreSettings.ForwardedAllowedHosts`. Only those values are accepted; any other forwarded host is ignored and the request's own `Host` is used.

```json
{
  "SitecoreSettings": {
    "ForwardedAllowedHosts": [ "www.example.com", "example.com" ]
  }
}
```

When the list is empty or missing, any forwarded host is accepted.

## Related

- [IP and path access control](ip-path-security.md) — allow-lists that depend on the forwarded client IP.
- [Health and readiness](health.md) — the readiness probe for container platforms.
- [Caching and content sync](caching-sync.md#multi-instance-cache-coordination) — running more than one instance.
- [Configuration settings](settings.md) — `ForwardedAllowedHosts`.
