using System.Security.Claims;
using Hemo.Application.Cqrs;
using Hemo.Application.Patients;
using Hemo.Api.Security;
using Microsoft.AspNetCore.Antiforgery;

namespace Hemo.Api.Patients;

/// <summary>
/// Thin Minimal API adapter. Commands and queries own authorization/orchestration;
/// handlers invoke persistence through application ports. HTTP owns CSRF and status mapping.
/// Existing URLs and response DTOs stay backward compatible with ST-04.
/// </summary>
public static class AdmissionEndpoints
{
    public static void MapAdmissionEndpoints(this WebApplication app)
    {
        var routes = app.MapGroup(
            "/api/v1/facilities/{facilityId:long}/departments/{departmentId:long}/admissions")
            .RequireAuthorization().WithTags("Admissions");

        routes.MapGet("/", async (long facilityId, long departmentId, string? search,
            int? page, HttpContext http,
            IRequestHandler<ListAdmissionsQuery, AdmissionOperation<AdmissionPage>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new ListAdmissionsQuery(
                Actor(http), facilityId, departmentId, search, page ?? 1), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Error(result.Error!);
        });

        routes.MapGet("/{encounterId:long}", async (long facilityId, long departmentId,
            long encounterId, HttpContext http,
            IRequestHandler<GetAdmissionQuery, AdmissionOperation<AdmissionView>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetAdmissionQuery(
                Actor(http), facilityId, departmentId, encounterId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Error(result.Error!);
        });

        routes.MapPost("/", async (long facilityId, long departmentId,
            CreateAdmissionRequest input, HttpContext http, IAntiforgery anti,
            IRequestHandler<CreateAdmissionCommand, AdmissionOperation<AdmissionView>> handler,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var result = await handler.Handle(new CreateAdmissionCommand(
                Actor(http), facilityId, departmentId, input), ct);
            return result.IsSuccess
                ? Results.Created(
                    $"/api/v1/facilities/{facilityId}/departments/{departmentId}/admissions/{result.Value!.EncounterId}",
                    result.Value)
                : Error(result.Error!);
        });

        routes.MapPut("/{encounterId:long}/patient", async (
            long facilityId, long departmentId, long encounterId, UpdatePatientRequest input,
            HttpContext http, IAntiforgery anti,
            IRequestHandler<UpdatePatientCommand, AdmissionOperation<AdmissionView>> handler,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var result = await handler.Handle(new UpdatePatientCommand(
                Actor(http), facilityId, departmentId, encounterId, input), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Error(result.Error!);
        });

        routes.MapPost("/{encounterId:long}/wristbands", async (
            long facilityId, long departmentId, long encounterId, HttpContext http,
            IAntiforgery anti,
            IRequestHandler<IssueWristbandCommand, AdmissionOperation<WristbandView>> handler,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var result = await handler.Handle(new IssueWristbandCommand(
                Actor(http), facilityId, departmentId, encounterId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Error(result.Error!);
        });

        routes.MapPost("/{encounterId:long}/identity-verifications", async (
            long facilityId, long departmentId, long encounterId,
            IdentityVerificationRequest input, HttpContext http, IAntiforgery anti,
            IRequestHandler<VerifyIdentityCommand, AdmissionOperation<IdentityVerificationView>> handler,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var result = await handler.Handle(new VerifyIdentityCommand(
                Actor(http), facilityId, departmentId, encounterId, input), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Error(result.Error!);
        });
    }

    private static long Actor(HttpContext http) =>
        long.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static IResult Error(string code) => code switch
    {
        "forbidden" => Results.Forbid(),
        "not_found" => Results.NotFound(),
        "duplicate_identifier" or "bed_occupied" or "constraint_conflict" or
        "concurrency_conflict" or "invalid_state" or "no_wristband" =>
            Results.Conflict(new { code }),
        "invalid_input" or "invalid_department" or "invalid_bed" or
        "invalid_rowversion" => Results.BadRequest(new { code }),
        _ => Results.Problem(statusCode: 500, title: "admission_error")
    };
}
