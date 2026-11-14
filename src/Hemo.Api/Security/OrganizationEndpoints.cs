using System.Security.Claims;
using Hemo.Application.Abstractions;
using Hemo.Application.Cqrs;
using Hemo.Application.Organization;
using Microsoft.AspNetCore.Antiforgery;

namespace Hemo.Api.Security;

/// <summary>Minimal API transport-only adapter for organization CQRS.</summary>
public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/organization")
            .RequireAuthorization("UsersManageGlobal").WithTags("Organization Admin");

        admin.MapGet("/facilities", async (
            IRequestHandler<ListAllFacilitiesQuery, IReadOnlyList<OrgItem>> handler,
            CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ListAllFacilitiesQuery(), ct)));

        admin.MapPost("/facilities", async (HttpContext http, IAntiforgery anti,
            NewFacilityRequest input,
            IRequestHandler<CreateFacilityCommand, Outcome<OrgItem>> handler,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var result = await handler.Handle(new CreateFacilityCommand(Actor(http), input), ct);
            return result.IsSuccess
                ? Results.Created("/api/v1/admin/organization/facilities", result.Value)
                : OrgError(result.Error!);
        });

        admin.MapPost("/departments", async (HttpContext http, IAntiforgery anti,
            NewDepartmentRequest input,
            IRequestHandler<CreateDepartmentCommand, Outcome<OrgItem>> handler,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var result = await handler.Handle(
                new CreateDepartmentCommand(Actor(http), input), ct);
            return result.IsSuccess
                ? Results.Created("/api/v1/admin/organization/departments", result.Value)
                : OrgError(result.Error!);
        });

        admin.MapPost("/beds", async (HttpContext http, IAntiforgery anti,
            NewBedRequest input,
            IRequestHandler<CreateBedCommand, Outcome<OrgItem>> handler,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var result = await handler.Handle(new CreateBedCommand(Actor(http), input), ct);
            return result.IsSuccess
                ? Results.Created("/api/v1/admin/organization/beds", result.Value)
                : OrgError(result.Error!);
        });

        var scoped = app.MapGroup("/api/v1/organization")
            .RequireAuthorization().WithTags("Organization");

        scoped.MapGet("/facilities", async (HttpContext http,
            IRequestHandler<ListScopedFacilitiesQuery, IReadOnlyList<OrgItem>> handler,
            CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ListScopedFacilitiesQuery(Actor(http)), ct)));

        scoped.MapGet("/facilities/{facilityId:long}/departments", async (
            long facilityId, HttpContext http,
            IRequestHandler<ListDepartmentsQuery, IReadOnlyList<OrgItem>> handler,
            CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ListDepartmentsQuery(Actor(http),
                facilityId), ct)));

        scoped.MapGet("/facilities/{facilityId:long}/departments/{departmentId:long}/beds",
            async (long facilityId, long departmentId, HttpContext http,
                IRequestHandler<ListAvailableBedsQuery, Outcome<IReadOnlyList<OrgItem>>> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(new ListAvailableBedsQuery(
                    Actor(http), facilityId, departmentId), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : OrgError(result.Error!);
            });

        scoped.MapGet("/facilities/{facilityId:long}/access-check", async (
            long facilityId, long? departmentId, HttpContext http,
            IResourcePermission access, CancellationToken ct) =>
            await access.CanAsync(Actor(http), "patient.read", facilityId,
                departmentId, ct)
                ? Results.Ok(new { authorized = true })
                : Results.StatusCode(StatusCodes.Status403Forbidden));
    }

    private static long Actor(HttpContext http) =>
        long.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static IResult OrgError(string code) => code switch
    {
        "forbidden" => Results.Forbid(),
        "duplicate_facility" or "duplicate_department" or "duplicate_bed" =>
            Results.Conflict(new { code }),
        "invalid_facility" or "invalid_department" or "invalid_parent" or "invalid_bed" =>
            Results.BadRequest(new { code }),
        _ => Results.Problem(statusCode: 500, title: "organization_error")
    };
}
