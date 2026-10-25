---
name: page-migration
description: Plan a staged migration of one ASP.NET Web Forms page (.aspx/.ascx) to Vue components and ASP.NET Core, one region at a time, keeping the page shippable after every step. Use when asked to assess, plan, or start migrating a Web Forms page. Plans only; does not change code.
---

# Page migration plan

Goal: move a Web Forms page to Vue + ASP.NET Core in small steps. The page keeps working after every step. Replacing the whole page with a new Vue page is the last step, not the first, and only once every region has moved.

Write the plan. Don't edit code. Cite every finding as `file:line`.

## 1. Read everything the page touches

- The `.aspx`, its code-behind and designer file.
- Every user control (`<%@ Register %>`), master page, and base page class it uses.
- `Web.config` sections it depends on: `appSettings`, `authentication`, `authorization`, `sessionState`, `pages`, `compilation/assemblies`.
- Script and CSS it loads, including anything registered from code (`ClientScript.Register*`, `ScriptManager.Register*`).
- Shared libraries the code-behind calls.

If a dependency can't be found, say so. Don't guess what it does.

## 2. Inventory

One table, one row per dependency: what, where (`file:line`), and why it matters for migration.

Look for:
- **ViewState:** explicit `ViewState[...]`, controls that rely on it (list items added only on first load, `EnableViewState`, dynamic controls).
- **Session, Application, Cache:** keys read and written. Note which other pages use the same keys.
- **Postback events:** button clicks, `AutoPostBack`, `OnSelectedIndexChanged`, `__doPostBack` in script, cross-page postbacks.
- **Partial updates:** `UpdatePanel`, `ScriptManager`, Telerik `RadAjaxManager`/`RadAjaxPanel`.
- **Validation:** validator controls, rules in code-behind, rules in a shared library. Name the one place each rule lives.
- **Third-party controls:** Telerik `Rad*` and others. Flag `RadWindow` and anything else that loads in an iframe.
- **Lifecycle order:** logic that depends on `Page_Init` vs `Page_Load` vs event handlers.
- **Auth:** role checks, `User.Identity`, forms auth cookie use.
- **Formatting and culture:** how numbers, money, and dates are parsed and displayed.

## 3. Sort the code-behind

Put each method or block in one bucket:
- **Business rules:** move to the shared library (netstandard2.0 if the Web Forms site still needs it).
- **Data access:** stays behind the server. Never call it from the browser.
- **Parsing and formatting:** stays with whichever host renders the field.
- **UI wiring:** goes away as each region moves.

If a business rule is duplicated (validator control plus code-behind, or client script plus server), say which copy wins.

## 4. Pick regions, in order

Split the page into regions (a results table, one input group, a dialog). For each, rate:
- **Independence:** does it read or write postback state other regions need?
- **Value:** does moving it unblock the reskin or the rebuild?
- **Risk:** UpdatePanels, third-party controls, Session, lifecycle tricks.

Order them by this default unless the page gives a reason not to:
1. **Read-only regions.** The server renders the data as JSON in an attribute on the custom element after each postback. No API needed. Keep the server-rendered markup as the element's light-DOM content, so the page still works with JavaScript off.
2. **Inputs that feed the postback.** Inputs inside a shadow root don't belong to the page's `<form runat="server">`, so their values don't post. Bridge each value through a light-DOM hidden field the code-behind reads, and have the server re-render the element's attribute from the posted value so the selection survives the round trip. Server validation still decides.
3. **Regions that need live data.** Add an API endpoint only when the region must update without a postback. Route it through the gateway, so it's same-origin and the page's auth cookie travels with it.
4. **The page shell.** Only when nothing is left but layout. Then the page becomes a host or a new route in the gateway, and the `.aspx` is deleted.

## 5. Known traps

Check each one against this page and say whether it applies.
- **UpdatePanel async postbacks** replace the panel's DOM. Any custom element inside is destroyed and recreated, and its Vue state is lost. Keep islands outside the panel, or persist state in an attribute the server re-renders.
- **Full postbacks** also recreate every element. Vue state only survives if the server writes it back.
- **Naming containers** mangle `name` and `id` (`ctl00$LoanA$Term`). Never hard-code them in Vue. Pass `ClientID` as an attribute, or put the hidden field inside the element.
- **Attributes are strings.** Numbers and booleans need prop types. Objects need JSON or a JS property.
- **Styles don't cross the shadow root** except custom properties and `::part`. Page CSS and Telerik skins won't reach inside the element, and element CSS won't leak out.
- **Iframes** (`RadWindow` opened by URL) don't inherit the parent page's CSS or custom properties.
- **Script loading:** several islands on one page should share one copy of Vue. Note the bundle size.
- **Enter key:** a form inside the element submits that form, not the page's postback.

## 6. Output

Use these sections, in this order:
1. **Summary:** two or three sentences, and the first region to move.
2. **Inventory** table from step 2.
3. **Code-behind buckets** from step 3.
4. **Steps:** one per region, in order. For each: what moves, how data gets in and out (attribute, hidden field, or API), what proves it worked, and how to roll it back.
5. **State ownership:** a table of each piece of state and who owns it before and after (ViewState, Session, server-rendered attribute, Vue, API).
6. **New API endpoints**, if any, with request and response shapes.
7. **Risks** from step 5 that apply, plus anything else found.
8. **What stays Web Forms for now, and why.**
9. **Questions** for the person, if anything couldn't be settled from the code.

## Rules

- Each step must leave the page shippable.
- Validation stays on the server. Client checks are convenience only.
- No new packages without naming the package and its license.
- If the page is too tangled to split, say so plainly and explain why, rather than forcing a plan.
