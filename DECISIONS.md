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
