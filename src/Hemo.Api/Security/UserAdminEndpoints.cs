using System.Security.Claims;
using Hemo.Application.Cqrs;
using Hemo.Application.Users;
using Microsoft.AspNetCore.Antiforgery;

namespace Hemo.Api.Security;

/// <summary>User management Minimal API; only transport, policy and antiforgery live here.</summary>
public static class UserAdminEndpoints
{
    public static void MapUserAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin/users")
            .RequireAuthorization("UsersManageGlobal").WithTags("User Admin");

        group.MapGet("/", async (string? search, int? page,
            IRequestHandler<ListManagedUsersQuery, Outcome<IReadOnlyList<ManagedUser>>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(
                new ListManagedUsersQuery(search, page ?? 1), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : UserError(result.Error!);
        });

        group.MapGet("/roles", async (
            IRequestHandler<ListManagedRolesQuery, IReadOnlyList<ManagedRole>> handler,
            CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ListManagedRolesQuery(), ct)));

        group.MapPost("/", async (CreateUserRequest input, HttpContext http,
            IAntiforgery anti,
            IRequestHandler<CreateManagedUserCommand, Outcome<ManagedUser>> handler,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var result = await handler.Handle(new CreateManagedUserCommand(
                Actor(http), input), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/admin/users/{result.Value!.Id}", result.Value)
                : UserError(result.Error!);
        });

        group.MapPost("/{userId:long}/grants", async (long userId,
            GrantUserRoleRequest input, HttpContext http, IAntiforgery anti,
            IRequestHandler<GrantManagedRoleCommand, Outcome<GrantReceipt>> handler,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var result = await handler.Handle(new GrantManagedRoleCommand(
                Actor(http), userId, input), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/admin/users/{userId}/grants", result.Value)
                : UserError(result.Error!);
        });

        group.MapPost("/{userId:long}/deactivate", async (long userId,
            HttpContext http, IAntiforgery anti,
            IRequestHandler<DeactivateManagedUserCommand, Outcome<bool>> handler,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var result = await handler.Handle(new DeactivateManagedUserCommand(
                Actor(http), userId), ct);
            return result.IsSuccess ? Results.NoContent() : UserError(result.Error!);
        });
    }

    private static long Actor(HttpContext http) =>
        long.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static IResult UserError(string code) => code switch
    {
        "not_found" => Results.NotFound(),
        "grant_exists" or "duplicate_user" or "last_system_admin" =>
            Results.Conflict(new { code }),
        "invalid_user" =>
            Results.ValidationProblem(new Dictionary<string, string[]>
            { ["user"] = ["مشخصات کاربر یا رمز اولیه معتبر نیست."] }),
        "search_too_long" or "invalid_assignment" or "invalid_system_scope" or
        "facility_required" or "invalid_department_scope" =>
            Results.BadRequest(new { code }),
        _ => Results.Problem(statusCode: 500, title: "user_admin_error")
    };
}
