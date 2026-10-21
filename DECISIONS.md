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
