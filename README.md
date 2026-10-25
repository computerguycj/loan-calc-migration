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
| `src/LoanCalc.Gateway` | YARP reverse proxy: one origin for both apps | Stage 5 ✅ |
| `Default.aspx` islands | Vue elements inside the Web Forms page: `<loan-results>` (6a), `<term-picker>` (6b), both inside an UpdatePanel (6c) | Stage 6 ✅ |

## Ports

| App | URL | Who opens it |
|---|---|---|
| LoanCalc.Gateway | http://localhost:5000 | **you, in the browser** |
| LoanCalc.WebForms (IIS Express) | http://localhost:5080 | the gateway |
| LoanCalc.Api | http://localhost:5090 | the gateway |
| Vite dev server (optional) | http://localhost:5173 | you, when working on the element alone |

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

## Run everything through the gateway (Stages 4 and 5)

`CompareVue.aspx` is served by the Web Forms site. It loads the `<loan-compare>` bundle, which calls
`/api/loans/compare` on the same origin. `LoanCalc.Gateway` makes that work: it sends `/api/*` to the API and
every other path to Web Forms. Open the site through the gateway.

1. Build the custom elements (from the repo root). This writes
   `src/LoanCalc.WebForms/Scripts/loan-compare/loan-compare.js`, which isn't checked in. Both pages load it:
   `CompareVue.aspx` for `<loan-compare>`, and `Default.aspx` for its `<loan-results>` and `<term-picker>` islands
   (without the bundle, `Default.aspx` still works, with the server-rendered table and a plain Term dropdown):

   ```bash
   npm --prefix src/loan-calc-vue ci
   ```

   ```bash
   npm --prefix src/loan-calc-vue run build
   ```

2. Start the Web Forms site as in Stage 2 (VS F5, or MSBuild + IIS Express). Leave it running.
3. In a second terminal, start the API and leave it running:

   ```bash
   dotnet run --project src/LoanCalc.Api
   ```

4. In a third terminal, start the gateway and leave it running:

   ```bash
   dotnet run --project src/LoanCalc.Gateway
   ```

5. Open http://localhost:5000/. Both pages work there: `Default.aspx` (Web Forms postback) and
   `CompareVue.aspx` (Vue). Each page links to the other.

Routes live in `src/LoanCalc.Gateway/appsettings.json` under `ReverseProxy`. To move another path to
ASP.NET Core, add a route for it there. Everything without a route still goes to Web Forms.

**Don't open `CompareVue.aspx` on port 5080 directly.** `LoanApiBaseUrl` in `Web.config` is empty, so the element
calls `/api` on port 5080. IIS Express has no `/api`, so the element shows "The loan service returned 404."
`Default.aspx` still works on 5080, because it doesn't call the API.

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
| Yarp.ReverseProxy 2.3.0 | LoanCalc.Gateway | MIT |
| System.IO.Hashing 8.0.0 (transitive, via YARP) | LoanCalc.Gateway | MIT |

`LoanCalc.Core`, `LoanCalc.WebForms`, and `LoanCalc.Api` have no package dependencies. `LoanCalc.WebForms`
references `System.Web.Extensions`, which ships with .NET Framework. The API and gateway
also use the ASP.NET Core shared framework (MIT).

npm packages in `src/loan-calc-vue` (versions pinned in `package.json`):

| Package | Kind | License |
|---|---|---|
| vue 3.5.44 | runtime (the only code in the shipped bundle) | MIT |
| vite 8.3.5 | build tool | MIT |
| @vitejs/plugin-vue 6.0.10 | build tool | MIT |

Their transitive dependencies (build-time only, checked against `node_modules`) are all open source:
MIT (most of them, including rolldown, postcss, and @vue/*), Apache-2.0 (detect-libc),
BSD-2-Clause (entities), BSD-3-Clause (source-map-js), ISC (picocolors), and MPL-2.0 (lightningcss).
