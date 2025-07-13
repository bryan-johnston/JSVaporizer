# JSVaporizer

***An instance of `DemoApp` is currently deployed at https://jsvaporizer-poc.azurewebsites.net/.***

**JSVaporizer** is an experimental runtime + toolchain that lets you build interactive web UIs entirely in C#.
All DOM access, event handling, and templating logic execute in WebAssembly; the browser receives only a minimal, generic JavaScript “shim.”
The goal is to **“vaporise”** most application‑specific JavaScript without sacrificing performance or flexibility.

---

## Key Objectives

| Objective                           | Approach                                                                                                |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------- |
| **Minimise handwritten JavaScript** | C# wrappers (`Window`, `Document`, `Element`, …) mirror the DOM.                                        |
| **Fast WASM ↔ JS inter‑op**         | Function & object **pools**: delegates and handles are stored once and referenced by small integer IDs. |
| **Light runtime payload**           | Ship only a handful of thin shims (`window.js`, `element.js`, `jsvwasm.js`, …).                         |
| **Security by default**             | Sanitize HTML/Handlebars templates on the managed side (`HtmlSafety`, `HandlebarsSafety`).              |
| **Declarative components**          | Convert templates to fluent builder APIs (`JSVComponent`, `JSVCompBuilder`, `JSVTransformer`).          |

---

## How It Works – 30‑second Tour

```text
C# (WebAssembly)                       JavaScript (browser)
┌────────────────────┐   thin shims   ┌────────────────────┐
│ Element / Document │  ───────────▶  │ element.js / ...   │
│ Window wrappers    │  ◀───────────  │ js_function_pool.js│
└────────────────────┘   int handles  └────────────────────┘
        ▲                                    ▲
        │ typed DOM API                      │ real DOM
        │                                     │
   Component builders                   Mutation events
```

1. **Build‑time**

   * Templates are transformed into C# component builders.
   * The project is compiled to WebAssembly; a small JS bundle is copied to *wwwroot*.

2. **Runtime**

   * JavaScript shims initialise object/function pools and create the first handles.
   * Managed wrappers map those handles to strongly‑typed C# objects.
   * DOM changes and events cross the boundary via pool IDs rather than JSON blobs.

---

## Repository Layout (selected files)

| Area                   | Representative files                                                    | Purpose                                                             |
| ---------------------- | ----------------------------------------------------------------------- | ------------------------------------------------------------------- |
| Inter‑op core          | `JSFunctionPool.cs`, `WasmJSVGenericFuncPool.cs`, `js_function_pool.js` | Store delegates/JS functions once and trade integer IDs.            |
| DOM façade             | `Window.cs`, `Document.cs`, `Element.cs` (+ matching JS files)          | Present a .NET‑friendly surface that mirrors the browser DOM.       |
| Event routing          | `WasmJSVEventListenerPool.cs`, `DomMutationEvents.cs`                   | Multiplex many DOM events through a jump table into managed code.   |
| Components & templates | `JSVComponent.cs`, `JSVCompBuilder.cs`, `JSVTransformer.cs`             | Turn Razor‑/Handlebars‑like markup into fluent, type‑safe builders. |
| Security helpers       | `HtmlSafety.cs`, `HandlebarsSafety.cs`                                  | Encode or strip unsafe markup to reduce XSS risk.                   |
| Utilities              | `JsObjectCache.cs`, `Convenience.cs`, `Invokers.cs`, `Exports.cs`       | Handle object identity, convenience wrappers, JS ↔ C# exports.      |

---

## Current Status

* **Proof of concept** – the core plumbing is present, but the API may change without notice.
* **Browser support** – verified in Chromium‑based browsers with WebAssembly enabled.
* **Performance** – early micro‑benchmarks show inter‑op latency well below JSON‑based approaches, though leak‑prevention and batching need work.
* **Documentation** – this README is the primary entry point; inline XML docs are incomplete.

---

## Building & Running

1. Build the `JSVaporizer.NET.8` project first.
1. Then build `DemoComponentLib` and `DemoApp` if you need to.
1. Run `DemoApp` if you'd like. 

---

## Roadmap

* **Component library** – ship a basic set of form controls and layout helpers.
* **Hot‑reload** – investigate Roslyn source generators + WASM dynamic linking.
* **Broader browser testing** – validate on Firefox and Safari.
* **WebGPU/WebGL wrappers** – reuse the inter‑op pools for graphics APIs.
* **Comprehensive docs & samples** – tutorials, API reference, real‑world demos.

---

## Immediate Next Steps (high‑severity issues)

| Area                      | Issue                                                                              | Why It Matters                                                             | Suggested Fix                                                                                                     |
| ------------------------- | ---------------------------------------------------------------------------------- | -------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| **Function Pool**         | Delegates are pinned indefinitely; there is no disposal path.                      | Long‑running sessions leak managed memory and JS callbacks.                | Add explicit `Unregister(int id)` and bulk shutdown method; track outstanding references.                         |
| **Element & DOM Handles** | `Element` holds an `int handle` that is never released from `JsObjectCache`.       | DOM nodes accumulate in JS even after C# objects are GC‑ed → JS heap leak. | Implement `IDisposable` / `Release()` pattern on `Element` and call it from component teardown.                   |
| **Event Listeners**       | No API to remove all listeners for an element when it unmounts.                    | Orphan listeners keep components alive and trigger unexpected callbacks.   | Provide `RemoveAllListeners(int elementHandle)` in `WasmJSVEventListenerPool` and call it from `IDisposable`.     |
| **Object Cache**          | Reference counting is only incremented; several `// TODO: DecRef` comments remain. | Handles never reach zero → perpetual growth on both sides of the bridge.   | Audit every `IncRef`; pair each with a matching `DecRef` during disposal. Add unit tests to verify stable counts. |
| **Security Tests**        | Sanitizers lack a regression suite for common XSS vectors.                         | Silent sanitizer regressions are dangerous in production apps.             | Add unit tests covering `<img onerror>`, SVG payloads, CSS `url(javascript:...)`, nested templates, etc.          |

Addressing these items first will prevent memory leaks, dangling event handlers, and security regressions—laying a stable foundation for the broader roadmap.

---

## Contributing

Issues, feature requests, and pull requests are welcome!
For large‑scale changes, please open a discussion or issue first so we can agree on direction.

---

## License

### MIT License

Copyright (c) 2025 Bryan Johnston

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
