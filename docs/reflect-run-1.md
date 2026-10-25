# /reflect, run 1 (Stage 6 session, 2026-10-25)

**Conversation summary:** Stage 6 (6a–6c) of the LoanCalc migration, in which Vue custom elements were added inside `Default.aspx`
one at a time. There was a review stop after each part and a commit after each review: `37ee597`, `f0af717`, `88ddbab`.

- Signals found: 9 across 6 categories (error → fix, tool friction, user correction, repeated pattern, skill gap, context lookup)
- Opportunities extracted: 5
- Skill learnings to promote: none (no `learnings.log` files under `~/.claude/skills`)

## Findings

### High

**Memory: clicking by coordinates in the built-in browser pane misses**
- **Fact:** `computer` clicks at given coordinates used a frame that was neither CSS pixels nor device pixels. It was CSS ÷ ~1.28 at
  DPR 1.25, and the screenshot frame size changed between shots (800×600 vs 799×609). Clicks by element ref landed correctly. A
  capture-phase `document` click logger showed where clicks actually landed.
- **Related trap:** a cached `site.css` produced one false failure until I fetched it with `cache: 'reload'`.
- **Evidence:** in 6b, about 10 turns went into missed clicks on the `<term-picker>` buttons, which I first took for a component bug.
  Keyboard input and `element.click()` worked the whole time.

**Missing skill: `/handoff` can't be run by Claude, and its output location doesn't match CLAUDE.md**
- **Issue:** the `handoff` skill (mattpocock plugin) has `disable-model-invocation: true`, so only the user can run it. It also saves
  to the OS temp folder, while CLAUDE.md said "The next session starts from the handoff doc", which needs the doc somewhere a fresh
  session will find it.
- **Evidence:** step 2 of the end-of-session list was blocked when Claude tried to run it.

### Medium

**Rule: don't change an agreed design without asking**
- **Rule:** when an agreed design turns out awkward to build, stop and propose the change. Don't build the variant and explain
  it afterwards.
- **Evidence:** in 6b Claude replaced the agreed `invalid` attribute with reading the select's `aria-invalid`, and only explained
  the switch afterwards. The user stopped the commit to discuss it, and the agreed attribute turned out better: Vue custom elements
  update when their own attributes change but not their children's, so only the attribute updates live.

### Low

**Possible skill: Web Forms verification helpers.** The sandboxed-iframe no-JS check and the async-postback `endRequest` helper
were each written three or more times. Not worth a skill for throwaway interview prep.

**Possible memory: `runat="server"` on a hyphenated tag fails to parse.** Skipped, because DECISIONS 28 already records it.

## What was applied, and why

| Change | Where | Why |
|---|---|---|
| Memory `browser-pane-click-coordinates`, marked "needs verification in a fresh session" | `~/.claude/projects/C--dci/memory/` (+ `MEMORY.md` index line) | The top change: it wasted the most turns, and the fix is a durable tool fact. It's marked tentative because the ratio was measured only once and may depend on pane size or zoom. Wrong memory is worse than none. |
| Working rule: "If an agreed design turns out awkward to build, stop and propose the change before building a different version." | `CLAUDE.md`, Working rules | The user asked for it as a project rule, not a memory. It turns the 6b correction into a standing instruction for future sessions. |
| End-of-session step 2: "Type /handoff yourself, then move the doc it writes to docs/handoff.md." | `CLAUDE.md`, End of session | The user's wording. It fixes both handoff problems: Claude can't run the skill, and its temp-folder output wasn't where the next session looks. |

## Not applied

- **Web Forms verification helpers:** low value for a throwaway project.
- **The `runat="server"` parser fact:** already recorded in DECISIONS 28.

None of these changes are committed.
