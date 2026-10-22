# LoanCalc

Practice project: an incremental migration of a two-loan mortgage calculator from ASP.NET Web Forms to
Vue + ASP.NET Core. See README.md for layout and how to run it, and DECISIONS.md for why things are the way they are.

## Working rules

- Keep it small; this is throwaway interview prep.
- Work in stages and stop after each for review. Commit only when asked.
- Ask before installing SDKs or global tools. Use only packages with clear open-source licenses, and list each in the README package table.
- One git repo at the root; never `git init` in a subfolder.
- Server-side validation (`LoanValidator` in LoanCalc.Core) is the source of truth; client checks are a convenience.
- Original design only: no real bank names, logos, or copied markup. Colors come from `--lc-*` CSS custom properties.
- Every real decision gets a DECISIONS.md entry (what, why, revisit). README keeps exact build and run steps.
- If a request won't work in this environment, say so before working around it.

## End of session

1. Run `/reflect` and apply the top change it suggests.
2. Run `/handoff` to write where things stand and what's next.
3. Move any real decision from the handoff into DECISIONS.md, in its existing format (what, why, revisit).
4. Run `/clear`. The next session starts from the handoff doc.

Use `/compact` only when mid-task and out of context.
