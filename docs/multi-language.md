# Multi-language support
[&lt; Docs index](index.md)

Sitecore content is naturally multilingual, and the Blazor SDK gives you a complete language-resolution pipeline out of the box. Configure the languages you want to support and the SDK will pick the right one for every request, set the request culture accordingly, and ask Sitecore Experience Edge for content in that language.

## Telling the SDK which languages your site supports

Two settings under `SitecoreSettings` drive language behaviour:

```json
"SitecoreSettings": {
    "DefaultLanguage": "en",
    "AllowedLanguages": [ "en", "fr", "de-DE" ]
}
```

- `DefaultLanguage` is the culture used when no other language can be resolved for a request. It is always implicitly allowed, even if you leave it out of `AllowedLanguages`.
- `AllowedLanguages` is the additional set of languages and cultures your site will accept. They are pre-loaded into the SDK's cache at startup so that the first request in each language is already warm.

Values are **case-sensitive**. Sitecore Edge treats language codes case-sensitively, so list each entry exactly as Sitecore expects it - typically a lowercase two-letter code (`en`, `fr`), or a `lang-REGION` form with lowercase language and uppercase region (`de-DE`, `en-AU`). The SDK validates each entry against `CultureInfo` and skips anything that doesn't resolve.

When the SDK starts, it builds an effective list of cultures from `DefaultLanguage` + `AllowedLanguages`, deduplicated by LCID. That list is used to:

- Pre-load layout responses in every supported language.
- Configure ASP.NET request localisation, so every request runs with `CurrentCulture` and `CurrentUICulture` set to the resolved language.
- Validate incoming requests - languages outside the list are ignored when resolving the request culture.

## How a request language is chosen

The SDK ships a `RequestCultureProvider` that runs at the front of the localisation pipeline. For every request it evaluates the following sources, in this order:

1. **Editing override** - in editing mode only, the `sc_lang` query string parameter (sent by Pages) wins.
1. **URL segment** - the first path segment if it matches an allowed language (e.g. `/fr/about` resolves to `fr`). When this source matches, the language segment is stripped from the path before Sitecore is queried.
1. **`Accept-Language` header** - the standard browser header. Candidates are ordered by descending `q` value, and the first one that matches an allowed language wins.
1. **Default** - falls back to `SitecoreSettings.DefaultLanguage`.

The matcher accepts an exact culture match (`fr-FR`), a language-only match (`fr`), and a related-language match (any allowed culture sharing the same language part). The resolved language is stored on the `HttpContext`, used to set the request culture, and forwarded to Sitecore Edge when fetching the layout.

For this resolution to run, your `Program.cs` must call `app.UseRequestLocalization()`. Place it between `UseHttpsRedirection()` and `UseAntiforgery()`:

```cs
app.UseHttpsRedirection();
app.UseRequestLocalization();
app.UseAntiforgery();
```

`AddSitecoreBlazorSDK` configures `RequestLocalizationOptions` for you; only the `app.UseRequestLocalization()` call needs to be added explicitly.

## Working with the resolved language in your code

Inside a component, the current language is available via `CultureInfo.CurrentUICulture.Name`. The SDK also injects a `Sitecore-Lang` value on the page context for diagnostic purposes.

If you need to make a Sitecore content call manually, take the language from the cascading `SitecoreContext` (`Context.Route?.ItemLanguage`) or from `CultureInfo.CurrentUICulture.Name` - both reflect the language resolved for the request.

## Authoring multilingual pages

There is nothing special you need to do in your Blazor components for multilingual content. The SDK fetches the layout response for the resolved language and binds the localised field values into your model exactly as it would for a single-language site. Components that depend on language-specific resources (for example, hard-coded labels) should use the standard ASP.NET `IStringLocalizer` infrastructure or read from `CultureInfo.CurrentUICulture` directly.

## URL strategies

The two natural URL strategies both work without further configuration:

- **Language prefix** (`/fr/about`) - just enable additional languages in `AllowedLanguages`. The SDK detects the prefix, sets the request language, and strips the prefix before resolving the Sitecore route.
- **Per-domain** - point each domain at the same application, set `AllowedLanguages` to the union of languages you support, and let the `Accept-Language` header (or a domain-specific default) pick the language. If your Sitecore tenant has site-per-domain configuration, the site name itself still comes from `SitecoreSettings.SiteName`; multi-site hosting is a separate concern from language resolution.

## Next steps

Continue with [Error handling](error-handling.md).
