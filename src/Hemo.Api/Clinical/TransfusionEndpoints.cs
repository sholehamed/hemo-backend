using System.Security.Claims;
using Hemo.Api.Security;
using Hemo.Application.Clinical;
using Hemo.Application.Cqrs;
using Microsoft.AspNetCore.Antiforgery;

namespace Hemo.Api.Clinical;

public static class TransfusionEndpoints
{
    public static void MapTransfusionEndpoints(this WebApplication app)
    {
        var group=app.MapGroup("/api/v1/facilities/{facilityId:long}/departments/{departmentId:long}/transfusions")
            .RequireAuthorization().WithTags("Transfusion");

        group.MapGet("/",async(long facilityId,long departmentId,HttpContext http,
            IRequestHandler<ListTransfusionsQuery,Outcome<IReadOnlyList<TransfusionView>>> handler,
            CancellationToken ct)=>Reply(await handler.Handle(new(Actor(http),facilityId,departmentId),ct)));

        group.MapGet("/{id:long}",async(long facilityId,long departmentId,long id,HttpContext http,
            IRequestHandler<GetTransfusionQuery,Outcome<TransfusionDetails>> handler,
            CancellationToken ct)=>Reply(await handler.Handle(new(Actor(http),facilityId,departmentId,id),ct)));

        group.MapPost("/",async(long facilityId,long departmentId,PrepareTransfusionRequest input,
            HttpContext http,IAntiforgery anti,
            IRequestHandler<PrepareTransfusionCommand,Outcome<TransfusionView>> handler,
            CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,departmentId,input),ct));
        });
        group.MapPost("/{id:long}/checklist",async(long facilityId,long departmentId,long id,
            ChecklistRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<RecordChecklistCommand,Outcome<TransfusionView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,departmentId,id,input),ct));
        });
        group.MapPost("/{id:long}/vitals",async(long facilityId,long departmentId,long id,
            VitalRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<RecordVitalCommand,Outcome<VitalView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,departmentId,id,input),ct));
        });
        group.MapPost("/{id:long}/start",async(long facilityId,long departmentId,long id,
            TransitionRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<StartTransfusionCommand,Outcome<TransfusionView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,departmentId,id,input),ct));
        });
        group.MapPost("/{id:long}/stop",async(long facilityId,long departmentId,long id,
            TransitionRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<StopTransfusionCommand,Outcome<TransfusionView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,departmentId,id,input),ct));
        });
        group.MapPost("/{id:long}/complete",async(long facilityId,long departmentId,long id,
            TransitionRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<CompleteTransfusionCommand,Outcome<TransfusionView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,departmentId,id,input),ct));
        });
        group.MapPost("/{id:long}/signoff",async(long facilityId,long departmentId,long id,
            SignoffRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<SignoffTransfusionCommand,Outcome<TransfusionView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,departmentId,id,input),ct));
        });
    }
    private static long Actor(HttpContext http)=>long.Parse(
        http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static IResult Reply<T>(Outcome<T> result)=>result.IsSuccess
        ? Results.Ok(result.Value) : Error(result.Error!);
    private static IResult Error(string code)=>code switch
    {
        "forbidden" or "independent_signoff_required"=>Results.Forbid(),
        "not_found"=>Results.NotFound(),
        "invalid_input" or "invalid_vitals" or "checklist_incomplete" or
        "invalid_transition" or "invalid_signoff"=>Results.BadRequest(new{code}),
        "unit_not_received" or "transfusion_exists" or "identity_not_verified" or
        "crossmatch_not_final" or "constraint_conflict" or "precheck_required" or
        "baseline_required" or "baseline_exists" or "monitoring_required" or
        "invalid_state" or "signed_record_locked" or "concurrency_conflict" =>
            Results.Conflict(new{code}),
        _=>Results.Problem(statusCode:500,title:"transfusion_error")
    };
}
