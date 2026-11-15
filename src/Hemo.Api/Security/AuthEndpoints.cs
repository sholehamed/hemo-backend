using System.Security.Claims;
using Hemo.Application.Authentication;
using Hemo.Application.Cqrs;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;

namespace Hemo.Api.Security;

/// <summary>Thin HTTP/BFF adapter: no SQL or credentials verification in the API layer.</summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapGet("/csrf", (HttpContext http, IAntiforgery anti,
            IWebHostEnvironment environment) =>
        {
            var tokens = anti.GetAndStoreTokens(http);
            http.Response.Headers.CacheControl = "no-store";
            http.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
                new CookieOptions
                {
                    HttpOnly = false, IsEssential = true,
                    SameSite = SameSiteMode.Strict, Path = "/",
                    Secure = !environment.IsDevelopment()
                });
            return Results.NoContent();
        }).AllowAnonymous();

        group.MapPost("/login", async (HttpContext http, LoginRequest? input,
            IAntiforgery anti,
            IRequestHandler<AuthenticateCommand, Outcome<SignInTicket>> handler,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            if (input is null) return Results.Unauthorized();
            var result = await handler.Handle(new AuthenticateCommand(
                input.UserName, input.Password,
                http.Connection.RemoteIpAddress?.ToString()), ct);
            if (!result.IsSuccess) return Results.Unauthorized();
            var ticket = result.Value!;
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ticket.UserId.ToString()),
                new Claim(ClaimTypes.Name, ticket.UserName),
                new Claim("hemo.session", ticket.SessionId.ToString())
            };
            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, SessionSecurity.Scheme));
            await http.SignInAsync(SessionSecurity.Scheme, principal,
                new AuthenticationProperties
                {
                    IsPersistent = false, AllowRefresh = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });
            http.Response.Headers.CacheControl = "no-store";
            return Results.NoContent();
        }).AllowAnonymous().RequireRateLimiting("login");

        group.MapGet("/session", async (ClaimsPrincipal principal,
            IRequestHandler<GetSessionQuery, Outcome<SessionView>> handler,
            CancellationToken ct) =>
        {
            if (!long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId)) return Results.Unauthorized();
            var result = await handler.Handle(new GetSessionQuery(userId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
        }).RequireAuthorization();

        group.MapPost("/logout", async (HttpContext http, IAntiforgery anti,
            IRequestHandler<RevokeSessionCommand, Outcome<bool>> handler,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            if (!long.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId) || !long.TryParse(
                    http.User.FindFirst("hemo.session")?.Value, out var sessionId))
                return Results.Unauthorized();
            var result = await handler.Handle(new RevokeSessionCommand(userId, sessionId), ct);
            if (!result.IsSuccess) return Results.Unauthorized();
            await http.SignOutAsync(SessionSecurity.Scheme);
            http.Response.Headers.CacheControl = "no-store";
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapGet("/api/v1/system/access-check",
            () => Results.Ok(new { authorized = true }))
            .RequireAuthorization("UsersManageGlobal").WithTags("System");
    }
}

public sealed record LoginRequest(string UserName, string Password);
