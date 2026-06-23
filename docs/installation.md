# Installation
[&lt; Docs index](index.md)

The Starter Kit is designed to provide a worked example with some skeleton code to reduce the effort required to start working with Blazor and SitecoreAI. Create a new GitHub or Azure DevOps repository and copy the contents of this kit into your new repo. You will need **all** the files and folders to ensure SitecoreAI works correctly, even though you will only be customising the project under `/apps/SitecoreAI Blazor Starter`.

> [!NOTE]
> **Do I need to copy the /docs too?**<br />Strictly speaking no, but you might find it helpful from time to time, particularly when onboarding new developers to your project.

## Licensing

BlazorSDK is a licensed product, but we are giving out free trials and you only pay for production environments. The license applies on a per-EdgeContextId basis which is Project+Environment. You can get a license by reaching out to us at licensing@ping-works.com.au.

**DO NOT SEND YOUR EDGE CONTEXT ID** in fact, you should never expose this anywhere.

To license the SDK we would like a hash of the Edge Context ID. Use PowerShell (or other) to generate it like this:

```powershell
$string = "YourEdgeContextId"
$bytes  = [System.Text.Encoding]::UTF8.GetBytes($string)
$stream = [System.IO.MemoryStream]::new($bytes)
(Get-FileHash -InputStream $stream -Algorithm SHA256).Hash
```

Send the hash value, and whether you are using it in a production or non-production context and we'll send you back a license file. The file should live in the root of your Blazor project (alongside `Program.cs`), and should be marked as `CopyToOutputDirectory=Always`.

## Update names and properties

Update the following items to suit your project:

- `/SitecoreAI Blazor Starter.slnx` - the Solution name
- `/apps/SitecoreAI Blazor Starter` - path to the Blazor project (rename to something meaningful for your site)
- `/apps/SitecoreAI Blazor Starter/SitecoreAI Blazor Starter.csproj` - the Blazor project file name
- `/apps/SitecoreAI Blazor Starter/UI/App.razor` - if you've renamed the assembly, update the component-scoped CSS reference (`SitecoreAI_Blazor_Starter.styles.css` by default) so the bundle loads correctly

If you open the csproj file, you will also note that the `<RootNamespace />` and `<AssemblyName />` should be updated to match your renamed project.

## Check for updated dependencies

The Starter Kit has a number of dependencies that may have been updated since the kit was created. You can check and update the following as needed. Updating to new minor versions is generally safe. If you encounter an issue, or discover new major versions, please let us know.

### /sitecore.json

This file contains versions of nuget packages used by SitecoreAI Pages. Check for new versions with the command:

```cmd
dotnet package search Sitecore.DevEx.Extensibility --source https://nuget.sitecore.com/resources/v3/index.json
```

Update the version numbers in the file as needed.

### /.config/dotnet-tools.json

This file contains the version of the SitecoreAI Blazor SDK CLI tool. Check for new versions with the command:

```
dotnet package search Sitecore.CLI --source https://nuget.sitecore.com/resources/v3/index.json
```

Update the version numbers in the file as needed.

### Blazor project

Update package dependencies as needed. The Starter Kit relies on:

- `PINGWorks.SitecoreAI.BlazorSDK` - the SDK itself; you need to get this from PING Works for now. Contact licensing@ping-works.com.au.

### Update settings

#### appsettings.json

Review the [Configuration settings](settings.md) documentation for the full list of available configuration, but as a minimum you should include the following.

```json
{
    "SitecoreSettings": {
        "SiteName": "website",
        "DefaultLanguage": "en",

        /* Default language is implicitly allowed */
        "AllowedLanguages": [],

        "EnableEditingMode": true,
        "EnvironmentHostName": "[YOUR_AUTHORING_HOSTNAME].sitecorecloud.io",

        "DefaultPageTemplate": "Page",

        "AuthoringHosts": []
    },

    "SyncSettings": {
        "CachingMode": "MemoryAndDisk",
        "DataPath": "./App_Data",
        "StoreCompressed": true,
        "DiskCacheTimeout": "1.00:00:00",
        "MemoryCacheTimeout": "01:00:00",

        "ChangeFeed": {
            "AutoCreateEdgeWebhook": true,
            "WebhookReceiverBase": "https://[YOUR_PUBLIC_URL]/"
        },
        "PreloadPaths": [ "/" ]
    },

    "ComponentResolverSettings": {
        "SearchNamespaces": [ "*" ]
    }
}
```

