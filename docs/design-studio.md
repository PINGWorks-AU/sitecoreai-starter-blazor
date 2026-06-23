# Design Studio support
[&lt; Docs index](index.md)

> [!NOTE]
> **Stub - documentation in progress.** Design Studio integration is supported by the SDK and the Starter Kit ships with the generated artifacts already committed under `wwwroot/App_Data/.sitecore`. The feature is opt-in; this page describes the parts involved and how to enable them. Full step-by-step authoring guidance will follow once the feature is past early access.

Sitecore's Design Studio (part of Design Library) is an AI-assisted authoring tool that understands the components your rendering head supports. It needs a machine-readable description of your Blazor components - their names, variants and field shapes - so it can generate page layouts and suggest compositions. The Blazor SDK gives you two things to support this:

1. A **build-time code extraction step** that reflects your assembly and emits React component approximations into `wwwroot/App_Data/.sitecore`.
2. A **startup upload service** that pushes those artifacts to Sitecore's mesh endpoint (changed files only) so Design Studio stays in sync with your codebase.

## The two steps

### Step 1 - build-time code extraction

The extraction step is a MSBuild target that runs after each build. It reflects the compiled head assembly and produces two kinds of artifact:

- **`wwwroot/App_Data/.sitecore/components/*.tsx`** - one file per Sitecore component, containing a React approximation of the component's interface (props, rendering-variant names).
- **`wwwroot/App_Data/.sitecore/component-map.ts`** - a manifest listing every component and the path to its artifact.
- **`wwwroot/App_Data/.sitecore/package.json`** - a package descriptor consumed by Design Studio.

These files are checked in to the repository and ship with the application. They're the same files you can see in `apps/SitecoreAI Blazor Starter/wwwroot/App_Data/.sitecore` in the Starter Kit.

Enable the extraction step by setting the `EnableDesignStudioSupport` MSBuild property in your project file:

```xml
<PropertyGroup>
    <EnableDesignStudioSupport>true</EnableDesignStudioSupport>
</PropertyGroup>
```

With this set, building the project runs the `PingWorksGenerateSitecoreDesignArtifacts` target. The output directory defaults to `wwwroot/App_Data/.sitecore` relative to your project root; override it by setting `SitecoreDesignOutputDir`.

You don't need to enable this property just to use the already-generated artifacts that ship in the Starter Kit. Enable it when you add new components or modify existing ones and want to regenerate the artifacts.

### Step 2 - startup upload

The upload service runs at application startup and pushes the generated artifacts to Sitecore's mesh endpoint. It performs change detection (using a SHA-256 hash of each file's content plus the rendering-host label) so only changed files are uploaded.

Enable upload with the `SitecoreSettings.DesignStudio.EnableUpload` setting:

```json
"SitecoreSettings": {
    "DesignStudio": {
        "EnableUpload": true,
        "ClientId": "[external-editing-host-client-id]",
        "ClientSecret": "[external-editing-host-client-secret]",
        "OrganizationId": "org_xxx",
        "TenantId": "[your-tenant-id]",
        "RenderingHost": "[optional-label]"
    }
}
```

Store `ClientId` and `ClientSecret` in User Secrets (development) or Key Vault (production) - not in `appsettings.json`.

When `EnableUpload` is `true` and credentials are provided, the upload runs in the background on startup. Log entries report how many files were uploaded and whether the upload succeeded. If credentials are missing or incomplete the upload is skipped with a warning.

## Settings reference

See [Configuration settings - SitecoreSettings.DesignStudio](settings.md#sitecoresettingsdesignstudio) for the full table.

The key settings:

| Setting | Purpose |
| - | - |
| `EnableUpload` | Master switch for the startup upload. Default `false`. |
| `ClientId` / `ClientSecret` | OAuth credentials for the Sitecore mesh endpoint. |
| `OrganizationId` / `TenantId` | Required for the client-credentials token request. |
| `ArtifactsPath` | Where the extraction step writes (and the upload service reads from). Default `wwwroot/App_Data/.sitecore`. |
| `RenderingHost` | Optional label attached to uploaded files - useful when multiple rendering heads share an Edge tenant. |
| `Origin` | CORS origin restriction for Design Studio. Default `https://designlibrary.sitecorecloud.io/`. |

## Checking what was generated

The `wwwroot/App_Data/.sitecore/components/` directory in the Starter Kit already contains the extracted artifacts for the Starter Kit's built-in components. Open any `.tsx` file to see what the generator produced: each file exports a React component approximation with the props the generator inferred from your Blazor model, plus any rendering-variant names it found. The `component-map.ts` at the root of the directory lists every component by the name Sitecore knows it by.

## The Tools page

The built-in Tools page (at `/.tools`) includes a **Design Studio** tab when `DesignStudio` settings are configured. It shows the status of the last upload and lets you trigger a re-upload on demand. The tab is only visible when the `DesignStudio.ClientId` and related credentials are present; without them it does not appear.
