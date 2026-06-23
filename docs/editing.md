# Editing in Sitecore Pages
[&lt; Docs index](index.md)

One of the reasons to build on Sitecore rather than a plain CMS is the authoring experience: content editors arrange components on a page, edit field values in place, and preview the result, all inside Sitecore Pages. The Blazor SDK makes your Blazor components fully editable in Pages with almost no extra work on your part. This page explains what "editing mode" actually does so you understand the few things you do need to get right.

## Turning editing on

Editing is gated by configuration. Set the following in `SitecoreSettings` (see [Configuration settings](settings.md)):

- `EnableEditingMode: true`
- `EditingSecret` - the shared secret from your Sitecore environment's Developer Settings
- `EnvironmentHostName` - your authoring host, so the SDK can build callback and media URLs

With editing enabled, the SDK registers the editing endpoints, applies the editing CORS policy (so Sitecore Pages is allowed to frame and call your app), and writes the appropriate Content-Security-Policy headers. You typically enable editing in development and authoring environments and leave it off on public production delivery.

## How Pages talks to your app

Sitecore Pages renders your live application inside an iframe and drives it through two endpoints the SDK registers for you.

### The config (handshake) endpoint

Default path `/api/editing/config`. When Pages connects, it calls this endpoint (passing the editing `secret`, validated against `EditingSecret`) and the SDK returns:

- `components` - the list of component names your app knows about. The SDK discovers these by reflecting over every loaded assembly for public types that subclass `SitecoreComponent` (excluding the SDK's own namespace). This is how the Pages component gallery knows which renderings your head supports.
- `editMode` - `"metadata"` (see below).
- `packages` - any npm packages declared in `SitecoreSettings.EditingConfigPackages`.

The response is cached and varies by host, origin and secret.

### The render endpoint

Default path `/api/editing/render`. Pages calls this to render a specific item/version/language in editing context. The SDK validates the secret and the route, then redirects to the actual page URL carrying the editing query parameters, so the page renders through the normal routing pipeline with editing turned on.

### Media while editing

When editing, requests for `/-/media/...` (and `/-/jssmedia/...`) are redirected to the authoring host so that unpublished media renders correctly in the editor. This is gated on the `sc_headless_mode=edit` cookie.

## Metadata editing mode

Older Sitecore headless editing injected editing "chrome" markup directly into the rendered HTML. SitecoreAI uses the newer **metadata editing mode** instead: your page renders as clean, production HTML, and the editor overlays its controls by reading metadata the SDK emits. The handshake reports `editMode: "metadata"` to tell Pages to use this approach.

The practical upshot is good news: the HTML your components produce in editing mode is essentially the same HTML they produce live. You don't write separate "edit" and "view" markup.

For metadata mode to work, the SDK emits a small block of editing state into the page - the canvas state (item id, version, site, language, device, page mode, variant) and a verification token - plus any client scripts Sitecore needs. This is rendered by the `SitecoreEditingScripts` component into the `SitecoreEditingScripts` section outlet, and only when the page is being rendered in editing context. Because the SDK handles this automatically inside its page renderer, all you have to do is make sure your `App.razor` includes the outlet:

```html
<SectionOutlet SectionName="SitecoreEditingScripts" />
```

See [Routing](routing.md) for the full document template.

## Field-level editing: the Sc components

In-place field editing is handled by the `Sc*` field components (`ScText`, `ScRichText`, `ScImage`, `ScLink`, and so on - see [Authoring-enabled components](authoring-enabled-components.md)). Each one renders editing chrome around its field when the page is in editing mode, so an author can click the field and edit it directly. This is driven by the shared `ScEditingChrome` component, which understands three kinds of editable region:

| Chrome type | Wraps |
| - | - |
| `Field` | An individual editable field (used by every `Sc*` field component). |
| `Rendering` | A component instance, so it can be selected, moved or deleted. |
| `Placeholder` | A placeholder, so components can be inserted into it. |

You get all of this for free by:

- Binding fields to `IField`-typed model properties (e.g. `RichTextField`, `ImageField`) rather than raw strings, and
- Rendering them through the `Sc*` components rather than emitting the value yourself.

If you render a field value directly (for example `@Model.Text?.Value`), it will display correctly but will **not** be editable in Pages. The Starter Kit's components often branch on `IsEditing` to show an editable `Sc*` component in the editor and a leaner output live - see `Title.razor` for an example.

## Empty placeholders and components

In editing mode, the placeholder component renders the empty-placeholder chrome so authors can drop components into a placeholder that has no content yet. If Pages references a component your head doesn't have a Blazor type for, the SDK renders a small placeholder marker (`SitecoreEmptyComponent`) naming the missing component, rather than failing - which makes it obvious in the editor that a component still needs building.

## A checklist for editable components

- Inherit from `SitecoreComponent<TModel>` (or `SitecoreTemplate<TModel>` for page templates).
- Type editable fields as Edge Content SDK field types (`RichTextField`, `ImageField`, `HyperlinkField`, …).
- Render fields with the matching `Sc*` component.
- Render child content areas with `<ScPlaceholder Name="…" />`.
- Make sure `App.razor` has the `SitecoreEditingScripts` outlet.

Do those five things and your component is fully authorable in Sitecore Pages.

## Next steps

Continue with [Working with GraphQL](graphql.md).
