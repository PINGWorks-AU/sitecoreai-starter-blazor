# SitecoreAI Blazor Starter Kit

This repository contains a Blazor starter kit for SitecoreAI development. Fork this repository or copy the pertinent components into your own repository when beginning a new SitecoreAI project that uses a Blazor rendering host.

## Architecture

The runtime uses Sitecore's GraphQL endpoints and the Admin API to get content and actively monitor the site for updates. AppSettings specify the site and synchronisation engine settings. By default the runtime will cache content into a path on the local disk to improve performance and reduce load on the Sitecore Edge, but this can be disabled if you prefer.

The runtime uses the Admin API to automatically create a webhook with the `OnUpdate` execution mode to evict the local cache when pages are published in Sitecore. The webhook receiver registers a default route of `webhook/ee` for the `POST` method - be careful not to overlap this with a path in your public-facing application. The path can be changed through AppSettings if required.

## Hosting

This approach uses a custom rendering host for SitecoreAI, and does not rely on the deployment processes built into Sitecore, nor is there any dependency on Vercel. The simplest approach is to publish your environment to an Azure App Service.

Runtime components of the Blazor rendering host connect to Sitecore via GraphQL and API services, and therefore require sensitive keys to be added to configuration files, and uses the file system for caching. For these reasons **you should not use a Blazor WebAssembly app as a rendering host** since all content will be accessible to visitors in their browser, and you will need a .NET runtime based hosting environment (either Windows or Linux) rather than simple flat-file hosting.

While the rendering app itself is a server-side application, individual interactive components can still be rendered in WASM mode if desired. The application default rendering mode is SSR - static server rendering with enhanced navigation.

> [!NOTE]
> Do not configure the Blazor rendering host project to be deployed to SitecoreAI in the `xmcloud.build.json` file's `buildTargets` section. This is **ONLY** for the authoring app, which is really just a placeholder at this point.

## Blazor Modes

The starter kit begins with the Blazor Web App project template targeting the .Net 10 runtime. Components are Static Server-Side Rendered by default. If your components require interactivity you may set the mode on a per-component basis using the `@rendermode` directive.

See [Blazor Render modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes) documentation for configuring render modes.

## Prerequisites

- Configured SitecoreAI Project and Environment

This template is designed to work with Visual Studio 2026, but should be adaptable to Ryder, Visual Studio 2022 or VS Code if preferred.

## Getting Started

1. Create a new repository in ADO or GitHub for your code and copy the contents of this Starter Kit to it.
1. Follow the 'Getting started' instructions in Sitecore's documentation - ensure you display the steps for `ASP.NET Core SDK`.
> [!NOTE]
> Set "Auto deploy on push to repository" to `No`. Deployment of this system is not a Sitecore concern and there is no need to re-deploy to Sitecore unless you add new content or template Items you want to import.

3. Once you have created your Site and opened Page Builder, you will notice the error "The remote name could not be resolved: 'blazor-starter'". This is because in the Starter Kit's `/authoring/items/blazor-starter/DefaultRenderingHost/Default.yml` item the rendering host field `ServerSideRenderingEngineApplicationUrl` is hard-coded as `https://blazor-starter`. You will need to update this field, as well as the other URLs, to match the URL of your own rendering host once it is deployed. For now, switch the Pages application to use the "Local host" rendering host so you can continue developing. 
3. Update this project's AppSettings to connect to your Sitecore application (use a User Secrets file for sensitive values).
3. Follow the guidance in this repository for creating new components.

[Consult the additional documentation for more information.](/docs/index.md)

## Getting the SDK

The Blazor SDK is licensed, but free to use on non-production environments. Contact PING Works at licensing@ping-works.com.au to get the SDK and license file needed to build and run this project.

## Disconnected development

Offline (or disconnected) development is not currently supported.
