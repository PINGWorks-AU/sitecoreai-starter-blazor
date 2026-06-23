# Starter Kit Project Anatomy
[&lt; Docs index](index.md)

The Starter Kit is delivered as a complete solution that you copy into your own repository. The layout below describes everything you get out of the box, why each piece is present, and which bits you are likely to touch as you build out your site.

|Path|Description|
|-|-|
| `/.config/dotnet-tools.json` | Lists tools required by this solution, in particular the Sitecore CLI tool used to push items to your authoring environment. |
| `/.sitecore/schemas/*.schema.json` | JSON schema files referenced by the Sitecore Content Serialization (SCS) configuration. They give you IntelliSense in `sitecore.json` and the module files under `/authoring/items`. |
| `/apps/SitecoreAI Blazor Starter` | The Blazor web application. This is the project you customise. |
| /authoring/items/* | Sitecore Content Serialization items that should be deployed to the authoring environment. We include: <ul><li>The URLs used by the Pages application for the Default Rendering Host.</li><li>A Datasource Resolver targeting an Item <strong>and</strong> Children as opposed to the standard Children only Resolver </li> |
| `/authoring/platform/*` | Minimal shell project to satisfy Sitecore's requirement to build something during Project provisioning. |
| `/docs/*` | Documentation and help for using this Starter Kit (you are reading it now). |
| `/Generate-GraphQL.ps1` | Invokes the `PolyglotDataStudio.GraphQL.CLI` tool to generate supporting classes for Sitecore's exposed types. |
| `/nuget.config` | Adds a connection to Sitecore's package feed for dependent packages of the form `Sitecore.*` |
| `/sitecore.json` | Sitecore Content Serializer instructions for deploying items from the solution. Plugins copied from Sitecore's [xmcloud-starter-js](https://github.com/Sitecore/xmcloud-starter-js/blob/main/sitecore.json) project. |
| `/xmcloud.build.json` | SitecoreAI build pipeline configuration. |
| `/SitecoreAI Blazor Starter.slnx` | Solution file. References the Blazor app project and the Blazor SDK. |

Sitecore's project setup process insists upon a Build step, even though this is not required by our setup. The Starter Kit therefore includes a minimal .NET project under `/authoring/platform` to satisfy this requirement.

## Inside `/apps/SitecoreAI Blazor Starter`

The website project itself is organised by responsibility rather than by file type, which makes it easier to find related files as the application grows.

|Path|Description|
|-|-|
| `App_Data/` | Working folder used by the SDK for on-disk cache data. Excluded from the project and from source control. |
| `Properties/` | Standard ASP.NET launch settings. |
| `UI/` | All Razor markup. See below for the convention used inside this folder. |
| `wwwroot/` | Static assets shipped with the app (stylesheets, images, web fonts, and so on). |
| `Program.cs` | The application bootstrap. Wires up the SDK, the antiforgery service and the Razor components. |
| `appsettings.json` | Non-secret configuration. See [Configuration settings](settings.md). |

## Inside `/apps/SitecoreAI Blazor Starter/UI`

The `UI` folder follows a simple location convention but you do not need to follow this:

|Path|Description|
|-|-|
| `Components/` | Blazor components mapped to Sitecore UI components (Rich Text, Navigation, Promo, and so on). |
| `Layouts/` | Standard Blazor layouts (`@inherits LayoutComponentBase`). `MainLayout.razor` is used by default for every Sitecore-routed page. |
| `Pages/` | Pages reached through explicit Blazor routes (`@page`), which override Sitecore's fallback route. The Starter Kit ships with `Status.razor` for error pages; the SDK contributes the built-in [Tools page](tools.md) at `/.tools`. |
| `Templates/` | Blazor components mapped to Sitecore page templates. `Page.razor` is the default and is selected when no template-specific component is found. |
| `App.razor` | The root document used for every request. See [Routing](routing.md). |
| `Routes.razor` | Hosts the SDK's `<SitecoreRouter>` and the `MainLayout` default. |
| `_Imports.razor` | Pulls in the namespaces every component file relies on, including the SDK's `Components`, `Binding`, `Data` and `Services` namespaces. |
