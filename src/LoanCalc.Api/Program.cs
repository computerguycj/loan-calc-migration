using LoanCalc.Core;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

// Every error response (malformed JSON, 404, unhandled exception) is RFC 9457 problem details.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    if (context.Exception is BadHttpRequestException)
        context.ProblemDetails.Detail = "The request body is missing or is not valid JSON for this endpoint.";
});

// The Web Forms host page (another origin) calls this API from the browser. Only listed origins
// may read responses; the list is empty unless configured (see appsettings.Development.json).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .WithMethods("POST")
    .WithHeaders("Content-Type")));

var app = builder.Build();

// First in the pipeline so error responses (including ones the exception handler writes)
// also carry CORS headers; otherwise the browser hides validation errors from the element.
app.UseCors();

// Bad request bodies surface as BadHttpRequestException; keep their 400 instead of turning them into a 500.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = ex => ex is BadHttpRequestException bad ? bad.StatusCode : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();

var loans = app.MapGroup("/api/loans");

loans.MapPost("/compare", Results<Ok<CompareResponse>, ValidationProblem> (CompareRequest request) =>
{
    // LoanCalc.Core is the source of truth for validation; this only shapes its errors for JSON.
    var errors = new Dictionary<string, string[]>();
    AddErrors(errors, "loanA", request.LoanA);
    AddErrors(errors, "loanB", request.LoanB);

    if (errors.Count > 0)
        return TypedResults.ValidationProblem(errors);

    return TypedResults.Ok(new CompareResponse(
        LoanCalculator.Calculate(request.LoanA!),
        LoanCalculator.Calculate(request.LoanB!)));
});

app.Run();

// Keys match the JSON request paths, e.g. "loanA.principal".
static void AddErrors(Dictionary<string, string[]> errors, string prefix, LoanInput? input)
{
    foreach (var group in LoanValidator.Validate(input!).GroupBy(e => e.Field))
    {
        string key = group.Key.Length == 0 ? prefix : $"{prefix}.{char.ToLowerInvariant(group.Key[0])}{group.Key[1..]}";
        errors[key] = group.Select(e => e.Message).ToArray();
    }
}

public record CompareRequest(LoanInput? LoanA, LoanInput? LoanB);

public record CompareResponse(LoanResult LoanA, LoanResult LoanB);
