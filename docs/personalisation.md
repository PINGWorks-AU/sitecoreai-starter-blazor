# Personalisation and testing
[&lt; Docs index](index.md)

> [!NOTE]
> **Placeholder - not yet implemented.**
> This page is a stub so we remember to document personalisation once SDK support lands. The content below sketches the intended scope; treat it as a to-do rather than a guide.

Sitecore CDP and Personalize build on the visitor data captured through tracking (see [Analytics and tracking](analytics-tracking.md)) to tailor what each visitor sees and to run experiments. This page will eventually cover how that works from a Blazor head.

## Personalisation with CDP / Personalize

Planned coverage:

- The relationship between tracking consent, the guest profile cookie (`sc_cid_personalize`) and audience membership
- How a personalised page variant is selected and delivered through the layout
- Surfacing the resolved variant in components (the `pageVariantId` already flows through `RecordPageView`)
- Server-side vs. client-side personalisation trade-offs in a server-rendered Blazor app
- Editing personalised variants in Sitecore Pages

## A/B/n testing

Planned coverage:

- Setting up an experiment in Sitecore
- How the head requests and renders the assigned variant
- Recording exposure and conversion events for an experiment
- Avoiding flicker / layout shift when a variant is applied
- Reading experiment results back

## To do

- [ ] Confirm how variant selection is delivered to the SDK (layout vs. separate call)
- [ ] Document the variant plumbing already present (`pageVariant` / `RecordPageView` pageVariantId, canvas-state `variant`)
- [ ] Add a worked personalised component to the Starter Kit
- [ ] Cross-link with analytics/tracking and editing docs
