# Authoring-enabled components
[&lt; Docs index](index.md)

The SDK ships a family of `Sc*` components for displaying Sitecore field data. These are **authoring-enabled components** you render with. The field data itself is held on your model in one of the Content SDK field types (see [Models and binding](models.md)); the `Sc*` component is what you wrap around it to put it on the page and make it editable in Sitecore Pages.

When you build a component, you choose the `Sc*` component that most closely aligns with the kind of field you want to author. That choice does three things:

- **Picks the HTML element** used to represent the data (an `<img>`, an `<a>`, escaped text, raw HTML, and so on).
- **Surfaces the relevant attributes** for that element (e.g. `Alt`/`SrcSet` on an image, `Target`/`Rel` on a link, `DateFormat` on a date).
- **Selects the correct value-parsing mechanism** so the field's stored value is interpreted and emitted appropriately (a date formatted to a culture, a number, an image URL with media parameters, rich-text HTML rendered verbatim rather than escaped).

The SDK provides the following authoring-enabled components:

- `ScDate`
- `ScFile`
- `ScImage`
- `ScLink`
- `ScNumber`
- `ScPlaceholder`
- `ScRichText`
- `ScText`

The Starter Kit provides some worked examples on use of these.

Each `Sc*` component takes a `Field` parameter sourced from your view model and renders the appropriate HTML. Parameters that you supply directly to the component (e.g. an `Alt` text on `ScImage`) override the value that would otherwise come from the Sitecore field.

All these components also accept `Editable="false"` to suppress the in-place editor chrome. The default is `true` so that fields are editable inside the Pages editor.

## Component-to-field mapping

Each component's `Field` parameter is typed to a field from the `PINGWorks.SitecoreExperienceEdge.ContentSDK` library. Use the table to pick the component that matches the field you're authoring.

| Component       | HTML rendered                                   | `Field` parameter type                   |
| --------------- | ----------------------------------------------- | ---------------------------------------- |
| `ScDate`        | formatted text                                  | `…ContentSDK.Fields.DateTimeField`       |
| `ScNumber`      | formatted text                                  | `…ContentSDK.Fields.DecimalField`        |
| `ScFile`        | `<a />`                                         | `…ContentSDK.Fields.Sitecore.FileField`  |
| `ScLink`        | `<a />`                                         | `…ContentSDK.Fields.HyperlinkField`      |
| `ScImage`       | `<img />`                                       | `…ContentSDK.Fields.Sitecore.ImageField` |
| `ScText`        | escaped text (optionally `<br />` for newlines) | `…ContentSDK.Fields.StringField`         |
| `ScRichText`    | raw HTML, rendered verbatim as a `MarkupString` | `…ContentSDK.Fields.StringField`         |

The `StringField` parameter on `ScText` and `ScRichText` is a base type, so any concrete string-backed field assigns to it - `StringField`, `SingleLineTextField` and `RichTextField` all derive from it. In practice you author single-line and multi-line text through `ScText`, and rich-text (HTML) content through `ScRichText`. Likewise the concrete `DateField` derives from `DateTimeField` (so it works with `ScDate`), and `GeneralLinkField` derives from `HyperlinkField` (so it works with `ScLink`). When in doubt, pick the component by the element you want on the page; the field type will line up.

## ScDate

Renders the text value of the field, optionally formatted, or nothing if empty.

```html
<ScDate Field="[DateTimeField]" Editable="[bool=true]"
  Culture="[string?]"
  DateFormat="[string?]" />
```

`DateFormat` is a standard or custom .NET format string; `Culture` accepts any culture name that `CultureInfo.CreateSpecificCulture` will resolve. If `Culture` is omitted, `CultureInfo.CurrentUICulture` is used (which the SDK sets per request based on the resolved language).

## ScFile

Renders an anchor (`<a />`). The label of the anchor will be the content of the element, or the `Title` property of the Sitecore field value.

```html
<ScFile Field="[FileField]" Editable="[bool=true]"
  Target="[string?]"></ScFile>
```

