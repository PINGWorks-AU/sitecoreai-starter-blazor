# Client-side script with `ComponentScript`
[&lt; Docs index](index.md)

Most components are pure markup, but some need a little JavaScript: a carousel, an accordion, a map, a third-party widget, a form that hosts its own script. In a traditional MVC site you'd drop a `<script>` tag next to the markup and be done. In a Blazor Web App that approach quietly breaks, and the reason is **enhanced navigation**. The SDK's `ComponentScript` component exists to make per-component JavaScript work correctly anyway.

## Why a plain `<script>` tag isn't enough

Sitecore pages in this SDK are served with static server-side rendering, and Blazor stitches navigations together with *enhanced navigation*: when a visitor moves from one page to another, Blazor fetches the new page and **patches the existing DOM** rather than triggering a full browser reload. The page never unloads, so the browser never re-runs `<script>` tags, and `DOMContentLoaded` never fires again.

That creates three problems for component scripts:

- **Scripts don't re-run on navigation.** A `<script>` that wired up a carousel on first load won't run when the visitor navigates to another page that also has a carousel - the new carousel stays dead.
- **Handlers leak or duplicate.** If a script attaches event listeners to elements that are later replaced by a DOM patch, you can end up with stale handlers, duplicated handlers, or listeners bound to elements that no longer exist.
- **No teardown hook.** When a component is patched out of the page there's no natural place to clean up timers, observers or global listeners it created.

`ComponentScript` solves all three by giving your script a proper lifecycle that is aware of enhanced navigation.

## How `ComponentScript` works

You place the component in your markup and point it at a JavaScript module:

```razor
<ComponentScript Src="./UI/Components/MyComponent.razor.js" Arg="@Id" />
```

- `Src` (required) is the path to an ES module. A leading `./` is resolved relative to the site's base URI.
- `Arg` (optional) is a string passed to your module's lifecycle functions - typically the component's `Id`, so a script can scope itself to one specific instance on the page.

Your module exports up to three lifecycle functions, all optional:

```js
export function onLoad(arg)    { /* module imported for the first time */ }
export function onUpdate(arg)  { /* the component appeared or the page changed */ }
export function onDispose(arg) { /* the component is gone from the page */ }
```

Behind the scenes the SDK ships a Blazor JS initializer that registers a `<component-script>` custom element and listens for Blazor's `enhancedload` event. The behaviour is:

- **First use of a `Src`+`Arg`:** the module is dynamically imported, then `onLoad(arg)` and `onUpdate(arg)` are called.
- **Subsequent uses of the same `Src`+`Arg`:** the module is *not* re-imported; `onUpdate(arg)` is called again. Instances are reference-counted, so the same module is loaded only once even if several components use it.
- **After every enhanced navigation:** `onUpdate(arg)` is called on every still-present module, and `onDispose(arg)` is called for any module whose components have all left the page (after which it is forgotten).

The practical division of labour is:

| Hook | Runs | Put here |
| - | - | - |
| `onLoad` | once, when the module is first imported | one-time setup that should survive navigation |
| `onUpdate` | on first load **and** after each enhanced navigation while the component is present | (re)binding to DOM that may have just been patched in |
| `onDispose` | when the component is no longer anywhere on the page | cleanup of timers, observers and global listeners |

Because `onUpdate` runs again after each navigation, it's the right place to attach handlers to elements the SDK may have just re-rendered. If your setup is idempotent (or scoped by `Arg`), `onUpdate` is often the only hook you need.

## Worked example

The Starter Kit's `Accordion` component is a clean example. The markup drops a single `ComponentScript` alongside the rendered accordion:

```razor
@inherits SitecoreComponent<Accordion.ViewModel>
<ComponentScript Src="./UI/Components/Accordion.razor.js" />

<section class="services-accordion">
    @* ...accordion markup... *@
</section>
```

