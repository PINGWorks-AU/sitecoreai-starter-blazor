# Pages, Templates and Components
[&lt; Docs index](index.md)

## Location conventions

The SDK does not enforce a folder layout, but the Starter Kit organises Razor files in a way that mirrors the three Sitecore concepts you'll work with most often.

|Path|Description|
|-|-|
| `\UI\Components`| Blazor components mapped to Sitecore renderings. Organise in any way convenient to your project |
| `\UI\Layouts`   | Standard Blazor layouts (`@inherits LayoutComponentBase`) |
| `\UI\Pages`     | Pages navigable through Blazor routes (which override Sitecore routes) |
| `\UI\Templates` | Blazor components mapped to Sitecore page templates |

## Sample file hierarchy

A typical `UI` folder ends up looking something like this. Most components are a single `.razor` file; a component that supports rendering variants (`Banner` below) gains a sibling folder of variant files, and a component with scoped styling or script gains `.razor.css` / `.razor.js` files beside it.

```
UI/
├── App.razor                     # root document (see App.razor requirements)
├── Routes.razor                  # hosts <SitecoreRouter>
├── _Imports.razor                # shared @using directives
├── Components/                   # mapped to Sitecore renderings
│   ├── RichText.razor
│   ├── LinkList.razor
│   ├── Promo.razor
│   ├── Banner.razor              # dispatcher, mapped to the "Banner" rendering
│   ├── Banners/                  # variant components used by Banner.razor
│   │   ├── DefaultVariant.razor
│   │   ├── BannerVariant.razor
│   │   └── ImageVariant.razor
│   ├── CookieConsent.razor
│   ├── CookieConsent.razor.css   # component-scoped styles
│   └── CookieConsent.razor.js    # component-scoped script
├── Layouts/
│   └── MainLayout.razor          # default layout for Sitecore pages
├── Pages/                        # explicit Blazor @page routes
│   └── Status.razor              # error page (see Error handling)
└── Templates/                    # mapped to Sitecore page templates
    └── Page.razor                # default page template
```

