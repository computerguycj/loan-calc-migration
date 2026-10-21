# LoanCalc

A practice project for rehearsing an incremental migration from ASP.NET Web Forms to
Vue + ASP.NET Core. The feature is a two-loan mortgage comparison calculator.

Throwaway code for interview prep. Not financial advice.

## Layout

| Path | What | Status |
|---|---|---|
| `src/LoanCalc.Core` | netstandard2.0 library: all loan math and validation | Stage 1 ✅ |
| `tests/LoanCalc.Core.Tests` | xUnit tests, run on net10.0 and net48 | Stage 1 ✅ |
| `src/LoanCalc.WebForms` | .NET Framework 4.8 Web Forms site (postback) | Stage 2 ✅ |
| `src/LoanCalc.Api` | ASP.NET Core minimal API | Stage 3 ✅ |
| `src/loan-calc-vue` | Vite + Vue 3 `<loan-compare>` custom element, hosted by `CompareVue.aspx` | Stage 4 ✅ |

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

This builds every project in the solution and runs the test suite twice, once on .NET 10 and
once on .NET Framework 4.8.

## Run the Web Forms site (Stage 2)

**From Visual Studio:** open `LoanCalc.slnx`, set `LoanCalc.WebForms` as the startup project,
and press F5. It runs on IIS Express at http://localhost:5080/.

**From the command line:** build with Visual Studio's MSBuild, then start IIS Express:

```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" src\LoanCalc.WebForms\LoanCalc.WebForms.csproj -restore
```

```bash
"C:\Program Files\IIS Express\iisexpress.exe" /path:C:\dci\src\LoanCalc.WebForms /port:5080
```

Then open http://localhost:5080/Default.aspx. Press `Q` in the IIS Express window to stop it.

`dotnet build` also compiles the Web Forms project, but it skips the Visual Studio web targets.
Those targets aren't needed to run the site locally, but publishing needs them, so use MSBuild for that.

## Run the API (Stage 3)

```bash
dotnet run --project src/LoanCalc.Api
```

It listens on http://localhost:5090. To try it, open `src/LoanCalc.Api/LoanCalc.Api.http` in
Visual Studio and click "Send request", or use curl:

```bash
curl -i -H "Content-Type: application/json" -d "{\"loanA\":{\"principal\":300000,\"annualRatePercent\":6.5,\"termYears\":30,\"fees\":6000},\"loanB\":{\"principal\":300000,\"annualRatePercent\":6.125,\"termYears\":30,\"fees\":9000,\"extraMonthlyPayment\":200}}" http://localhost:5090/api/loans/compare
```

### `POST /api/loans/compare`

Request:

```json
{
  "loanA": { "principal": 300000, "annualRatePercent": 6.5, "termYears": 30, "fees": 6000, "extraMonthlyPayment": 0 },
  "loanB": { "principal": 300000, "annualRatePercent": 6.125, "termYears": 30, "fees": 9000, "extraMonthlyPayment": 200 }
}
```

Responses:

- **200:** `{ "loanA": { "monthlyPayment", "totalInterest", "totalPayback", "aprPercent", "payoffMonths" }, "loanB": { … } }`
- **400, validation:** problem details with `errors` keyed by request path, e.g.
  `{ "errors": { "loanA.annualRatePercent": ["Interest rate must be between 0 and 30%."], "loanB": ["Loan details are required."] } }`
- **400, bad body:** problem details when the body is missing or isn't valid JSON (e.g. `"principal": "lots"`).

## Run the Vue page (Stage 4)

`CompareVue.aspx` is served by the Web Forms site. It loads the `<loan-compare>` bundle, which calls the API
directly. It needs three things: the bundle built, the API running, and the Web Forms site running.

1. Build the custom element (from the repo root). This writes
   `src/LoanCalc.WebForms/Scripts/loan-compare/loan-compare.js`, which isn't checked in:

   ```bash
   npm --prefix src/loan-calc-vue ci
   ```

   ```bash
   npm --prefix src/loan-calc-vue run build
   ```

2. Start the API (leave it running): `dotnet run --project src/LoanCalc.Api`
3. Start the Web Forms site as in Stage 2 (VS F5, or MSBuild + IIS Express).
4. Open http://localhost:5080/CompareVue.aspx. Each page links to the other.

The page reads the API address from `LoanApiBaseUrl` in `src/LoanCalc.WebForms/Web.config`. The API allows
that origin through `Cors:AllowedOrigins` in `src/LoanCalc.Api/appsettings.Development.json`.

### Working on the element by itself

With the API running:

```bash
npm --prefix src/loan-calc-vue run dev
```

Then open http://localhost:5173. The Vite dev server hot-reloads, and it forwards `/api` to the API, so no CORS is involved.

### Re-theming

The element reads the same `--lc-*` custom properties as `Content/site.css`, and they inherit through the
shadow root. Override them on the host page, e.g. `:root { --lc-accent: #6b2f8a; }`. With no theme set,
the element falls back to its built-in defaults.

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

`LoanCalc.Core`, `LoanCalc.WebForms`, and `LoanCalc.Api` have no package dependencies. The API uses only
the ASP.NET Core shared framework (MIT).

npm packages in `src/loan-calc-vue` (versions pinned in `package.json`):

| Package | Kind | License |
|---|---|---|
| vue 3.5.44 | runtime (the only code in the shipped bundle) | MIT |
| vite 8.3.5 | build tool | MIT |
| @vitejs/plugin-vue 6.0.10 | build tool | MIT |

Their transitive dependencies (build-time only, checked against `node_modules`) are all open source:
MIT (most of them, including rolldown, postcss, and @vue/*), Apache-2.0 (detect-libc),
BSD-2-Clause (entities), BSD-3-Clause (source-map-js), ISC (picocolors), and MPL-2.0 (lightningcss).
