using System.Globalization;
using System.Security.Cryptography;
using System.Security.Claims;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

namespace Hemo.Api.Security;

public static class SessionSecurity
{
    public const string Scheme = "Hemo.Session";

    public static IServiceCollection AddHemoSessionSecurity(
        this IServiceCollection services, bool isDevelopment)
    {
        services.AddScoped<IPasswordHasher<AccountUser>, PasswordHasher<AccountUser>>();
        services.AddScoped<HemoAccessService>();
        services.AddScoped<ResourceAccessService>();
        services.AddScoped<HemoAuditWriter>();

        services.AddAuthentication(Scheme)
            .AddCookie(Scheme, options =>
            {
                options.Cookie.Name = "Hemo.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = isDevelopment
                    ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
                options.Events.OnValidatePrincipal = async ctx =>
                {
                    var idText = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!long.TryParse(idText, NumberStyles.None,
                        CultureInfo.InvariantCulture, out var userId))
                    {
                        ctx.RejectPrincipal();
                        return;
                    }

                    var db = ctx.HttpContext.RequestServices.GetRequiredService<HemoDbContext>();
                    var sessionText = ctx.Principal?.FindFirst("hemo.session")?.Value;
                    if (!long.TryParse(sessionText, out var sessionId))
                    {
                        ctx.RejectPrincipal();
                        return;
                    }

                    var isActive = await db.AccountUsers.AsNoTracking()
                        .AnyAsync(u => u.AccountUserId == userId && u.IsActive);
                    var validSession = await db.UserSessions.AsNoTracking()
                        .AnyAsync(x => x.UserSessionId == sessionId
                            && x.AccountUserId == userId
                            && x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow);
                    if (!isActive || !validSession)
                        ctx.RejectPrincipal();
                };
            });

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "Hemo.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = isDevelopment
                ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });

        services.AddAuthorizationBuilder()
            .AddPolicy("UsersManageGlobal", policy =>
                policy.RequireAuthenticatedUser().AddRequirements(
                    new GlobalPermissionRequirement("users.manage")));
        services.AddScoped<IAuthorizationHandler, GlobalPermissionHandler>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", http =>
                RateLimitPartition.GetFixedWindowLimiter(
                    http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 8, Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0, AutoReplenishment = true
                    }));
        });

        return services;
    }
}

public sealed record GlobalPermissionRequirement(string Code) : IAuthorizationRequirement;

public sealed class GlobalPermissionHandler(HemoAccessService access)
    : AuthorizationHandler<GlobalPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, GlobalPermissionRequirement requirement)
    {
        if (long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId) &&
            await access.HasGlobalPermissionAsync(userId, requirement.Code))
            context.Succeed(requirement);
    }
}
