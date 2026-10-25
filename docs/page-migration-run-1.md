# Migration plan: `Default.aspx` → Vue + ASP.NET Core

## 1. Summary

`Default.aspx` is about as easy to split as a Web Forms page gets. Its only state is post data and a bit of ViewState. It has one postback event and no Session, auth, UpdatePanels or third-party controls. The finished Vue version already exists as `CompareVue.aspx`, but it was built as a full-page replacement. This plan moves `Default.aspx` toward the components that page already uses, one region at a time. **Move the results table first**, as a read-only `<loan-results>` island fed by a JSON attribute.

## 2. Inventory

|What|Where|Why it matters|
|---|---|---|
|User control, used twice|[Default.aspx:2](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx:2), [:20-21](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx:20)|Naming containers produce names like `LoanA$Principal` / `LoanA_Principal`. There's no master page, so no `ctl00`.|
|Postback: `CompareButton_Click`|[Default.aspx:24](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx:24), [Default.aspx.cs:20-32](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx.cs:20)|The only event. No `AutoPostBack`, `__doPostBack` or cross-page postback.|
|ViewState (implicit): `ResultsPanel.Visible`, Repeater items|[Default.aspx:26](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx:26), [:33](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx:33)|The Repeater re-renders from ViewState on a postback that skips the click. None exists today, but the results island must not depend on it.|
|ViewState (implicit): `Errors` list, `CssClass`, `aria-invalid`|[LoanInputs.ascx.cs:61-85](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanInputs.ascx.cs:61)|Reset on every click, so nothing depends on it in practice.|
|Term list rebuilt in `Page_Init`|[LoanInputs.ascx.cs:16-22](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanInputs.ascx.cs:16)|Built before post data loads on purpose, so the list never depends on ViewState. Lifecycle-sensitive.|
|Default values on first load|[Default.aspx.cs:13-17](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx.cs:13)|Also copied in [LoanCompare.vue:12-13](https://claude.ai/epitaxy/loan-calc-vue/src/LoanCompare.vue:12).|
|Session / Application / Cache|none (grep)|Nothing to share with other pages.|
|UpdatePanel / ScriptManager / Telerik / iframes|none|Not a factor here.|
|Auth|none: [Web.config:1-17](https://claude.ai/epitaxy/LoanCalc.WebForms/Web.config:1) has no `authentication` or `authorization`, and nothing reads `User`|The gateway doesn't need to forward cookies for auth.|
|Validation: HTML5 attributes|[LoanInputs.ascx:9](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanInputs.ascx:9), [:13](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanInputs.ascx:13), [:21](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanInputs.ascx:21), [:25](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanInputs.ascx:25)|Convenience only.|
|Validation: parsing ("is a number", "required")|[LoanInputs.ascx.cs:87-102](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanInputs.ascx.cs:87)|Belongs to the host (DECISIONS 9).|
|Validation: range rules|[LoanValidator.cs:30-64](https://claude.ai/epitaxy/LoanCalc.Core/LoanValidator.cs:30)|**Source of truth.**|
|Allowed terms, copied client-side|[LoanValidator.cs:25](https://claude.ai/epitaxy/LoanCalc.Core/LoanValidator.cs:25) vs [LoanFields.vue:3](https://claude.ai/epitaxy/loan-calc-vue/src/LoanFields.vue:3)|Core wins. Web Forms reads Core directly.|
|Display formatting (en-US money, APR to 3 places, duration)|[Default.aspx.cs:38](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx.cs:38), [:53](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx.cs:53), [:70-76](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx.cs:70)|Copied in [LoanCompare.vue:40-41](https://claude.ai/epitaxy/loan-calc-vue/src/LoanCompare.vue:40), [:57](https://claude.ai/epitaxy/loan-calc-vue/src/LoanCompare.vue:57).|
|Input parsing culture (invariant)|[LoanInputs.ascx.cs:42](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanInputs.ascx.cs:42), [:99](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanInputs.ascx.cs:99)|`type="number"` always posts `.`.|
|CSS|[Default.aspx:9](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx:9) → [site.css](https://claude.ai/epitaxy/LoanCalc.WebForms/Content/site.css)|`.results` / `.field` rules won't reach inside a shadow root. Only `--lc-*` variables cross ([site.css:2-16](https://claude.ai/epitaxy/LoanCalc.WebForms/Content/site.css:2)).|
|Scripts|none on `Default.aspx`. The bundle is only on [CompareVue.aspx:13](https://claude.ai/epitaxy/LoanCalc.WebForms/CompareVue.aspx:13)|Step 1 adds the first script tag to this page.|
|`compilation/assemblies` netstandard|[Web.config:10-13](https://claude.ai/epitaxy/LoanCalc.WebForms/Web.config:10)|Needed while the markup touches Core types ([Default.aspx:33](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx:33) `ItemType`).|
|No `System.Web.Extensions` reference|[LoanCalc.WebForms.csproj:39-41](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanCalc.WebForms.csproj:39)|No `JavaScriptSerializer` today. See Step 1.|

## 3. Code-behind buckets

|Code|Bucket|Notes|
|---|---|---|
|`LoanValidator.Validate`, `LoanCalculator.Calculate` calls ([LoanInputs.ascx.cs:55](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanInputs.ascx.cs:55), [:58](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanInputs.ascx.cs:58))|Business rules|Already in Core.|
|"Lower is better" highlight ([Default.aspx.cs:65-66](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx.cs:65))|Business rule (display)|Copied in [LoanCompare.vue:51](https://claude.ai/epitaxy/loan-calc-vue/src/LoanCompare.vue:51). Each host keeps its copy until Web Forms renders no results. Not worth putting in Core.|
|Default loan values ([Default.aspx.cs:15-16](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx.cs:15))|Business rule (minor)|Copied in Vue. Optional: move to Core.|
|`ParseAmount`, term parse ([LoanInputs.ascx.cs:42](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanInputs.ascx.cs:42), [:87-102](https://claude.ai/epitaxy/LoanCalc.WebForms/LoanInputs.ascx.cs:87))|Parsing|Stays in Web Forms while it renders the inputs.|
|`ResultRow` formatting ([Default.aspx.cs:36-77](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx.cs:36))|Formatting|Stays as the no-JS fallback. Vue formats the island.|
|`SetValues`, `ShowErrors`, `Page_Init` term list, `ResultsPanel.Visible`|UI wiring|Removed as each region moves.|
|Data access|none||

**Duplicated rules and which copy wins:** the HTML5 `min`/`max` attributes and `TERMS` in Vue copy Core, and Core wins. The Extra field has no client-side max; Core caps it at the principal ([LoanValidator.cs:59](https://claude.ai/epitaxy/LoanCalc.Core/LoanValidator.cs:59)).

## 4. Steps

### Step 1: Results table → `<loan-results>` (read-only)

- **What moves:** Extract the `<div class="results">` template, `rows`, the formatters and the `.results` CSS from [LoanCompare.vue:40-60](https://claude.ai/epitaxy/loan-calc-vue/src/LoanCompare.vue:40) and [:74-90](https://claude.ai/epitaxy/loan-calc-vue/src/LoanCompare.vue:74) into `LoanResults.vue`. `LoanCompare.vue` then uses it as a child, so `CompareVue.aspx` keeps working. Register `loan-results` in [main.js](https://claude.ai/epitaxy/loan-calc-vue/src/main.js) in the same bundle, so the page loads one copy of Vue.
- **Data in:** Inside `ResultsPanel`, wrap the existing `<table>` in `<loan-results results='…'>`. The JSON uses the API's `CompareResponse` shape (`{loanA:{monthlyPayment,…},loanB:{…}}`), HTML-encoded with `<%: %>`. The Repeater markup stays as light-DOM content. With no `<slot>` it's hidden when JS runs and shown when JS is off. Add `<script src="Scripts/loan-compare/loan-compare.js" defer>` to [Default.aspx:9](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx:9).
- **JSON on net48:** Either add the framework reference `System.Web.Extensions` (part of .NET Framework, not a NuGet package) for `JavaScriptSerializer`, or write the 10 numbers by hand with `InvariantCulture`. I'd write them by hand: no new reference for 10 numbers. That choice needs a DECISIONS entry.
- **Data out:** none.
- **Proof:** The same defaults show identical numbers and highlights in the island, the JS-off fallback and `CompareVue.aspx`. Changing `--lc-accent` on `:root` restyles the island. `CompareVue.aspx` is unchanged. `dotnet test` still passes.
- **Rollback:** Remove the wrapper tag and the script tag. The Repeater was never removed.

### Step 2: Loan inputs → `<loan-inputs>` island inside `LoanInputs.ascx`

- **What moves:** A wrapper element around the existing [LoanFields.vue](https://claude.ai/epitaxy/loan-calc-vue/src/LoanFields.vue), one per user control.
- **Data in:**
    - **Values:** the server-rendered `TextBox`/`DropDownList` fields stay as the element's light DOM. The element reads its starting values from them, found by a `data-field="principal"` attribute and never by `name`/`id`. Postback restores them as it does now, so values survive the round trip without ViewState.
    - **Errors:** `ShowErrors` also writes an `errors='{"principal":["…"]}'` JSON attribute.
- **Data out:** On every change, the element writes the value back into the matching light-DOM field. Those fields stay the posted fields, so `TryCalculate`, parsing and Core validation don't change at all. On mount the element strips `required`/`min`/`max` from the hidden fields, because the browser can't focus them to show a validation message and would silently block the submit.
    - This deviates slightly from the skill's "hidden field" pattern: the existing inputs are the bridge, so there's no new field and no code-behind branch.
- **Enter key:** Pressing Enter in a shadow-root input no longer submits `form1`. Fix: set `DefaultButton="CompareButton"` on [Default.aspx:12](https://claude.ai/epitaxy/LoanCalc.WebForms/Default.aspx:12). Keyboard events are composed, so `WebForm_FireDefaultButton` should catch them. Test this. If it fails, the element calls `form.requestSubmit(submitter)` with a submitter marked by `data-default-submit`.
- **Proof:** A bad value (rate 50) shows Core's message under the field after the postback. Valid input gives the same results as Step 1. JS off still works. Enter submits.
- **Rollback:** Remove the wrapper from `LoanInputs.ascx`. The original fields and code-behind never changed.

### Step 3: Live comparison (Compare without a postback)

- **What moves:** The Compare button and the calculation. Combine the two islands under one `<loan-compare>`, the existing element, which calls `POST /api/loans/compare` through the gateway. `CompareButton`, `TryCalculate` and `ResultRow` become the no-JS fallback only.
- **Data in/out:** the existing API. No new endpoint.
- **Proof:** No network request to `Default.aspx` when Compare is clicked. The API's 400 errors map to fields. The page must be opened through port 5000 (DECISIONS 24).
- **Rollback:** Go back to the Step 2 markup.

### Step 4: Page shell

- **What moves:** `Default.aspx` and `CompareVue.aspx` are now the same page. Serve a static host page from `LoanCalc.Api` (`UseStaticFiles`, part of the shared framework, so no package). Add gateway routes for `/` and `/Default.aspx`, and move the bundle output out of the Web Forms site. Then delete `Default.aspx`, `LoanInputs.ascx`, `CompareVue.aspx` and the netstandard entry in Web.config (DECISIONS 10).
- **Proof:** The gateway serves the page with Web Forms stopped.
- **Rollback:** Remove the gateway routes. The legacy fallback serves the `.aspx` again until it's deleted.

## 5. State ownership

|State|Now|After 1|After 2|After 3|After 4|
|---|---|---|---|---|---|
|Input values|post data (TextBox)|same|light-DOM fields, mirrored by Vue|Vue|Vue|
|Term list|`Page_Init` from Core|same|Vue `TERMS` (copy of Core)|same|same|
|Validation errors|`BulletedList` + ViewState|same|`errors` attribute rendered by the server|API 400 → Vue|same|
|Results|Repeater + ViewState|`results` attribute rendered by the server|same|API → Vue|same|
|Results visible|`ResultsPanel.Visible` (ViewState)|same|same|Vue (`v-if`)|same|
|Defaults|code-behind|same|same|Vue (copy)|Vue|

## 6. New API endpoints

None. `POST /api/loans/compare` ([Program.cs:26-39](https://claude.ai/epitaxy/LoanCalc.Api/Program.cs:26)) covers Step 3. `GET /api/loans/terms` (DECISIONS 13, 20) would remove the copied `TERMS`, but it's optional.

## 7. Risks

- **Full postbacks recreate the elements.** This applies in Steps 1–2. That's safe because all state is written back by the server (attribute or light-DOM fields).
- **Naming containers.** This applies to Step 2. `data-field` lookups avoid hard-coded names.
- **Attributes are strings.** Results and errors go in as JSON, so the props must parse them.
- **Styles don't cross the shadow root.** The `.results` and `.field` CSS ends up in two places (site.css for the fallback, SFC styles for the island). The `--lc-*` variables keep the theme consistent.
- **Script loading.** One IIFE defines every element, so there's one copy of Vue. `Default.aspx` gains about 81 kB (31 kB gzipped).
- **Enter key.** Applies to Step 2 (see above).
- **Hidden `required` fields blocking submit.** Applies to Step 2. Found while planning; it's not in the skill's list.
- **UpdatePanels and iframes:** don't apply.
- **Copied formatting** (C# `ResultRow` vs JS `Intl`) could drift, for example negative values or `C2` vs `Intl` edge cases. Step 1's proof compares them.

## 8. What stays Web Forms for now

Through Step 2, the postback, parsing in `TryCalculate`, and the Repeater fallback all stay. The server still decides everything, and JS-off users keep a working page. Through Step 3, the `.aspx` shell and the no-JS fallback stay, because removing them is the point where JS becomes required.

## 9. Questions

1. **No-JS support:** keep it until Step 4, or drop it earlier? Dropping it would let Step 3 delete `TryCalculate` and `ResultRow` sooner.
2. **`CompareVue.aspx`:** keep it as the reference until Step 4, or retire it once `Default.aspx` reaches Step 3?
3. **JSON in Step 1:** write it by hand (my pick) or add the `System.Web.Extensions` framework reference?