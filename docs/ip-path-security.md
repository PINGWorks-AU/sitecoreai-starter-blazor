# IP and path access control
[&lt; Docs index](index.md)

Some endpoints should only be reachable from certain machines or networks — the built-in [Tools page](tools.md), an internal admin area, a diagnostics endpoint. The Blazor SDK can restrict any URL path prefix to a set of client IP addresses, returning a configurable status (404 by default) to everyone else. It is configured entirely through `SitecoreSettings.PathAccessControl`; there is no code to write.

## How it works

`PathAccessControl` is a list of rules. Each rule guards a **path prefix** and lists the **IP addresses** allowed to reach it:

```json
"SitecoreSettings": {
  "PathAccessControl": [
    {
      "PathPrefix": "/.tools",
      "AllowedIPs": [ "127.0.0.1", "::1" ],
      "DeniedStatusCode": 404
    },
    {
      "PathPrefix": "/admin",
      "AllowedIPs": [ "203.0.113.7", "10.0.0.0/8" ]
    }
  ]
}
```

On every request the SDK finds the rule whose `PathPrefix` matches the request path. If the caller's IP is in that rule's `AllowedIPs` the request proceeds as normal; otherwise it is short-circuited with the rule's `DeniedStatusCode`.

| Property | Type | Default | Description |
| - | - | - | - |
| `PathPrefix` | string | _required_ | The URL path this rule guards, e.g. `/.tools` or `/admin`. Matching is case-insensitive and **segment-aware**: `/admin` matches `/admin` and `/admin/users`, but not `/administrator`. |
| `AllowedIPs` | IReadOnlyList&lt;string&gt; | `[]` | Addresses permitted to reach the prefix. Accepts plain IPv4/IPv6 (`203.0.113.7`, `::1`) and CIDR ranges (`10.0.0.0/8`, `2001:db8::/32`). An empty list denies everyone. |
| `DeniedStatusCode` | int | `404` | Status returned to a disallowed caller. `404` (the default) keeps the guarded path undiscoverable; use `403` if you'd rather signal that the resource exists but is forbidden. |

### Most-specific prefix wins

When more than one rule matches a request, the **longest** matching prefix governs — so you can grant broad access and then tighten a sub-path, or vice versa. Rules of equal length merge their allow-lists.

### `/.tools` is open by default

`PathAccessControl` is **empty by default**. You should, as a minimum, add a single rule restricting `/.tools` (the [Tools page](tools.md)) to loopback — `127.0.0.1` and `::1`. The Tools page therefore works on `localhost` during development with no configuration, and is closed to everyone else. Because this is a config-based rule, you can:

- **Grant remote access** to the Tools page by adding addresses to the `/.tools` rule's `AllowedIPs`.
- **Open the Tools page** by removing the `/.tools` rule.
- **Guard your own paths** by adding more rules.

## Behind a reverse proxy

This is the part that catches people out. When the app runs behind a reverse proxy — Azure Container Apps, App Service on Linux, an ingress controller — the host sees **every** connection as coming from the proxy (typically `127.0.0.1` or a private address), not the real visitor. An IP allow-list compared against that would either allow everyone or no-one.

The real client IP arrives in the `X-Forwarded-For` header. `UseSitecoreBlazorSDK()` registers ASP.NET Core's forwarded-headers middleware for you, so the SDK compares the allow-list against the **originating** client IP rather than the proxy. The middleware is configured to trust the forwarded headers, because the container ingress IP can't be known ahead of time.

> [!NOTE]
> Because the **forwarded** client IP is what's checked, the default loopback rule on `/.tools` does not accidentally expose the page through the proxy — a real external visitor presents their own public IP, not the proxy's loopback address. Depending on configuration, it may still be possible for a determined attacker to spoof the necessary headers. Test in your own environment or disable the page on public-facing systems for protection by removing all allowed IPs from the rule.

## Related

- [The Tools page](tools.md) — the built-in consumer of this feature.
- [Configuration settings](settings.md) — the `PathAccessControl` reference.
- [Page-level security](page-security.md) — per-page authentication and authorization, a separate concern from network/IP access.
