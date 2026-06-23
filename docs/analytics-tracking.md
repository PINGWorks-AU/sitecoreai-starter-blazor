# Analytics and tracking
[&lt; Docs index](index.md)

If you've worked with Sitecore on-premises you'll know xConnect and the xDB: the engagement database that captured page views, goals, outcomes and contact data. In SitecoreAI that role is taken over by Sitecore's cloud events ingestion (the same pipeline that feeds CDP and Personalize). The Blazor SDK gives you a small, consent-aware API over that pipeline so you can record page views, custom events, form interactions and visitor identity without dealing with the wire format yourself.

## The moving parts

| Piece | Role |
| - | - |
| `ISitecoreTracking` | The service you inject to record events. |
| `SitecoreAnalyticsSettings` | The `SitecoreSettings.Analytics` configuration section - the master switch, consent cookie names and tracking cookie settings. |
| Tracking token endpoint | A built-in endpoint (default `/api/token`) used to establish a browser context id and record the first page view. |
| Consent cookies | Two cookies that gate whether events and profile data are recorded at all. |

The SDK records events against Sitecore's cloud endpoint via the Experience Edge Events SDK, so there is no xConnect or xDB to stand up - tracking works the moment your Edge context is configured.

## Consent first

No event is recorded unless the visitor has opted in. The SDK checks two consent cookies on every request:

- `Analytics.EnableEventsCookie` (default `pw#eventConsent`) - gates **event tracking** (page views, custom events, form events).
- `Analytics.EnableProfileCookie` (default `pw#profileConsent`) - gates **guest profile** tracking (the identity associated with a visitor across sessions).

A cookie enables tracking only when its value is exactly `"1"`. When consent is present, the SDK reads (or issues) the corresponding tracking identifiers:

- `Analytics.EventsCookie` (default name `sc_cid`) holds the browser context id used for events.
- `Analytics.ProfileCookie` (default name `sc_cid_personalize`) holds the guest profile id.

If `Analytics.Enabled` is `false`, nothing is tracked regardless of cookies. This is the master kill-switch.

The Starter Kit ships a `CookieConsent.razor` component that shows a preferences panel until the visitor has made a choice, and an accompanying `CookieConsent.razor.js` that writes the consent cookies. Use it as-is or replace it with your own consent UI - all the SDK cares about is the cookie values.

## Establishing a session: the token endpoint

The first thing the browser does on a tracked page is call the tracking token endpoint (default `/api/token`). That endpoint:

1. Reads the consent cookies from the request.
2. If event tracking is consented, ensures a browser context id exists (issuing one if needed) and writes the tracking cookie back to the response.
3. Records the initial page view.
4. Returns the cookie name, the browser context id and the cookie expiry to the client.

If event tracking is not consented, the endpoint returns `204 No Content` and records nothing.

You don't normally call this endpoint yourself - the SDK's client wiring does it - but it's useful to know it exists when you're debugging why events aren't appearing.

## Recording events from a component

Inject `ISitecoreTracking` and call the method you need. Every method is a no-op when the visitor hasn't consented, so you don't need to guard your calls.

```razor
@inject ISitecoreTracking Tracking

<button @onclick="Download">Download the brochure</button>

@code {
    private async Task Download()
    {
        await Tracking.RecordCustomEvent(
                "BrochureDownload",
                eventProperties: ( "asset", "2026-brochure.pdf" ) );

        // ...trigger the download...
    }
}
```

### The API

All parameters after the first are optional; sensible defaults are applied (current path, resolved language, `ChannelKind.WEB`, `DateTime.UtcNow`).

| Method | Use it to |
| - | - |
| `RecordPageView( path, language, pageVariantId, channel, referrerUrl, searchData, extraParams, requestedAt )` | Record a page view. The SDK records the initial view for you via the token endpoint; call this for client-side navigations you want counted. |
| `RecordCustomEvent( eventName, path, language, channel, searchData, extraParams, requestedAt, params eventProperties )` | Record an arbitrary named event with optional key/value properties. |
| `RecordFormEvent( componentInstanceId, formId, interaction, requestedAt )` | Record a form interaction (e.g. viewed, submitted) using the `FormInteraction` enum. |
| `IdentifyUser( user, path, language, channel, extraParams, requestedAt )` | Associate the current visitor with a `SitecoreIdentity` (email, name, address, custom identifiers). Only fires when event tracking is consented and a browser context id exists. |
| `SetTrackingFromContext( httpContext )` | Establish the tracking context from the current request. Called by the SDK; you rarely need it directly. |
| `VerifyReferrer( referrer )` | Returns the referrer only when it is an external host, so you don't record same-site navigations as referrals. |

`Context` exposes the current `SitecoreTrackingData` (browser context id, guest profile id, consent flags) if you need to inspect what the SDK resolved for the request.

## How tracking survives the static-to-interactive transition

Blazor renders a page first as static server-side HTML and then "lights it up" as an interactive circuit. The SDK registers a tracking circuit handler that re-establishes the tracking context when the interactive circuit opens, so events fired from interactive components continue to use the same browser context id that was issued during the initial request.

## A note on personalisation

Consent for the profile cookie (`sc_cid_personalize`) is what enables visitor profiles to be built up over time - the foundation that Sitecore CDP and Personalize use to target content. Personalisation itself (audiences, experiences, A/B/n tests) is a separate topic; see [Personalisation and testing](personalisation.md).
