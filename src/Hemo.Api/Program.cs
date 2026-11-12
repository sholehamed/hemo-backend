using System.Diagnostics;
using Hemo.Infrastructure;
using Hemo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
});

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<HemoDbContext>(
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Stage 1 exposes only infrastructure endpoints. NO fake login, auth, or clinical APIs.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthAsync
}).WithTags("Monitoring");

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthAsync
}).WithTags("Monitoring");

app.MapGet("/api/v1/system/status", (HttpContext context) =>
    Results.Ok(new SystemStatusResponse(
        "hemo-api",
        "running",
        DateTimeOffset.UtcNow,
        Activity.Current?.Id ?? context.TraceIdentifier)))
    .WithName("GetSystemStatus")
    .WithTags("System")
    .Produces<SystemStatusResponse>(StatusCodes.Status200OK);

app.Run();

static Task WriteHealthAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";
    return context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(x => new
        {
            name = x.Key,
            status = x.Value.Status.ToString()
        })
    });
}

public sealed record SystemStatusResponse(
    string Service,
    string Status,
    DateTimeOffset UtcTime,
    string TraceId);

public partial class Program { }
