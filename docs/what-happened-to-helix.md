# What happened to Helix?
[&lt; Docs index](index.md)

If you've built a Sitecore MVC solution in the last decade, Helix will be second nature to you, and you may be wondering where it went. The short answer is that the architecture of a headless Blazor solution makes most of Helix unnecessary, but its concepts still echo through the Sitecore content tree, so a quick refresher is worthwhile.

## What Helix was

Helix is a set of design principles and conventions for structuring a Sitecore implementation. It was never a product or a library you installed; it was guidance for keeping a large MVC codebase maintainable as it grew. Helix organised everything into three layers:

- **Foundation** - low-level, stable building blocks that other things depend on (e.g. a base page template, indexing setup, a multisite module). Foundation modules can depend on other Foundation modules.
- **Feature** - discrete pieces of business functionality that a content author would recognise (Navigation, Search, Promo, Carousel). Feature modules depend only on Foundation, preferably not on each other.
- **Project** - the layer that binds features together into an actual website: the page templates, the layout, the site-specific styling and composition.

The big idea was **cohesion within a module and low coupling between modules**. A typical Helix module in MVC bundled a lot of moving parts that all had to agree with each other: a controller, a view, a view model, a repository or Glass Mapper model, the Sitecore templates, the renderings, the placeholder settings, the serialised content items, and the dependency-injection registrations. Keeping all of that together in one module, with strict rules about which layer could reference which, is what made a sprawling MVC solution navigable.

## Why it largely disappears in headless Blazor

In the modern decoupled headless architecture, the code required to render a specific component most closely resembles a "View Rendering" with considerably fewer moving parts than are required in a typical MVC setup. Sitecore config files and serialised content are now delivered to the Authoring Environment through a separate "authoring" project, so these files lose their cohesion to the supporting code. A common Helix module collapses into one or two razor files, making the overhead far outweigh the benefits.

Use of companion projects is still highly recommended for custom SDKs and integrations, shareable business logic, and any other complex implementation.

Put plainly: the elaborate layering existed to manage complexity that the headless model simply removes. There is no controller, no Glass Mapper, no server-rendered MVC view, no per-module DI wiring for the rendering itself. A component that used to span half a dozen files and two or three projects is now a `.razor` file (and perhaps a scoped `.razor.css` or `.razor.js` beside it). When a module shrinks to one or two files, wrapping it in its own project with formal layer rules costs more than it saves.

The serialised content and configuration that used to live alongside a Helix module now lives in the `/authoring` project (see [Starter Kit anatomy](anatomy.md)). Because content serialization is deployed to Sitecore independently of your Blazor code, the old tight coupling between "the code for a feature" and "the items for a feature" no longer holds, and there is nothing to keep cohesive in a single module.

## Where Helix lives on

Even though we don't build our rendering system on top of Helix, vestiges of it remain throughout the SitecoreAI content systems, and it is useful to recognise them:

- **Foundation templates.** Page templates you create in Sitecore often still inherit from Foundation-layer base templates, for example `/sitecore/templates/Foundation/JSS Experience Accelerator/Multisite/Base Page`. Getting this inheritance right is what makes features like rendering variants and multisite behave.
- **The content tree shape.** The familiar layout of items under `/sitecore/content`, `/sitecore/layout`, and `/sitecore/templates` is unchanged, and templates are still frequently grouped along Foundation / Feature / Project lines.
- **Module vocabulary.** Sitecore tooling, the SXA/JSS scaffolding, and a lot of community documentation still talk in Helix terms. Knowing what "Foundation" or "a Feature module" means will save you a lot of head-scratching when you read it.

## How to think about our implementation

In Helix terms, our entire implementation is effectively a set of **Project Modules**. The website project composes features into a working site; it does not try to reproduce the Foundation / Feature / Project separation in code. The roles that Foundation and Feature layers used to play are now filled by:

- the **Blazor SDK** (`PINGWorks.SitecoreAI.BlazorSDK`), which provides the stable, reusable plumbing a Foundation layer once did - routing, model binding, field components, caching, editing, analytics, and
- **companion projects** where genuine reuse or complexity justifies them, for example a custom integration SDK or a library of shareable business logic.

## Practical guidance

- **Don't recreate Helix layering for its own sake.** A folder convention inside the website project (the `UI/Components`, `UI/Templates`, `UI/Layouts` split described in [Pages, Templates and Components](components.md)) is enough structure for most sites.
- **Do reach for a companion project when it earns its place.** Custom SDKs, third-party integrations, code shared across multiple sites, and anything with significant logic or its own test surface all benefit from living in their own project. The line to watch is reuse and complexity, not ceremony.
- **Keep the authoring artifacts in the authoring project.** Templates, renderings, placeholder settings and serialised content belong under `/authoring`, deployed via Sitecore Content Serialization, not bundled with your Razor code.
- **Lean on the SDK as your Foundation.** Before you write plumbing, check whether the SDK already provides it. Most of what a Foundation layer used to supply is now in the box.

If you remember one thing: Helix solved a complexity problem that headless mostly dissolves. Understand the concepts so you can read Sitecore's content tree and its documentation fluently, then build the simplest thing that works.