The folders are a convention the Starter Kit follows, not a rule the SDK enforces. As the table above notes, you are free to organise `Components` in whatever way suits your project; the SDK finds components by name (see [Name resolution process](#name-resolution-process)), not by location.

## Pages and Routes

The SDK's `SitecorePage` component is registered as a fallback route at `/{*RequestPath}`, which means that all explicitly-specified routes and static assets are fully supported - you can mix and match Sitecore pages and regular Blazor pages within a single application.

`SitecorePage` is wired up through the `<SitecoreRouter>` component you place in `Routes.razor`. The router behaves like the standard `<Router>` but routes any URL it does not recognise through to Sitecore content. The Starter Kit also passes a `NotFoundPage` so unmatched URLs render the [error-handling](error-handling.md) page rather than crashing.

When you first create a SitecoreAI site, your navigable page items typically inherit from the Sitecore template called `Page`.

### Loading a page by id

Sometimes you have a page's **ItemId** but not its URL. The most common case is search results: a search index returns the ItemId of a matching page, but may not store the page's friendly URL. Rather than look the URL up yourself, you can let the SDK resolve and render the page from its id.

Navigate to:

```
/?pageId={id}
```

and the SDK loads that page in a single request to Experience Edge. The layout and the page's canonical URL come back together, SDK renders it, and rewrites the address bar to the friendly URL without an HTTP redirect. This works under both static SSR and interactive rendering, and across enhanced navigation.

```
https://yoursite/?pageId=c6321f45-2f83-4b09-af1a-f2f12fdb0732
   → renders the page, then the address bar becomes
      → https://yoursite/blog/my-article
```

A few rules govern the behaviour:

- **A route path always wins.** `pageId` is only consulted when the request has no route path (i.e. the root `/`). If both are present, the path is used and `pageId` is ignored.
- **The site's route allow-list still applies.** The resolved page must belong to the current site, exactly as for a directly-requested URL. A page that the site does not serve resolves to the standard not-found page.
- **Page-level [authorization policies](page-security.md) still apply.** Loading by id is not a way around page security - the resolved page is authorised the same way a direct URL would be.
- **The page is cached under its URL.** A later direct visit to the friendly URL is served from cache.

The parameter is named `pageId` (matched **case-insensitively**) rather than `itemId` on purpose: a generic name keeps public URLs from advertising the underlying CMS. The value binds to a `Guid`, so anything that is not a valid id is simply ignored and the request falls through to normal routing.

## Templates

The SDK maps a Sitecore page template to a Blazor component of the same name that inherits from `SitecoreTemplate`. This lets you vary the structure of your pages when the page template changes in Sitecore. If no matching Blazor component is found, the default - usually `\UI\Templates\Page.razor` - is used. The default template name can be changed via the `SitecoreSettings.DefaultPageTemplate` configuration value.

To override the name mapping, add `@attribute [SitecoreComponent( "Sitecore Template Name" )]` to your `.razor` file. The Starter Kit's `Page.razor` uses this to register itself under the name `Default` as well:

```razor
@attribute [SitecoreComponent( "Default" )]
@inherits SitecoreTemplate<Page.ViewModel>
```

## Components

Create a component with the instructions laid out in [Sitecore Docs](https://doc.sitecore.com/sai/en/developers/sitecoreai/build-your-first-component.html#create-and-deploy-the-new-component)

When creating the rendering definition in SitecoreAI's Content Editor, target the clone to Renderings/Project/websites

The SDK automatically maps a Sitecore rendering to a Blazor component of the same name that inherits from `SitecoreComponent`.

To override the name mapping, add `@attribute [SitecoreComponent( "Sitecore Rendering Name" )]` to your `.razor` file.

## Name resolution process

The SDK resolves page templates and renderings the same way:

1. `appSettings.json` specifies a list of namespaces to search in `ComponentResolverSettings.SearchNamespaces`. The default `[ "*" ]` searches every loaded assembly.
1. `public` types decorated with the `SitecoreComponentAttribute` are searched first for a case-insensitive match by name.
1. A Blazor component mapped to a Sitecore page template must inherit from `SitecoreTemplate` (or `SitecoreTemplate<>`).
1. A Blazor component mapped to a Sitecore rendering must inherit from `SitecoreComponent` (or `SitecoreComponent<>`).

The first match that satisfies the requirements is automatically selected and cached for future use.

## Rendering Variants

### What a variant is

In Sitecore, a single rendering can be authored to display in more than one way. These are called **rendering variants**: the content author picks a variant from a dropdown in the Pages editor, and the same rendering (with the same datasource) is presented differently. A `Banner` rendering, for instance, might offer a full-bleed hero image, a plain image with a caption, or a default two-image layout for desktop and mobile.

The key distinction for "rendering variant" versus "component" is that all rendering variants for a component share the same data. Only the presentation of this data changes.

The SDK surfaces the author's choice on the model through the rendering's `FieldNames` parameter (as this is how Sitecore sends it). The Starter Kit's `ViewModelBase` exposes it as `RenderingVariant`:

```cs
[SitecoreComponentParam( "FieldNames" )]
public string? RenderingVariant { get; set; }
```

So inside your component you can read `Model.RenderingVariant` to find out which variant the author selected. (The value is the variant's name, or empty/`"Default"` when none is chosen.)

### The problem variants create

Name resolution maps **one Blazor component to one Sitecore rendering**. The author's variant choice does not change the rendering name, so it does not pick a different Blazor component - the SDK always lands on the same `Banner.razor`. That one file is therefore responsible for rendering *every* variant.

If you put all the variant markup in a single file behind a chain of `@if` blocks, the file quickly becomes long and hard to follow. The **dispatcher pattern** keeps it manageable.

### The dispatcher pattern

The idea is to split the work in two:

1. A **dispatcher** - the component the SDK actually resolves (`Banner.razor`). It owns the `ViewModel`, reads `Model.RenderingVariant`, and does nothing but choose which variant to show. It contains no variant-specific markup.
2. One **variant component** per variant (`DefaultVariant.razor`, `BannerVariant.razor`, `ImageVariant.razor`). Each is a plain Razor component holding the markup and helpers for just that one variant. These are **explicitly NOT derived from `SitecoreComponent`**.

This keeps each file small and focused: the dispatcher is a routing table, and each variant file reads like an ordinary component.

### Worked example: `Banner`

The `Banner` rendering in the Starter Kit supports three variants. Here is the whole feature, file by file.

**The shared model.** The dispatcher declares the `ViewModel` once; every variant binds to the same data, so all the fields an author might use live in one place.

```cs
// inside Banner.razor (shown in full below)
public sealed class ViewModel : ViewModelBase
{
    public HyperlinkField? TargetUrl { get; set; }
    public ImageField? Image { get; set; }
    public StringField? ImageCaption { get; set; }
    public ImageField? ImageDesktop => Image;   // alias used by the Default variant
    public ImageField? ImageMobile { get; set; }
}
```

**The dispatcher, `Components/Banner.razor`.** It inherits `SitecoreComponent<Banner.ViewModel>` so the SDK binds the model and supplies the `Id` and `IsEditing` context. The body is a simple branch on the variant name; each branch renders one variant component. The sample shows an `if` statement, but a `switch` works well for dispatchers with more than 2 or 3 variants to handle.

```razor
@using BlazorStarter.UI.Components.Banners
@inherits SitecoreComponent<Banner.ViewModel>

@if ( Model.RenderingVariant == VARIANT_DEFAULT )
{
    <DefaultVariant Model="Model" />
}
else if ( Model.RenderingVariant == VARIANT_BANNER )
{
    <BannerVariant Model="Model" Id="@Id" IsEditing="IsEditing" />
}
else
{
    <ImageVariant Model="Model" Id="@Id" IsEditing="IsEditing" />
}

@code {
    private const string VARIANT_BANNER = "Banner";
    private const string VARIANT_DEFAULT = "Default";

    public sealed class ViewModel : ViewModelBase
    {
        public HyperlinkField? TargetUrl { get; set; }
        public ImageField? Image { get; set; }
        public StringField? ImageCaption { get; set; }
        public ImageField? ImageDesktop => Image;
        public ImageField? ImageMobile { get; set; }
    }
}
```

**The variant components, `Components/Banners/*.razor`.** Each one is a normal Razor component. It is **not** a `SitecoreComponent`, so it does not get a model or the `Id` / `IsEditing` context automatically - the dispatcher hands those down as parameters.

`DefaultVariant.razor` only needs the model:

```razor
<section class="banner-image">
    <div class="banner-image__inner">
        <ScImage Field="@Model.ImageDesktop" Class="image-desktop" />
        <ScImage Field="@Model.ImageMobile" Class="image-mobile" />
    </div>
</section>

@code {
    [Parameter, EditorRequired] public required Components.Banner.ViewModel Model { get; set; }
}
```

`ImageVariant.razor` also needs `Id` and `IsEditing`, so it declares them as required parameters and the dispatcher passes them in:

```razor
<div class="component image @Model.Styles @Model.GridParameters" id="@Id">
    <div class="component-content">
        <span class="sc-image-wrapper">
            @if ( IsEditing || string.IsNullOrWhiteSpace( Model.TargetUrl?.Value?.Href ) )
            {
                <ScImage Field="Model.Image" />
            }
            else
            {
                <ScLink Field="@Model.TargetUrl">
                    <ScImage Field="Model.Image" />
                </ScLink>
            }
        </span>
        <span class="image-caption field-imagecaption">
            <ScText Field="Model.ImageCaption" />
        </span>
    </div>
</div>

@code {
    [Parameter, EditorRequired] public required Components.Banner.ViewModel Model { get; set; }
    [Parameter, EditorRequired] public required string? Id { get; set; }
    [Parameter, EditorRequired] public required bool IsEditing { get; set; }
}
```

`BannerVariant.razor` shows how variant-specific helpers (computed CSS classes and inline styles) live in the variant file rather than polluting the model or the dispatcher:

```razor
<div class="component hero-banner @Model.Styles @Model.GridParameters @BannerClassHeroBannerEmpty" id="@Id">
    <div class="component-content sc-sxa-image-hero-banner" style="@BannerBackgroundStyle">
        @if ( IsEditing )
        {
            <div class="image">
                <ScImage Field="@Model.Image" />
            </div>
        }
    </div>
</div>

@code {
    [Parameter, EditorRequired] public required Components.Banner.ViewModel Model { get; set; }
    [Parameter, EditorRequired] public required string? Id { get; set; }
    [Parameter, EditorRequired] public required bool IsEditing { get; set; }

    private string BannerClassHeroBannerEmpty
        => !IsEditing && string.IsNullOrWhiteSpace( Model.Image?.Value?.Src ) ? "hero-banner-empty" : string.Empty;

    private string BannerBackgroundStyle
        => !string.IsNullOrWhiteSpace( Model.Image?.Value?.Src ) && !IsEditing ? $"background-image: url('{Model.Image?.Value?.Src}')" : string.Empty;
}
```

### The conventions, and why they exist

A few rules make this pattern consistent across the codebase. Each one solves a concrete problem:

- **Variants live in a sibling folder, and that folder must not be named the same as the component.** The Starter Kit puts `Banner`'s variants in a `Banners` folder. Blazor turns a folder name into a namespace segment, so a folder called `Banner` would produce the namespace `…Components.Banner` - which collides with the `Banner` class itself, and the project won't compile. Using the plural (`Banners`), or a `<ComponentName>Variants` suffix, sidesteps the clash. Either convention is fine; just keep it different from the type name.
- **The dispatcher adds `@using …Components.Banners`** (the variant folder's namespace) so it can refer to `<DefaultVariant />`, `<BannerVariant />` and so on without fully-qualifying them.
- **Name the variant files after the Sitecore variant.** Take the variant name as it appears in Sitecore, remove the spaces, and suffix `Variant`: `Image Left` becomes `ImageLeftVariant.razor`. The fallback (no variant selected) is `DefaultVariant.razor`. This makes it obvious which file renders which authored variant.
- **Pass down only the context a variant actually uses.** Variant components are ordinary components, so they receive nothing from the SDK automatically. Declare `Model` (always needed) plus any host context the variant uses - `Id`, `IsEditing` - as `[Parameter, EditorRequired]`, and let the dispatcher supply them. `DefaultVariant` above takes only `Model` because it needs nothing else.
- **Keep shared markup on the dispatcher, variant-specific markup in the variant.** If every variant needs the same wrapper, heading or `<style>` block, you can place it on the dispatcher above the branch. Anything that differs between variants belongs in the variant file.

### When you don't need this

The dispatcher pattern is only worth it for components that genuinely have multiple variants. A component with a single presentation is just one `SitecoreComponent<TModel>` file with its markup inline - no folder, no dispatcher. Reach for the pattern when a second variant appears, not before.

Some example components in the Starter Kit that follow this pattern include `Banner` and `Promo`.
