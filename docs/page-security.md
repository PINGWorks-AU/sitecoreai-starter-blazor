# Page-level security
[&lt; Docs index](index.md)

Some pages aren't public: members-only content, gated downloads, sections for particular roles. The SDK lets a **content author** mark any page with an **authorization policy** - a named rule evaluated against the current user when the page is viewed. If the user doesn't satisfy the policy they're redirected away; pages with no policy are anonymous (public).

The key design decision: **policy names carry no built-in meaning.** They're just strings chosen in Sitecore that map to ASP.NET Core authorization policies you register in your host. The SDK defines no roles, no claims, and no sign-in mechanism, and it doesn't interpret the policy itself - *you* decide what each named policy means in `Program.cs`. Authentication (establishing *who* the user is) is wired up the standard ASP.NET Core / Blazor way; the SDK reads the result and lets your policies decide *what they may see*, per page.

## How it works at runtime

When `<SitecorePage>` renders a route:

1. It reads the policy-name field - `SitecoreSettings.AuthorizationPolicyFieldName` (default `_AccessPolicy`) - from the route's fields.
2. **Blank field - anonymous.** The page renders normally.
3. **Policy name present** - the SDK resolves the matching ASP.NET Core `AuthorizationPolicy` (through `IAuthorizationPolicyProvider`) and evaluates it against the current user (from the Blazor `AuthenticationStateProvider`).
4. **Pass** - the page renders.
5. **Fail** - the visitor is redirected to `SitecoreSettings.AuthorizationPolicyFailedRedirect` (default `/status-403`).

Evaluation runs on every page view. It is **skipped while editing** in Sitecore Pages - the authoring render path doesn't go through the `<SitecorePage>` route - so authors and designers always see content regardless of policy. Resolved policies are cached by name for the application's lifetime, so the per-request cost is negligible.

> [!WARNING]
> Evaluation is **fail-open**. If a page names a policy that doesn't resolve to a registered policy - a typo, or one you forgot to register - the SDK can't evaluate it and the page is **allowed** to render. Keep every policy name authors can pick in step with an `AddPolicy(...)` in your host, and treat the Sitecore policy list (below) as the source of truth for valid names.

### Access control, not per-user content

Page authorization gates **access to shared content**: every authorized user sees the *same* cached page, and unauthorized users are redirected. The decision is made live, per request, so it is fully compatible with the [shared layout cache](caching-sync.md). It does **not** make the page's content vary per user - that's [personalisation](personalisation.md), a separate concern that *does* interact with caching.

## Defining policies in your application

Register policies with the standard ASP.NET Core `AddAuthorization()` call. The policy **names must match** the names authors can pick in Sitecore.

```csharp
builder.Services
    .AddAntiforgery()
    .AddSitecoreBlazorSDK( builder.Configuration )
    .AddAuthorization( opts => {
        // Names must match the items under Settings/Authorization Policies in Sitecore.
        opts.AddPolicy( "Members", b => b.RequireAuthenticatedUser() );
        opts.AddPolicy( "Editors", b => b.RequireRole( "Editor" ) );
        opts.AddPolicy( "Group A", b => b.AddRequirements( new SampleAuthRequirement() ) );
    } )
    .AddRazorComponents()
    .AddInteractiveServerComponents();
```

A policy can be any ASP.NET Core construct - built-in requirements (`RequireAuthenticatedUser`, `RequireRole`, `RequireClaim`, `RequireUserName`…) or a custom `IAuthorizationRequirement` with its own handler:

```csharp
public class SampleAuthRequirement : AuthorizationHandler<SampleAuthRequirement>, IAuthorizationRequirement
{
    protected override Task HandleRequirementAsync( AuthorizationHandlerContext context, SampleAuthRequirement requirement )
    {
        // The SDK passes an AuthData instance as the authorization resource.
        if ( context.Resource is AuthData authData )
        {
            // authData.CurrentUser     -> the current ClaimsPrincipal
            // authData.RequestPath     -> the path being requested
            // authData.SitecoreContext -> the resolved route, its fields, etc.
        }

        context.Succeed( requirement );   // or leave unmet to fail the policy
        return Task.CompletedTask;
    }
}
```

### The `AuthData` resource

The SDK supplies an `AuthData` (`PINGWorks.SitecoreAI.BlazorSDK.Data.AuthData`) as the authorization **resource**, so your handlers can make **content-aware** decisions:

| Member | Type | Description |
| - | - | - |
| `CurrentUser` | `ClaimsPrincipal` | The current user, from the Blazor authentication state. |
| `RequestPath` | `string?` | The path being requested. |
| `SitecoreContext` | `SitecoreContext` | The resolved context for the page - the route item, its fields, and other Sitecore data - so a policy can branch on the content being viewed (for example a field on the page). |

## Bringing your own authentication

The SDK reads the current user from the Blazor `AuthenticationStateProvider`; it does **not** provide a sign-in flow. Wire up authentication in the host the standard way - cookie auth, OpenID Connect against Microsoft Entra ID, Auth0, or any provider that populates the `ClaimsPrincipal`. Your policies then run against that authenticated user.

A visitor who isn't signed in is simply an unauthenticated `ClaimsPrincipal`, which most policies (e.g. `RequireAuthenticatedUser`) reject - sending them to your failure redirect, where you can present a sign-in prompt or your own 403 page.

## Settings

| Setting | Default | Purpose |
| - | - | - |
| `AuthorizationPolicyFieldName` | `_AccessPolicy` | The route-level field the SDK reads the policy name from. Set to an **empty string to disable** the feature entirely. |
| `AuthorizationPolicyFailedRedirect` | `/status-403` | Where a failed visitor is redirected. Falls back to `/` if left blank. |

```json
{
  "SitecoreSettings": {
    "AuthorizationPolicyFieldName": "_AccessPolicy",
    "AuthorizationPolicyFailedRedirect": "/status-403"
  }
}
```

The default `/status-403` pairs with the status-page routing in [Error handling](error-handling.md) - add a `403` branch to your `/status-{code}` page for a friendly "Forbidden" screen. For an unauthenticated visitor you may prefer to point the redirect at a sign-in route instead.

## Authoring in Sitecore

The feature is driven by content; the supporting items ship in the Starter Kit module. There are two parts.

**The Authorization section on pages.** An interface template, `_PageAuthorization`, is added as a base template of the site's base **Page** template, contributing an **Authorization** section with one field:

- **Access policy** (`_AccessPolicy`) - a *Droplist* sourced from the site's `Settings/Authorization Policies` folder. Authors pick a policy name, or leave it blank for anonymous access.

Because the field lives on the base Page template, every page type inherits it - and you can set a **site-wide default on standard values**, with individual pages overriding as needed.

**The list of policy names.** Available policies are items under the site's `Settings/Authorization Policies` folder. Each **Authorization Policy** item's **name** is all that matters - that name appears in the *Access policy* droplist and must match an `AddPolicy("<name>", …)` in your host.

To add a policy: create an `Authorization Policy` item with the desired name, then register the matching `AddPolicy(...)` in `Program.cs`.

## Related

- [Error handling](error-handling.md) - the `/status-{code}` page that backs the default `403` redirect.
- [Caching and content sync](caching-sync.md) - why gating shared content is cache-safe.
- [Configuration settings](settings.md) - the settings reference.
