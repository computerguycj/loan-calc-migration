# LoanCalc

A practice project for rehearsing an incremental migration from ASP.NET Web Forms to
Vue + ASP.NET Core. The feature is a two-loan mortgage comparison calculator.

Throwaway code for interview prep. Not financial advice.

## Layout

| Path | What | Status |
|---|---|---|
| `src/LoanCalc.Core` | netstandard2.0 library: all loan math and validation | Stage 1 ✅ |
| `tests/LoanCalc.Core.Tests` | xUnit tests, run on net10.0 and net48 | Stage 1 ✅ |
| `src/LoanCalc.WebForms` | .NET Framework 4.8 Web Forms site | Stage 2 |
| `src/LoanCalc.Api` | ASP.NET Core minimal API | Stage 3 |
| `src/loan-calc-vue` | Vite + Vue 3 `<loan-compare>` custom element | Stage 4 |

## Prerequisites

- Windows with .NET Framework 4.8
- .NET SDK 10.0.x (tested with 10.0.401)
- Visual Studio 2026 with the "ASP.NET and web development" workload (for the Web Forms site)
- Node.js 24.x and npm 11.x (for the Vue element)

## Build and test (Stage 1)

From the repo root:

```bash
dotnet test LoanCalc.slnx
```

This builds `LoanCalc.Core` and runs the test suite twice, once on .NET 10 and once on
.NET Framework 4.8.

## How the numbers are calculated

- **Monthly payment:** the standard annuity formula, rounded to the cent. A 0% rate is
  simply principal ÷ months. The extra payment is not included in this figure.
- **Schedule:** each month's interest is rounded to the cent. The extra payment goes to
  principal. The final payment is whatever clears the balance, so rounding drift never adds up.
- **Total interest:** the sum of the schedule's interest, after any extra payments.
- **Total payback:** all payments plus the up-front fees.
- **APR:** the annual rate at which the present value of the *scheduled* payments equals
  the amount financed (principal − fees). See [DECISIONS.md](DECISIONS.md).

## Third-party packages

| Package | Used by | License |
|---|---|---|
| xunit | LoanCalc.Core.Tests | Apache-2.0 |
| xunit.runner.visualstudio | LoanCalc.Core.Tests | Apache-2.0 |
| Microsoft.NET.Test.Sdk | LoanCalc.Core.Tests | MIT |

`LoanCalc.Core` has no package dependencies.
