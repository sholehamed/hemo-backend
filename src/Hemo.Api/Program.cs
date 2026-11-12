using System.Diagnostics;
using Hemo.Api.Security;
using Hemo.Infrastructure;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
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
builder.Services.AddHemoSessionSecurity(builder.Environment.IsDevelopment());
builder.Services.AddHealthChecks()
    .AddDbContextCheck<HemoDbContext>(
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"])
    .AddCheck<IdentitySchemaHealthCheck>(
        name: "sec-schema",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);

var app = builder.Build();

if (args.Contains("--bootstrap-system-admin", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    await AdminBootstrap.RunAsync(
        scope.ServiceProvider.GetRequiredService<HemoDbContext>(),
        scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<AccountUser>>(),
        app.Configuration);
    Console.WriteLine("Initial system administrator provisioned.");
    return;
}

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

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

app.MapAuthEndpoints();

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
