using System.Security.Claims;
using Hemo.Api.Security;
using Hemo.Application.Blood;
using Hemo.Application.Cqrs;
using Microsoft.AspNetCore.Antiforgery;

namespace Hemo.Api.Blood;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        var group=app.MapGroup("/api/v1/facilities/{facilityId:long}/blood-bank")
            .RequireAuthorization().WithTags("Blood Bank Inventory");
        group.MapGet("/units",async(long facilityId,HttpContext http,
            IRequestHandler<ListStockQuery,Outcome<IReadOnlyList<InventoryUnitView>>> handler,
            CancellationToken ct)=>Respond(await handler.Handle(new(Actor(http),facilityId),ct)));
        group.MapGet("/blood-groups",async(long facilityId,HttpContext http,
            IRequestHandler<ListBloodGroupsQuery,Outcome<IReadOnlyList<BloodGroupOption>>> handler,
            CancellationToken ct)=>Respond(await handler.Handle(new(Actor(http),facilityId),ct)));
        group.MapGet("/worklist",async(long facilityId,HttpContext http,
            IRequestHandler<ListBankWorkQuery,Outcome<IReadOnlyList<BankWorkItem>>> handler,
            CancellationToken ct)=>Respond(await handler.Handle(new(Actor(http),facilityId),ct)));
        group.MapPost("/units",async(long facilityId,RegisterUnitRequest input,HttpContext http,
            IAntiforgery anti,IRequestHandler<ReceiveStockCommand,Outcome<InventoryUnitView>> handler,
            CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))
                return Results.BadRequest(new{code="invalid_csrf"});
            var result=await handler.Handle(new(Actor(http),facilityId,input),ct);
            return result.IsSuccess?Results.Created(
                $"/api/v1/facilities/{facilityId}/blood-bank/units",result.Value)
                :Fail(result.Error!);
        });
        group.MapPost("/reservations",async(long facilityId,ReserveUnitRequest input,
            HttpContext http,IAntiforgery anti,
            IRequestHandler<ReserveStockCommand,Outcome<long>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))
                return Results.BadRequest(new{code="invalid_csrf"});
            var result=await handler.Handle(new(Actor(http),facilityId,input),ct);
            return Respond(result);
        });
        group.MapPost("/crossmatches",async(long facilityId,RecordCrossmatchRequest input,
            HttpContext http,IAntiforgery anti,
            IRequestHandler<RecordCrossmatchCommand,Outcome<CrossmatchView>> handler,
            CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))
                return Results.BadRequest(new{code="invalid_csrf"});
            return Respond(await handler.Handle(new(Actor(http),facilityId,input),ct));
        });
        group.MapPost("/crossmatches/{crossmatchId:long}/finalize",async(
            long facilityId,long crossmatchId,FinalizeCrossmatchRequest input,HttpContext http,
            IAntiforgery anti,
            IRequestHandler<FinalizeCrossmatchCommand,Outcome<CrossmatchView>> handler,
            CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))
                return Results.BadRequest(new{code="invalid_csrf"});
            return Respond(await handler.Handle(new(Actor(http),facilityId,crossmatchId,input),ct));
        });
    }

    private static long Actor(HttpContext http)=>long.Parse(
        http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static IResult Respond<T>(Outcome<T> value)=>value.IsSuccess
        ?Results.Ok(value.Value):Fail(value.Error!);
    private static IResult Fail(string code)=>code switch
    {
        "forbidden" or "independent_review_required"=>Results.Forbid(),
        "unit_not_found" or "reservation_not_found" or "not_found"=>Results.NotFound(),
        "invalid_unit" or "invalid_request" or "invalid_catalog" or
            "invalid_crossmatch" or "invalid_rowversion"=>Results.BadRequest(new{code}),
        "duplicate_unit" or "unit_unavailable" or "request_not_approved" or
            "product_mismatch" or "quantity_exceeded" or "already_reserved" or
            "crossmatch_exists" or "constraint_conflict" or "concurrency_conflict" or
            "invalid_state" or "missing_test_evidence"=>Results.Conflict(new{code}),
        _=>Results.Problem(statusCode:500,title:"blood_bank_error")
    };
}
