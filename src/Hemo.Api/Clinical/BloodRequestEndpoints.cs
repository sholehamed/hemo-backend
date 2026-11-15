using System.Security.Claims;
using Hemo.Api.Security;
using Hemo.Application.Clinical;
using Hemo.Application.Cqrs;
using Microsoft.AspNetCore.Antiforgery;

namespace Hemo.Api.Clinical;

public static class BloodRequestEndpoints
{
    public static void MapBloodRequestEndpoints(this WebApplication app)
    {
        var group=app.MapGroup(
            "/api/v1/facilities/{facilityId:long}/departments/{departmentId:long}/blood-requests")
            .RequireAuthorization().WithTags("Blood Requests");
        group.MapGet("/products",async (long facilityId,long departmentId,HttpContext http,
            IRequestHandler<ListProductTypesQuery,Outcome<IReadOnlyList<ProductTypeOption>>> handler,
            CancellationToken ct)=>{
            var result=await handler.Handle(new ListProductTypesQuery(
                Actor(http),facilityId,departmentId),ct);
            return result.IsSuccess?Results.Ok(result.Value):Error(result.Error!);
        });
        group.MapGet("/",async (long facilityId,long departmentId,int? page,
            string? status,HttpContext http,
            IRequestHandler<ListBloodRequestsQuery,Outcome<BloodRequestPage>> handler,
            CancellationToken ct)=>{
            var result=await handler.Handle(new ListBloodRequestsQuery(
                Actor(http),facilityId,departmentId,page??1,status),ct);
            return result.IsSuccess?Results.Ok(result.Value):Error(result.Error!);
        });
        group.MapGet("/{requestId:long}",async (long facilityId,long departmentId,
            long requestId,HttpContext http,
            IRequestHandler<GetBloodRequestQuery,Outcome<BloodRequestView>> handler,
            CancellationToken ct)=>{
            var result=await handler.Handle(new GetBloodRequestQuery(
                Actor(http),facilityId,departmentId,requestId),ct);
            return result.IsSuccess?Results.Ok(result.Value):Error(result.Error!);
        });
        group.MapPost("/",async(long facilityId,long departmentId,
            NewBloodRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<SubmitBloodRequestCommand,Outcome<BloodRequestView>> handler,
            CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))
                return Results.BadRequest(new{code="invalid_csrf"});
            var result=await handler.Handle(new SubmitBloodRequestCommand(
                Actor(http),facilityId,departmentId,input),ct);
            return result.IsSuccess?Results.Created(
                $"/api/v1/facilities/{facilityId}/departments/{departmentId}/blood-requests/{result.Value!.BloodRequestId}",
                result.Value):Error(result.Error!);
        });
        group.MapPost("/{requestId:long}/review",async(long facilityId,long departmentId,
            long requestId,ReviewBloodRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<ReviewBloodRequestCommand,Outcome<BloodRequestView>> handler,
            CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))
                return Results.BadRequest(new{code="invalid_csrf"});
            var result=await handler.Handle(new ReviewBloodRequestCommand(
                Actor(http),facilityId,departmentId,requestId,input),ct);
            return result.IsSuccess?Results.Ok(result.Value):Error(result.Error!);
        });
    }

    private static long Actor(HttpContext http)=>
        long.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static IResult Error(string code)=>code switch
    {
        "forbidden" or "clinical_role_required" or "doctor_required" or
            "self_approval_prohibited"=>Results.Forbid(),
        "not_found" or "encounter_not_found"=>Results.NotFound(),
        "invalid_request" or "invalid_product" or "invalid_review" or
            "invalid_rowversion"=>Results.BadRequest(new{code}),
        "identity_not_verified" or "concurrency_conflict" or "invalid_state" =>
            Results.Conflict(new{code}),
        _=>Results.Problem(statusCode:500,title:"clinical_request_error")
    };
}
