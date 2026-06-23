# Working with GraphQL
[&lt; Docs index](index.md)

> [!NOTE]
> **The Starter Kit does not ship the GraphQL companion project.** This Starter Kit does not includes a `Generate-GraphQL.ps1` code-generation script. The content below describes the capability as it works in the SDK and how to set it up if you want it; none of it is pre-wired for you. If you don't need GraphQL queries, skip this page.

Most of the time you don't need to think about GraphQL at all - the SDK fetches the layout for a page, binds it to your component models, and you work with strongly-typed fields. But sometimes you need data that isn't part of the current page's layout: a navigation tree, a breadcrumb trail, a list of related articles, a global settings item. For those cases the SDK gives you a typed GraphQL client over Sitecore Experience Edge, and you can add a code-generation workflow that turns your Sitecore schema into C# query classes.

## The two pieces

| Piece | What it is |
| - | - |
| A GraphQL class library | A class library of generated C# types - one per Sitecore template and Edge type - that lets you build queries in a fluent, compile-checked way. You create this project yourself, or add the generated files into your main project. |
| A generation script | A script that regenerates the library from your Sitecore Edge endpoint. |
| `ISitecoreGraphQL` | The SDK service you inject to execute a query and read the result. |

The generated project targets `netstandard2.1` and depends on `PolyglotDataStudio.GraphQL`, which supplies the fluent query base types (`Query`, `GraphQLObject<T>`, fragments and so on).

## Generating the client

The generator is a global .NET tool. Install (or update) it once:

```cmd
dotnet tool install --global polyglotdatastudio.graphql.cli
dotnet tool update --global polyglotdatastudio.graphql.cli
```

Then invoke it from the solution root, pointing it at your Sitecore Edge GraphQL endpoint. The key arguments are:

- `-u` - the Edge GraphQL endpoint, e.g. `https://[environment].sitecorecloud.io/sitecore/api/graph/edge`
- `-h` - request headers. Supply an `sc_apikey` header with your API key. For security, read it from an environment variable or prompt rather than hard-coding it.
- `-n` - the namespace for the generated classes (e.g. `BlazorStarter.GraphQL`)
- `-o` - the output folder
- `--force` - overwrite existing generated files
- `--public` - generate public types

Before running, update the endpoint URL to match your own environment.

A sample script you can run whenever you make template changes in your system is included that wraps up the above call. Find it in the solution root at `Generate-GraphQL.ps1`.

> [!NOTE]
> The generated files should be committed to the repo so the solution builds without anyone needing the tool. Regenerate (and commit the result) whenever your Sitecore templates change in a way you want to query - new templates, new fields, renamed fields.

## Executing a query

Inject `ISitecoreGraphQL`, build a `Query`, execute it, then read the typed result. The fluent builder mirrors the GraphQL shape: `item(...)` selects an item, `.WithFields(...)` selects fields, `.WithFragment(...)` applies a fragment, and `.WithAlias(...)` names a result so you can pull it back out.

Here is an example breadcrumb query pattern:

```csharp
public async Task<Breadcrumb.ViewModel> BreadcrumbNavigation( string path )
{
    var query = new Query();

    query.item( Lang, path )
            .WithFragment( "breadcrumbFields" )
            .WithFields( i => i.ancestors( hasLayout: true )
                .WithFragment( "breadcrumbFields" ) );

    query.AddFragment<Item>( "breadcrumbFields" )
            .WithFields( i => i.url.WithFields( u => u.path ) )
            .WithFragment<_PageContent>()
                .WithFields( p => p.breadcrumbText.WithFields( t => t.value ) );

    await ScGql.Execute( query ).ConfigureAwait( false );

    var item = query.GetResult<C___PageContentModel>();

    return MapBreadcrumbViewModel( item );
}
```

The pattern is always the same:

1. `new Query()` and describe the shape you want with `item(...)` / `.WithFields(...)` / fragments.
2. `await ScGql.Execute( query )` to run it against Edge.
3. `query.GetResult<TModel>()` (optionally passing an alias) to materialise the typed result.

Types without the `Model` suffix (e.g. `_PageContent`, `_Navigation`) are used for building queries; types suffixed with `Model` are generated models used for parsing the results. When you query a Sitecore field you usually drill into its `value` (`.WithFields( c => c.value )`), because ExperienceEdge returns field values as objects with a `value` (and metadata) rather than bare scalars.

>[!NOTE]
> GraphQL types and interfaces are emitted from Sitecore Templates and must be unique. Unfortunately, Sitecore does not place any uniqueness constraints on Template names, which results
> in considerable duplication across the system. In an effort to disambiguate class names, Sitecore sometimes goes to extreme lengths, with GUID suffixes and `C_`, `C__`, and `C___` prefixes in its
> set of strategies. If you are trying to use a Sitecore Template called "PageContent" you can be sure to find several classes that match.
>
> Hover the class name in your editor and the tooltip will show you the full path to the Template and its ID, which are embedded by the generator.

## Adding a new query

To query something new:

1. **Make sure the types exist.** If you're querying templates or fields that already existed when the client was last generated, the C# types are already there. If you've added templates or fields in Sitecore, rerun the generator to regenerate the client.
2. **Write a method** (a repository class is the natural home) that injects `ISitecoreGraphQL`, builds the `Query` with the fluent API, executes it and maps the result into a view model your components can consume.
3. **Cache if appropriate.** Global data such as navigation is loaded once at startup and rebuilt on cache-clear. Subscribe to `ICacheEvictor.OnCacheCleared` so your cached data refreshes when content is republished - see [Caching and content sync](caching-sync.md).
4. **Consume it from a component** by injecting your repository and calling the method.

## When to use GraphQL vs. model binding

- **Use model binding** (inherit `SitecoreComponent<TModel>`, declare fields) for everything that is part of the page the visitor requested. This is the overwhelming majority of your components and needs no GraphQL at all.
- **Use a GraphQL query** for cross-cutting data that isn't on the current page: site-wide navigation, breadcrumbs built from ancestors, "related content" lists, global configuration items.

## Next steps

Continue with [Forms](forms.md).
