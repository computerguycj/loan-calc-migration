# Decisions

One entry per real decision: what, why, and what I'd revisit.

## 1. .NET 10 SDK and Node 24

- **What:** Use .NET 10 (LTS) for the API and tests, and Node 24 (LTS) for Vite + Vue.
- **Why:** Both are already installed, current, and on long-term support.
- **Revisit:** If the target shop is still on .NET 8, retarget the API to `net8.0` (that SDK is installed too).

## 2. Web Forms uses a legacy (non-SDK) project, built with Visual Studio's MSBuild

- **What:** `LoanCalc.WebForms` is an old-style `.csproj`, run on IIS Express.
- **Why:** SDK-style projects and `dotnet build` don't support Web Forms.
- **Revisit:** Nothing. This limitation is part of why you'd migrate off Web Forms in the first place.

## 3. Tests target both net10.0 and net48

- **What:** The test project builds for two frameworks, so the tests run twice.
- **Why:** netstandard2.0 can't host tests. Running on net48 checks the math on the same
  runtime as the Web Forms site; net10.0 checks it for the API.
- **Revisit:** Drop net48 after Web Forms is retired.

## 4. Money in `decimal`, APR solver in `double`

- **What:** Payments, interest, and balances use `decimal`, rounded to the cent (midpoint away from zero).
  The APR solver uses `double`.
- **Why:** `decimal` gives exact cents, so the results tie out to the penny. APR is an iterative estimate
  reported to 3 decimal places, so `double` precision is more than enough.
