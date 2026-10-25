# LoanCalc handoff: after Stage 6

Next session's focus: **manual validation, code review, training, and DCI interview prep.** No new features are planned.

## Where things stand

- Stage 6 is complete: Vue custom elements now sit inside the Web Forms page `Default.aspx` (no separate Vue page), added one
  piece at a time. Commits on `main`:
  - `37ee597`: 6a, the `<loan-results>` element
  - `f0af717`: 6b, the `<term-picker>` element
  - `88ddbab`: 6c, the UpdatePanel
  - Review the whole stage with `git diff cde0773..88ddbab`.
- **Why things are the way they are:** `DECISIONS.md` entries 27–32. Don't re-derive them; read them.
- **Uncommitted:** `CLAUDE.md` (a new working rule, plus end-of-session step 2 now says to move this doc to `docs/handoff.md`).
- **Untracked:** `docs/`. The user said not to commit it yet.
  - `docs/reflect-run-1.md` is this session's retrospective.
  - `docs/page-migration-run-1.md` is the user's private comparison plan. **Don't open it** unless the user asks.
- **Build and run steps:** README.md ("Run everything through the gateway"). `.claude/launch.json` has `webforms`, `api`,
  `gateway`; start them in that order. Rebuild the bundle (`npm --prefix src/loan-calc-vue run build`) and the site (VS MSBuild)
  first. Use http://localhost:5000.

## Manual validation checklist

What I checked in the browser this session (repeat it by hand in a real browser, with a real mouse):

1. **6a `<loan-results>`:** after Compare, the shadow-DOM table matches the server table cell for cell, and there's no `/api` call.
   With JavaScript off, the server table shows.
2. **6b `<term-picker>`:**
   - Mouse clicks and keyboard both work (Tab lands on the checked button; arrow keys move the choice).
   - Chosen terms survive Compare.
   - With JavaScript off, a full-width dropdown shows instead.
   - Setting `invalid="true"` in devtools turns it red right away; `"false"` turns it back.
3. **6c UpdatePanel:**
   - Compare doesn't reload the page.
   - A server-rejected input (rate 50%, with browser validation turned off) shows the error next to the field.
   - Both islands come back after every async postback.
4. **`CompareVue.aspx`** still compares, and is still themed.

**Not covered yet:**
- **A screen reader:** check that the radio group is announced as "Term".
- **Browsers other than the pane's Chromium.**
- **The server sending `invalid="true"`:** the UI can't produce it, because event validation blocks bad terms first (DECISIONS 31).
- **Focus after an async postback:** it drops to `<body>` (DECISIONS 32).

**Tooling trap:** clicks by coordinates in the built-in browser pane missed their targets this session. Use clicks by element ref, or
calibrate with a click logger. See memory `browser-pane-click-coordinates` (tentative).

## Code review: where to look hardest

- `src/loan-calc-vue/src/TermPicker.vue`:
  - `useHost().querySelector('select')` runs once in setup, so it depends on the script loading with `defer`.
  - Look at how the `change` listener is cleaned up, and the "true" / "" / "false" handling of `invalid`.
- `src/LoanCalc.WebForms/Default.aspx.cs`: `ResultsJson` uses `JavaScriptSerializer` with anonymous camelCase types and is
  rendered with `<%: %>` (HTML-encoded) in an attribute.
- `src/LoanCalc.WebForms/LoanInputs.ascx(.cs)`: `TermInvalid` and `aria-invalid` are set together in `ShowErrors`.
- Results are formatted twice, in C# `ResultRow` and in `ResultsTable.vue`, so the two could drift (DECISIONS 27).
- The bundle is cached only by ETag (DECISIONS 29).
- Line endings: files were written with LF, and git warns that it will convert them to CRLF. That's harmless, but expect the
  warnings.

## Training: concepts this stage exercised

- **Light DOM vs. shadow DOM:** unslotted children don't render but still post with the form, which is the no-JS fallback.
- **Why labels can't point into a shadow root,** and why the radio group is named by its `label` attribute instead.
- **Vue `defineCustomElement`:**
  - It watches the element's own attributes but not its children.
  - It unmounts an element when the element is removed from the page.
  - Styles from child components are injected into the shadow root, so the theme has to be declared in every element's root
    component (`theme.css`).
- **Web Forms:**
  - The parser rejects `runat="server"` on tags with a hyphen in the name.
  - Event validation checks posted dropdown values.
  - An UpdatePanel replaces its HTML wholesale, so everything the server changes must be inside it.
  - ScriptManager adds about 40 kB gzipped, cached for a year.

## Interview prep: talking points

- **"How do you migrate a Web Forms page without a rewrite?"** Strangler fig at two levels: by route, with the YARP gateway
  (DECISIONS 22), and within a page, with custom-element islands (27–32). The page keeps working after every step.
- **Islands keep their state in markup** (attributes, real form fields), so they survive full postbacks, UpdatePanel replacement,
  and having no JavaScript at all.
- **Stories of things that went wrong:** the hyphenated-tag parser error (28); errors lost when the UpdatePanel was too small (32);
  the agreed design being better than a quiet substitute (31, and the new CLAUDE.md rule).
- **Next step to describe:** have Compare call `/api/loans/compare` as `<loan-compare>` does, which removes the UpdatePanel and the
  ScriptManager (DECISIONS 32 revisit).

## Suggested skills

- `code-review` or `mattpocock-skills:code-review`: review `cde0773..88ddbab`.
- `run` and `anthropic-skills:built-in-browser`: start the three apps and drive the manual checks.
- `mattpocock-skills:grilling`: rehearse the interview questions above.
- `reflect`: at the end of the session (CLAUDE.md end-of-session steps).
