using System.Security.Claims;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Api.Security;

public sealed record LoginRequest(string UserName, string Password);
public sealed record SessionResponse(long UserId, string UserName, string DisplayName,
    IReadOnlyList<RoleGrant> Grants);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapGet("/csrf", (HttpContext http, IAntiforgery antiforgery,
            IWebHostEnvironment environment) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(http);
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

        group.MapPost("/login", async (
            HttpContext http, LoginRequest? input, IAntiforgery anti,
            HemoDbContext db, IPasswordHasher<AccountUser> hasher,
            CancellationToken ct) =>
        {
            if (!await ValidCsrfAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });

            if (input is null ||
                string.IsNullOrWhiteSpace(input.UserName) ||
                string.IsNullOrEmpty(input.Password) ||
                input.UserName.Length > 100 || input.Password.Length > 512)
                return Results.Unauthorized();

            var username = input.UserName.Trim();
            var user = await db.AccountUsers.SingleOrDefaultAsync(
                x => x.UserName == username, ct);
            if (user is null || !user.IsActive)
                return Results.Unauthorized();

            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, input.Password);
            if (result == PasswordVerificationResult.Failed)
                return Results.Unauthorized();

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = hasher.HashPassword(user, input.Password);
                await db.SaveChangesAsync(ct);
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.AccountUserId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };
            var identity = new ClaimsIdentity(claims, SessionSecurity.Scheme);
            await http.SignInAsync(SessionSecurity.Scheme, new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });
            http.Response.Headers.CacheControl = "no-store";
            return Results.NoContent();
        }).AllowAnonymous().RequireRateLimiting("login");

        group.MapGet("/session", async (
            ClaimsPrincipal principal, HemoDbContext db, HemoAccessService access,
            CancellationToken ct) =>
        {
            var id = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await db.AccountUsers.AsNoTracking()
                .SingleAsync(x => x.AccountUserId == id && x.IsActive, ct);
            var grants = await access.GetGrantsAsync(id, ct);
            return Results.Ok(new SessionResponse(
                user.AccountUserId, user.UserName, user.DisplayName, grants));
        }).RequireAuthorization();

        group.MapPost("/logout", async (HttpContext http, IAntiforgery anti) =>
        {
            if (!await ValidCsrfAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });

            await http.SignOutAsync(SessionSecurity.Scheme);
            http.Response.Headers.CacheControl = "no-store";
            return Results.NoContent();
        }).RequireAuthorization();

        // Proves server-side authorization is based on the current sec.* grants,
        // not Angular navigation visibility or untrusted client role selection.
        app.MapGet("/api/v1/system/access-check",
            () => Results.Ok(new { authorized = true }))
            .RequireAuthorization("UsersManageGlobal")
            .WithTags("System");
    }

    private static async Task<bool> ValidCsrfAsync(
        HttpContext http, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(http);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }
}