and `UI/Components/Accordion.razor.js` rebinds in `onUpdate` and cleans up in `onDispose`. The key detail is that it **remembers the handlers it attached** (in a `Map`) so it can remove exactly those handlers later - the canonical way to avoid the leaked/duplicated handlers described above:

```js
let listItems = {};
let allTabs = {};
let eventListeners = new Map();

export function onUpdate() {
    // runs on first load and after every enhanced navigation
    listItems = document.querySelectorAll(".accordion__label");
    allTabs = document.querySelectorAll(".services-accordion__img");

    eventListeners = new Map();
    for (let i = 0; i < listItems.length; i++) {
        const handler = () => {
            toggleActiveClass(listItems[i]);
            toggleMaps(listItems[i].dataset.class);
        };
        eventListeners.set(i, handler);          // remember it so we can remove it
        listItems[i].addEventListener("click", handler);
    }
}

export function onDispose() {
    // the component has left the page - detach the handlers we added
    for (let i = 0; i < listItems.length; i++)
        listItems[i].removeEventListener("click", eventListeners.get(i));

    eventListeners.clear();
    listItems = {};
    allTabs = {};
}

function toggleActiveClass(active) { /* ... */ }
function toggleMaps(dataClass)     { /* ... */ }
```

`onUpdate` does the binding because the accordion's markup may have just been patched into the page by an enhanced navigation; `onDispose` runs only once the accordion is no longer present anywhere, and uses the remembered handler references to detach cleanly. Holding the handlers in module-level state (`listItems`, `eventListeners`) is what makes the matched `addEventListener` / `removeEventListener` pair possible - an anonymous inline handler could never be removed.

### Scoping to one instance with `Arg`

When a page can contain more than one instance of a component, query within that instance rather than across the whole document. Pass the component `Id` as `Arg` and have your script narrow its work to that element:

```razor
<div id="@Id" class="carousel">
    @* ...slides... *@
</div>
<ComponentScript Src="./UI/Components/Carousel.razor.js" Arg="@Id" />
```

```js
const controllers = new Map();

export function onUpdate(id) {
    const root = document.getElementById(id);
    if (!root || controllers.has(id))
        return;                      // already initialised this instance

    const controller = new AbortController();
    root.querySelectorAll(".next").forEach(btn =>
        btn.addEventListener("click", advance, { signal: controller.signal }));

    controllers.set(id, controller);
}

export function onDispose(id) {
    controllers.get(id)?.abort();    // remove this instance's listeners
    controllers.delete(id);
}

function advance(e) { /* ... */ }
```

Here `onUpdate` re-checks after every navigation but only initialises an instance once, and `onDispose` tears down exactly the listeners that instance created. Using an `AbortController` as the listener `signal` makes teardown a single call.

### Re-running third-party scripts on navigation

`onUpdate` is also where you re-trigger third-party widgets that expect a fresh page. The Starter Kit's `Form` component uses it to reset Google reCAPTCHA each time the form is shown after an enhanced navigation, falling back to re-injecting the form's own script when needed:

```js
export function onUpdate(id) {
    if (typeof grecaptcha !== 'undefined') {
        try { grecaptcha.reset(); }
        catch (e) { reloadFormScript(id); }
    } else {
        reloadFormScript(id);
    }
}
```

## Guidance

- **Reach for `ComponentScript` instead of inline `<script>`** for anything that has to survive navigation or attach event handlers. Inline scripts are fine only for content that never changes and never needs to re-run.
- **Prefer `onUpdate` for DOM binding**, because it runs after the DOM has been patched on each navigation. Use `onLoad` for genuinely one-time work and `onDispose` for cleanup.
- **Scope with `Arg`** when more than one instance can appear on a page, and guard `onUpdate` so re-initialisation is idempotent.
- **Keep modules side-effect-light at import time.** Do your work in the lifecycle functions, not at the top level of the module, so the reference-counting and navigation hooks stay in control.
- **Co-locate the script** with its component as `<ComponentName>.razor.js` (the Starter Kit convention) so the pair is easy to find.