## ScImage
Renders an image (`<img />`).

```html
<ScImage Field="[ImageField]" Editable="[bool=true]" 
  Alt="[string?]" Class="[string?]"
  Width="[int?]" Height="[int?]"
  HSpace="[int?]" VSpace="[int?]"
  Title="[string?]" Border="[int?]" 
  ImageParams="[object?]"
  Sizes="[string?]" SrcSet="[IReadOnlyList<IReadOnlyDictionary<string, object?>>?]"
/>
```

Of note, `ImageParams` is expected to be an anonymous object that provides properties to Sitecore used in rendering, e.g. `new { mw = 200 }`. These are also applied to each element in a SrcSet, but the SrcSet property allows overriding them.

`SrcSet` is a list of dictionaries, with each dictionary being the URL query-string arguments to be sent to Sitecore's media handler.

For example, to output the following:

```html
<img
  srcset="/-/media/banner.jpg?mw=480&h=250 480w, /-/media/banner.jpg?mw=800&h=500 800w"
  sizes="(width <= 600px) 480px, 800px"
  src="/-/media/banner.jpg"
  alt="Elva dressed as a fairy" />
```

... you would provide the following content:
```html
@* alt and src properties are drawn from the Sitecore field value *@
<ScImage Field="Model.Banner" SrcSet="@SrcSet" Sizes="(width <= 600px) 480px, 800px" />
```
```cs
@code {
    public class ViewModel {
        public ImageField Banner { get; set; }
    }

    // each dictionary becomes an element in the srcset
    // the w/mw/width/maxWidth properties are extracted and used as the set key (e.g. 480w)
    // the base URL comes from the Field's Src property (unless overridden with a component attribute)
    private IReadOnlyList<IReadOnlyDictionary<string, object?>> SrcSet = new List<Dictionary<string,object?>>() {
        new() { { "w", 480 }, { "h", 250 } },   // becomes 480w
        new() { { "w", 800 }, { "h", 500 } }    // becomes 800w
    };
}
```


## ScLink
Renders an anchor (`<a />`). The label of the anchor will be the content of the element, or the `Text`, `Title` or `Href` properties of the Sitecore field value (in that order of preference).

```html
<ScLink Field="[HyperlinkField]" Editable="[bool=true]"
  Text="[string?]"
  Url="[string?]"
  Anchor="[string?]"
  Title="[string?]"
  Class="[string?]"
  Target="[string?]"
  QueryString="[string?]"
  Rel="[string?]"
></ScLink>
```

Any parameter you supply takes precedence over the corresponding value on the field. Provide `ChildContent` (the element body) to render custom markup in place of the field's text.

## ScNumber
Renders a numeric value or nothing.

```html
<ScNumber Field="[DecimalField]" Editable="[bool=true]"
  Culture="[string?]"
  NumberFormat="[string?]"
/>
```

`NumberFormat` is a standard or custom .NET numeric format string.

## ScPlaceholder
Renders each Sitecore component placed inside the named placeholder, in the order they were authored.

```html
<ScPlaceholder Name="[string]" Editable="[bool=true]" ParentId="[string?]" />
```

`Name` is the placeholder key as defined in your Sitecore layout. `ParentId` is needed when the placeholder lives inside a rendering that contains an interactive component, so that Blazor can correctly transition state between static and interactive modes. For route-level placeholders you can leave `ParentId` blank.

## ScRichText
Renders the Sitecore Field value verbatim, as a `MarkupString`, or nothing.

```html
<ScRichText Field="[StringField]" Editable="[bool=true]" />
```

## ScText
Renders the Sitecore Field value as escaped text, or nothing. When `ConvertNewLines` is `true` (the default) line breaks (`\r` or `\r\n`) are converted to `<br />` and the content is rendered using a `MarkupString`.

```html
<ScText Field="[StringField]" Editable="[bool=true]"
  ConvertNewLines="[bool=true]"
/>
```
