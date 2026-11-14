using System.Security.Claims;
using Hemo.Application.Patients;
using Hemo.Infrastructure.Patients;
using Hemo.Infrastructure.Security;
using Microsoft.AspNetCore.Antiforgery;
using Hemo.Api.Security;

namespace Hemo.Api.Patients;

/// <summary>Facility+department routes are checked against actual persisted resources.</summary>
public static class AdmissionEndpoints
{
    public static void MapAdmissionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(
            "/api/v1/facilities/{facilityId:long}/departments/{departmentId:long}/admissions")
            .RequireAuthorization().WithTags("Admissions");

        group.MapGet("/", async (long facilityId, long departmentId,
            string? search, int? page, HttpContext http, ResourceAccessService access,
            AdmissionService service, CancellationToken ct) =>
        {
            if (!await access.CanAsync(CurrentId(http), "patient.read", facilityId, departmentId, ct))
                return Results.Forbid();
            return Results.Ok(await service.ListAsync(
                facilityId, departmentId, search, page ?? 1, ct));
        });

        group.MapGet("/{encounterId:long}", async (long facilityId, long departmentId,
            long encounterId, HttpContext http, ResourceAccessService access,
            AdmissionService service, CancellationToken ct) =>
        {
            if (!await access.CanAsync(CurrentId(http), "patient.read", facilityId, departmentId, ct))
                return Results.Forbid();
            var view = await service.GetAsync(facilityId, departmentId, encounterId, ct);
            return view is null ? Results.NotFound() : Results.Ok(view);
        });

        group.MapPost("/", async (long facilityId, long departmentId,
            CreateAdmissionRequest input, HttpContext http, IAntiforgery anti,
            ResourceAccessService access, AdmissionService service, CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            if (!await access.CanAsync(CurrentId(http), "patient.create", facilityId, departmentId, ct))
                return Results.Forbid();

            var result = await service.CreateAsync(
                facilityId, departmentId, CurrentId(http), input, ct);
            return !result.IsSuccess ? Error(result.Error!) :
                Results.Created(
                    $"/api/v1/facilities/{facilityId}/departments/{departmentId}/admissions/{result.Value!.EncounterId}",
                    result.Value);
        });

        group.MapPut("/{encounterId:long}/patient", async (
            long facilityId, long departmentId, long encounterId,
            UpdatePatientRequest input, HttpContext http, IAntiforgery anti,
            ResourceAccessService access, AdmissionService service, CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            if (!await access.CanAsync(CurrentId(http), "patient.update", facilityId, departmentId, ct))
                return Results.Forbid();
            var result = await service.UpdatePatientAsync(facilityId, departmentId,
                encounterId, CurrentId(http), input, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Error(result.Error!);
        });

        group.MapPost("/{encounterId:long}/wristbands", async (
            long facilityId, long departmentId, long encounterId,
            HttpContext http, IAntiforgery anti, ResourceAccessService access,
            AdmissionService service, CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            if (!await access.CanAsync(CurrentId(http), "patient.update", facilityId, departmentId, ct))
                return Results.Forbid();
            var result = await service.IssueWristbandAsync(
                facilityId, departmentId, encounterId, CurrentId(http), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Error(result.Error!);
        });

        group.MapPost("/{encounterId:long}/identity-verifications", async (
            long facilityId, long departmentId, long encounterId,
            IdentityVerificationRequest input, HttpContext http, IAntiforgery anti,
            ResourceAccessService access, AdmissionService service, CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            if (!await access.CanAsync(CurrentId(http), "patient.identity.verify",
                facilityId, departmentId, ct))
                return Results.Forbid();
            var result = await service.VerifyIdentityAsync(
                facilityId, departmentId, encounterId, CurrentId(http), input, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Error(result.Error!);
        });
    }

    private static long CurrentId(HttpContext http) =>
        long.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static IResult Error(string code) => code switch
    {
        "not_found" => Results.NotFound(),
        "duplicate_identifier" or "bed_occupied" or "constraint_conflict" or
            "concurrency_conflict" or "invalid_state" or "no_wristband" =>
            Results.Conflict(new { code }),
        "invalid_input" or "invalid_department" or "invalid_bed" or
            "invalid_rowversion" => Results.BadRequest(new { code }),
        _ => Results.Problem(statusCode: 500, title: "admission_error")
    };
}
