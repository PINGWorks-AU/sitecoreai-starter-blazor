# Health and readiness
[&lt; Docs index](index.md)

On startup the SDK loads the site map (routes, dictionary, redirects) for every language and runs the initial content sync before the site can serve pages properly. The readiness endpoint lets your hosting platform hold traffic back until that has finished.

## Readiness endpoint

`GET /api/health/ready`

| Response | When |
| - | - |
| `503` `Starting` | Site routes or the initial content sync are still loading. |
| `200` `Ready` | Site routes are loaded and the initial sync has completed. |

The endpoint short-circuits the pipeline, so it is cheap to call often and is not affected by page routing or redirects. Change the path with `SitecoreSettings.ReadinessPath`.

## Wiring it to your platform

Point the platform's **readiness** probe at the endpoint so a starting instance receives no traffic until it is ready:

- **Azure Container Apps** — add a readiness probe of type HTTP on `/api/health/ready`.
- **Azure App Service** — set **Health check** path to `/api/health/ready`. App Service removes an unhealthy instance from the load balancer and replaces it if it stays unhealthy.
- **Kubernetes** — use it as the `readinessProbe` `httpGet` path.

Use it for readiness rather than liveness: a `503` during startup means "not yet", not "restart me".

> [!NOTE]
> If you restrict paths with [IP and path access control](ip-path-security.md), make sure the probe path is not covered by a rule that blocks the platform's probe.

## Related

- [Caching and content sync](caching-sync.md) — what happens at startup.
- [Configuration settings](settings.md) — `ReadinessPath`.
