// One origin for the whole app: /api goes to the new ASP.NET Core API, everything else to the
// legacy Web Forms site. Routes and destinations live in appsettings.json ("ReverseProxy").
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapReverseProxy();

app.Run();