- **Revisit:** Rounding rules (banker's rounding vs. away-from-zero) if matching a specific lender's statements.

## 5. APR uses scheduled payments only; fees are paid up front

- **What:** APR solves for the rate at which (principal − fees) equals the present value of the scheduled
  payments. Extra payments are ignored for APR but included in total interest and total payback.
  Fees are not rolled into the loan.
- **Why:** This follows the US Truth in Lending convention, because extra payments are voluntary.
  Paying fees up front keeps the inputs simple.
- **Revisit:** Add a "finance the fees" option. Real Reg Z APR also handles odd first periods and
  irregular payments, which are out of scope here.

## 6. Validation lives in Core and is the source of truth

- **What:** `LoanValidator` holds every input rule. `LoanCalculator.Calculate` refuses invalid input by throwing.
- **Why:** Web Forms and the API then enforce identical rules. Client-side checks only copy them for convenience.
- **Revisit:** Return a result type instead of throwing, if callers start using exceptions for control flow.

## 7. Plain C# 7.3 in Core and tests; `src/` + `tests/` folders

- **What:** No newer language features, so nothing needs runtime polyfills on net48.
- **Why:** That keeps things simple for a library shared with .NET Framework.
- **Revisit:** Raise `LangVersion` if records or nullable annotations become worth the polyfills.

## 8. Web Forms import of VS web targets is conditional on the file existing

- **What:** `Microsoft.WebApplication.targets` is imported only when the file exists, and the Web Forms project is in `LoanCalc.slnx`.
- **Why:** That lets `dotnet build` / `dotnet test` run on the whole solution. VS and MSBuild still get the full web targets.
- **Revisit:** If CI ever needs to publish Web Forms, it must use VS MSBuild. `dotnet` can't publish it.

## 9. Web Forms: a user control per loan, no ASP.NET validator controls

- **What:** `LoanInputs.ascx` is used twice. Its code-behind parses the text, then calls `LoanValidator` from Core.
  Browser-side checks are just HTML5 attributes (`type="number"`, `min`, `max`, `required`).
- **Why:** Validator controls would duplicate the rules in a second place. Parsing ("is this a number?") belongs
  to the host, while range rules belong to Core.
- **Revisit:** Nothing. This mirrors how the API will work: parse, then let Core decide.

## 10. Web Forms needs a `netstandard` assembly entry in Web.config

- **What:** `<compilation><assemblies>` lists `netstandard, Version=2.0.0.0`.
- **Why:** ASP.NET compiles `.aspx` markup at runtime. Markup that touches netstandard2.0 types fails without it.
- **Revisit:** Not needed once the page is gone.

## 11. Results display in en-US, regardless of server culture

- **What:** Money and percentages are formatted with `CultureInfo("en-US")`. Input is parsed with the invariant culture.
- **Why:** The calculator is in US dollars, and `type="number"` inputs always post `.` as the decimal separator.
- **Revisit:** Localization, if it ever matters.

## 12. Fixed local ports: Web Forms on 5080, API on 5090

- **What:** IIS Express serves Web Forms at http://localhost:5080 (set in the `.csproj` too, so VS F5 uses the same port).
  The API runs at http://localhost:5090, HTTP only (the template's HTTPS profile was removed).
- **Why:** Stage 4 needs a predictable origin for the API's CORS setup or proxy.
- **Revisit:** Stage 5 (YARP) puts everything behind one port. Use HTTPS anywhere that isn't local.

## 13. API: one endpoint, Core's rules returned as `ValidationProblem`

- **What:** `POST /api/loans/compare` takes `{ loanA, loanB }` and either returns both results or a 400 whose
  `errors` keys mirror the JSON paths (`loanA.principal`). Both loans are always validated, so the caller sees every error at once.
- **Why:** Problem details (RFC 9457) is the built-in ASP.NET Core error format. Path-shaped keys let the Vue element put
  each message next to the right field. The API adds no rules of its own.
- **Revisit:** Add a single-loan endpoint, or `GET /api/loans/terms` so the UI doesn't hard-code the allowed terms.

## 14. API: malformed bodies return 400, not 500

- **What:** `UseExceptionHandler` keeps the status code of `BadHttpRequestException` and adds a plain-English `detail`.
- **Why:** In Development, minimal APIs throw on bad JSON, and the default exception handler turned that into a 500.
  A client mistake should be a 400 in every environment.
- **Revisit:** Nothing. This is just correct behavior.

## 15. API: no Swagger/OpenAPI package; a `.http` file instead

- **What:** Built from the `web` (empty) template, with no package references. Manual testing uses `LoanCalc.Api.http`.
- **Why:** Keeps the package list empty and the project small.
- **Revisit:** Add `Microsoft.AspNetCore.OpenApi` (MIT) if a typed client for the Vue app becomes worth generating.
  Add `WebApplicationFactory` integration tests if the API grows past one endpoint.

## 16. Cross-origin calls: CORS on the API (not a proxy) until Stage 5 — *superseded by 22*

- **What:** The Web Forms page (localhost:5080) calls the API (localhost:5090) directly. The API's CORS policy allows
  only origins listed in `Cors:AllowedOrigins` (Development: `http://localhost:5080`), only `POST`, and only the `Content-Type` header.
- **Why:** A proxy needs something in front of the Web Forms site. IIS Express has no reverse-proxy module
  (URL Rewrite + ARR) out of the box, and the Vite dev proxy only covers pages Vite itself serves. CORS is a few lines of
  config and makes the cross-origin boundary visible, which is the honest state of a half-migrated app.
  `UseCors()` runs first so that error responses carry CORS headers too. Without that, the browser hides 400s from the element.
- **Revisit:** Stage 5. With YARP serving both apps from one origin, `LoanApiBaseUrl` becomes empty and the CORS list can be removed.

## 17. API address comes from `Web.config`, passed as the `api-base` attribute

- **What:** `CompareVue.aspx` renders `<loan-compare api-base="…">` from `appSettings["LoanApiBaseUrl"]`. The element
  defaults to `''`, meaning same origin.
- **Why:** The bundle stays environment-agnostic, so the same JS works cross-origin today and same-origin behind a proxy.
- **Revisit:** Nothing, until there are more settings than one URL.

## 18. Vite library build, IIFE format, Vue bundled in, output into the Web Forms site

- **What:** `vite build` emits one `loan-compare.js` (about 81 kB, 31 kB gzipped) straight into
  `LoanCalc.WebForms/Scripts/loan-compare/`. That folder is git-ignored and listed as `Content` in the `.csproj`.
- **Why:** IIFE works from a plain `<script>` tag on a legacy page, and bundling Vue means the page doesn't need Vue
  loaded separately. `customElement: true` inlines all component CSS into the shadow root, so no CSS file is emitted.
- **Revisit:** If several Vue islands share a page, switch to ES modules with Vue as a shared chunk, so it loads once.
  Add a cache-busting query string or hashed name when deploying for real.

## 19. Element styles: `--lc-*` theme variables with built-in fallbacks

- **What:** `:host` maps each `--lc-*` variable to a private `--_*` variable with a default, and the component CSS uses the
  private ones. The names match `site.css`.
- **Why:** Custom properties are the one styling channel that crosses the shadow boundary on purpose. Both pages share a
  theme, and the element still looks right on an unthemed page.
- **Revisit:** Add `::part()` names if a host ever needs to restyle more than colors.

## 20. Element validation: HTML5 attributes only; server errors mapped per field

- **What:** Inputs use `required`/`min`/`max`/`type="number"`, as in the Web Forms page. The 400 `errors` keys
  (`loanA.principal`) are shown under the matching field. Network and CORS failures show one plain-English message.
- **Why:** That's the same split as Web Forms: the browser checks are a convenience, and Core decides. No JS validation library needed.
- **Revisit:** Fetch the allowed terms from the API instead of hard-coding them in `LoanFields.vue`.

## 21. Plain JavaScript, not TypeScript, in the Vue project

- **What:** `.js` and `<script setup>` without `lang="ts"`.
- **Why:** Keeps the toolchain to three packages for a throwaway project.
- **Revisit:** Use TypeScript for anything real, ideally with types generated from the API's OpenAPI document.

## 22. Stage 5: YARP gateway in front of both apps (strangler fig)

- **What:** `LoanCalc.Gateway` (port 5000) routes `/api/{**rest}` to the API and everything else (`Order: 1000`)
  to Web Forms. Routes live in `appsettings.json`. `LoanApiBaseUrl` is now empty, so the element calls its own origin.
- **Why:** One origin is what users would see in production, and it's how an incremental migration usually ships.
  Each migrated path is one explicit route; everything else falls through to the legacy app. The gateway is a separate,
  near-empty app, so neither the old nor the new app has to know about the other.
- **Revisit:** Add HTTPS, health checks, and forwarded-header handling (`X-Forwarded-*`) in the apps before anything real.
  Eventually the API could host the gateway routes itself, removing one hop.

## 23. CORS removed from the API

- **What:** The CORS policy, `UseCors()`, and the `Cors:AllowedOrigins` setting are gone.
- **Why:** Browsers now reach the API only through the gateway, on the same origin as the pages. A CORS policy nobody
  needs is just attack surface to maintain.
- **Revisit:** Add it back only if a page on another origin legitimately needs the API.

## 24. Gateway is the only supported way to open the Vue page

- **What:** `CompareVue.aspx` on port 5080 directly shows "The loan service returned 404". `Default.aspx` still works on 5080.
- **Why:** Supporting both direct and proxied access needs a mode switch plus CORS again, for no real benefit. The
  README says to open http://localhost:5000.
- **Revisit:** If people keep opening 5080 by habit, make VS F5 launch the gateway too, or redirect 5080 to 5000.

## 25. Host header: YARP's default (the destination's host)

- **What:** No transforms. YARP sends `Host: localhost:5080` or `localhost:5090`, not `localhost:5000`.
- **Why:** IIS Express only accepts requests whose Host header matches its binding (`localhost:5080`), and rejects others
  with 400 "Invalid Hostname". The default just works; I verified it with postbacks through the gateway.
- **Revisit:** If the apps need the public host (for absolute URLs or redirects), read `X-Forwarded-Host`, which YARP
  adds by default, rather than switching to `RequestHeaderOriginalHost`.

## 26. `.claude/launch.json` for starting the three apps

- **What:** Named start configurations for `webforms` (IIS Express, 5080), `api` (5090), and `gateway` (5000), used by
  Claude Code's preview tool. The `webforms` entry hard-codes `C:\Program Files\IIS Express\iisexpress.exe` and `C:\dci\...`.
- **Why:** Starting, stopping, and restarting the apps by hand was the most repeated chore during the build. Named configs
  start each app in one step and reuse it if it's already running. Hard-coded paths match the README and this one machine.
- **Revisit:** If the repo is cloned anywhere other than `C:\dci`, make the paths relative (or document the change).
  The configs only start the apps: build first (README), and start `webforms` and `api` before `gateway`.

## 27. Stage 6a: `<loan-results>` island inside the Web Forms page, fed by a JSON attribute

- **What:** `Default.aspx` wraps the server-rendered results panel in `<loan-results results="…">`. After a successful
  Compare postback the code-behind serializes both `LoanResult`s with `JavaScriptSerializer` (from `System.Web.Extensions`, part of
  .NET Framework) into the same camelCase shape the API returns. The element renders that in its shadow root and never calls the API.
  The server table stays as light DOM; the element has no `<slot>`, so the table shows only when JavaScript is off.
- **Why:** It's the smallest island that proves a Vue component can live inside `<form runat="server">` and take its data from a
  postback. Matching the API shape lets the island reuse the same Vue table as `<loan-compare>`. Leaving the table in place keeps the
  no-JS path for free.
- **Revisit:** The table is rendered twice (C# `ResultRow` and `ResultsTable.vue`) and the formatting could drift. Delete the server
  table once no-JS support stops mattering.

## 28. Custom elements on a Web Forms page are plain markup, not `runat="server"`

- **What:** `<loan-results>` is written as literal HTML, with its attribute set by `<%: ResultsJson %>` from a protected
  code-behind property.
- **Why:** The Web Forms parser rejects server tags with a hyphen in the name ("The server tag is not well formed"), so a custom element
  can't be an `HtmlGenericControl`. `<%: %>` HTML-encodes, so the JSON is a safe attribute value.
- **Revisit:** If an island ever needs server-side state across postbacks (view state), wrap it in a `PlaceHolder` or write a small
  server control that renders the tag.

## 29. One bundle for every element, still named `loan-compare.js`

- **What:** `main.js` registers `<loan-compare>` and `<loan-results>` (and later islands) from one IIFE bundle. The file keeps its
  Stage 4 name and folder. Adding `<loan-results>` took it from 81,314 to 81,787 bytes (+473 B; gzipped 30,704 → 30,796, +92 B).
- **Why:** Vue is almost all of the bundle, and one bundle loads it once per page whatever elements the page uses. That settles
  decision 18's worry about several islands on one page without moving to ES modules. Keeping the name means `CompareVue.aspx`
  stays untouched.
- **Revisit:** Rename it (e.g. `loan-elements.js`) the next time `CompareVue.aspx` changes anyway. Split per element only if one gets
  heavy enough that pages not using it notice.

## 30. Shared `ResultsTable.vue` and `theme.css` across elements

- **What:** The results table and its styles moved out of `LoanCompare.vue` into `ResultsTable.vue`. The `:host` theme block moved
  into `theme.css`, which each element's root component includes with `<style src="./theme.css">`.
- **Why:** Two elements show the same table. Child component styles are injected into the parent element's shadow root, but
  each element is its own shadow root, so the theme variables must be declared in every root.
- **Revisit:** Nothing, unless an element needs a different table.

## 31. Stage 6b: `<term-picker>` wraps the real `<select>` instead of a hidden field

- **What:** In `LoanInputs.ascx` the Term `DropDownList` is the light-DOM child of `<term-picker label="Term">`. The element
  finds it with `useHost().querySelector('select')`, reads its options and value from it, and renders a
  `role="radiogroup"` of buttons (arrow keys, roving tabindex) in its shadow root. Each choice is written back to the select, which
  is what posts. The code-behind still reads `Term.SelectedValue`, and `LoanValidator` still decides. Invalid state is an
  explicit `invalid="true|false"` attribute, rendered from `LoanInputs.TermInvalid`, which `ShowErrors` sets.
  Bundle: 81,787 → 83,590 bytes (+1,803; gzipped 30,796 → 31,362, +566).
- **Why:**
  - Finding its own child needs no client IDs, and two pickers on one page can't mix up their fields.
  - Without JavaScript the select is an ordinary dropdown; a hidden field would have left no way to pick a term.
  - The options come from `LoanValidator.AllowedTerms` through `Page_Init`, so nothing is hard-coded in JS.
  - Invalid state is the element's own attribute, not read from the select's `aria-invalid`. Vue custom elements react to changes
    in their own attributes but not their children's, so reading the select would only ever see the value at mount. The attribute
    also puts the element's contract (`label`, `invalid`, a child select) in the markup, instead of relying on how `ShowErrors`
    marks the select. Because the element is plain markup (decision 28), the attribute is always written, so `"false"` means
    false. An empty value also means true, as with boolean attributes. Changing it in place restyles the same element.
  - The cost is two flags set in the same `ShowErrors` call: `aria-invalid` on the select (still needed for no-JS screen readers)
    and `invalid` on the element.
  - Labels and `aria-labelledby` can't reach into a shadow root, so the group gets its name from the `label` attribute.
  - The page CSS sets `.field term-picker` to a flex column; without it the undefined, inline element shrinks the no-JS select.
- **Revisit:** Event validation rejects a posted term that isn't in the list (500, "Invalid postback or callback argument") before
  Core sees it. That's a second guard, but it means Core's term error, and so `invalid="true"` from the server, can't be reached
  from the UI; only the element side is tested (by setting the attribute in the browser).
  Catch it and show a friendly error if tampered posts ever matter. Consider form-associated custom elements (`ElementInternals`)
  once Vue's `defineCustomElement` supports them, so the element could post without a child field.