#### User Secrets

DO NOT STORE SECRETS IN CONFIG FILES OR SOURCE CONTROL!

Right-click your project in the Visual Studio Solution Explorer and select "Manage User Secrets". 

```json
{
    "SitecoreSettings": {
        "EditingSecret": "[SITECORE_EDITING_SECRET]",
        "EdgeContextId": "[SITECORE_EDGE_CONTEXT_ID]"
    },

    "SyncSettings": {
        "ChangeFeed": {
            "ClientID": "[ORG_OR_ENV_CREDENTIALS_CLIENT_ID]",
            "ClientSecret": "[ORG_OR_ENV_CREDENTIALS_CLIENT_SECRET]"
        }
    }
}
```
You can get these values from the following locations:

- SitecoreSettings - these values come from the Sitecore Deploy portal (https://deploy.sitecorecloud.io). Navigate to 'Projects' and then the 'Environment' you want to connect to, and switch to the 'Developer settings' tab. Set the 'SDK Version' to `Content SDK` and the 'Context' to `Preview`.
  - EditingSecret - the value is listed under `SITECORE_EDITING_SECRET`
  - EdgeContextId - the value is listed under `SITECORE_EDGE_CONTEXT_ID`

- SyncSettings.ChangeFeed - these values allow the SDK to manage a webhook subscription for detecting content changes. In the Sitecore Deploy portal, navigate to 'Credentials' and create a new set of `Edge administration` credentials at either the Environment or Organization scope, as required (Environment is recommended).
  - ClientID - the value is listed under `Client ID`
  - ClientSecret - the value is listed under `Client Secret`

## Installing from scratch

If you don't want to use the Blazor template included in the Starter Kit, there's actually only a few steps needed to enable the functionality in any template.

1. Ensure you are using .NET 10 - the BlazorSDK is built for this version
1. Ensure your project template is 'server-side' - the BlazorSDK will not run entirely in WASM as it requires secure keys and disk access that are not safe from within a user's browser
1. Add the package `PINGWorks.SitecoreAI.BlazorSDK`
1. Register services and routes in `Program.cs`
    1. Register services with `builder.Services.AddSitecoreBlazorSDK( builder.Configuration )` before other services
    1. Disable default Blazor Server CORS - replace `.AddInteractiveServerRenderMode()` with `.AddInteractiveServerRenderMode( opt => opt.ContentSecurityFrameAncestorsPolicy = null )` as the SDK module will create CSP policies to work with Sitecore Pages
    1. After `.AddInteractiveServerRenderMode( ... )` you must call `.AddAdditionalAssemblies( typeof( SitecorePage ).Assembly )` which makes the routes and components from the SDK available to the application
    1. Register middleware and routes with `app.UseSitecoreBlazorSDK()` after `MapRazorComponents<App>()`
    1. Add `app.UseRequestLocalization()` before `UseAntiforgery()` so the SDK's language resolver runs on every request
1. Set up error pages with `app.UseStatusCodePagesWithReExecute( "/status-{0}", createScopeForStatusCodePages: true )` and an exception handler that re-executes to the same route (see [Error handling](error-handling.md))
1. Update settings as described above
1. In `App.razor` add the following section outlets after `<HeadOutlet />`:
    1. `<SectionOutlet SectionName="HeadLayout" />`
    1. `<SectionOutlet SectionName="HeadPage" />`
    1. `<SectionOutlet SectionName="SitecoreEditingScripts" />`
1. In `App.razor` add the following section outlets before the end body tag `</body>`:
    1. `<SectionOutlet SectionName="ScriptLayout" />`
    1. `<SectionOutlet SectionName="ScriptPage" />`
1. Replace the standard `<Router>` in `Routes.razor` with the SDK's `<SitecoreRouter>` so that unmatched URLs fall through to Sitecore content
1. In `MainLayout.razor` (the layout used for all Sitecore pages), add a section content as follows:
    ```html
    <SectionContent SectionName="HeadLayout">
        <!-- Add any other head content that should apply to all Sitecore pages -->
    </SectionContent>
    ```

`SitecoreEditingScripts` is rendered automatically by the SDK while editing - you do not need to opt-in to it explicitly inside your layout.

## Next steps

Continue with [Pages, Templates and Components](components.md)
