using System.Security.Claims;
using Hemo.Api.Security;
using Hemo.Application.Cqrs;
using Hemo.Application.Hemovigilance;
using Microsoft.AspNetCore.Antiforgery;

namespace Hemo.Api.Hemovigilance;

/// <summary>
/// HTTP BFF route adapters only. Workflow decisions are CQRS; SQL verifies
/// the linked clinical record and current facility/department for every action.
/// </summary>
public static class ReactionEndpoints
{
    public static void MapReactionEndpoints(this WebApplication app)
    {
        var ward=app.MapGroup(
            "/api/v1/facilities/{facilityId:long}/departments/{departmentId:long}/reactions")
            .RequireAuthorization().WithTags("Adverse Event Reporting");
        ward.MapGet("/",async(long facilityId,long departmentId,HttpContext http,
            IRequestHandler<ListReactionsQuery,Outcome<IReadOnlyList<ReactionView>>> handler,
            CancellationToken ct)=>Reply(await handler.Handle(new(Actor(http),facilityId,departmentId),ct)));
        ward.MapGet("/{id:long}",async(long facilityId,long departmentId,long id,HttpContext http,
            IRequestHandler<GetReactionQuery,Outcome<ReactionDetails>> handler,CancellationToken ct)=>
            Reply(await handler.Handle(new(Actor(http),facilityId,departmentId,id),ct)));

        ward.MapPost("/",async(long facilityId,long departmentId,
            ReportReactionRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<ReportReactionCommand,Outcome<ReactionView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,departmentId,input),ct));
        });
        ward.MapPost("/{id:long}/assess",async(long facilityId,long departmentId,long id,
            PhysicianAssessmentRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<AssessReactionCommand,Outcome<ReactionView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,departmentId,id,input),ct));
        });

        var hv=app.MapGroup("/api/v1/facilities/{facilityId:long}/hemovigilance")
            .RequireAuthorization().WithTags("Hemovigilance");
        hv.MapPost("/reactions/{id:long}/investigations",async(long facilityId,long id,
            InvestigationRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<InvestigateReactionCommand,Outcome<ReactionView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,id,input),ct));
        });
        hv.MapPost("/reactions/{id:long}/review",async(long facilityId,long id,
            ReviewReactionRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<ReviewReactionCommand,Outcome<ReactionView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,id,input),ct));
        });
        hv.MapGet("/rca/{rcaId:long}",async(long facilityId,long rcaId,
            HttpContext http,
            IRequestHandler<GetRcaQuery,Outcome<RcaDetails>> handler,CancellationToken ct)=>
            Reply(await handler.Handle(new(Actor(http),facilityId,rcaId),ct)));
        hv.MapPost("/reactions/{eventId:long}/rca",async(long facilityId,long eventId,
            HttpContext http,IAntiforgery anti,
            IRequestHandler<OpenRcaCommand,Outcome<RcaView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,eventId),ct));
        });
        hv.MapPost("/rca/{rcaId:long}/causes",async(long facilityId,long rcaId,
            RcaCauseRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<AddRcaCauseCommand,Outcome<RcaView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,rcaId,input),ct));
        });
        hv.MapPost("/rca/{rcaId:long}/actions",async(long facilityId,long rcaId,
            CorrectiveActionRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<AssignCorrectiveActionCommand,Outcome<CorrectiveActionView>> handler,
            CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,rcaId,input),ct));
        });
        hv.MapPost("/actions/{actionId:long}/complete",async(long facilityId,long actionId,
            ActionStateRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<CompleteCorrectiveActionCommand,Outcome<CorrectiveActionView>> handler,
            CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,actionId,input),ct));
        });
        hv.MapPost("/actions/{actionId:long}/verify",async(long facilityId,long actionId,
            ActionStateRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<VerifyCorrectiveActionCommand,Outcome<CorrectiveActionView>> handler,
            CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,actionId,input),ct));
        });
        hv.MapPost("/rca/{rcaId:long}/close",async(long facilityId,long rcaId,
            CloseRcaRequest input,HttpContext http,IAntiforgery anti,
            IRequestHandler<CloseRcaCommand,Outcome<RcaView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,rcaId,input),ct));
        });
    }

    private static long Actor(HttpContext http)=>long.Parse(
        http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static IResult Reply<T>(Outcome<T> result)=>result.IsSuccess
        ? Results.Ok(result.Value):Error(result.Error!);
    private static IResult Error(string code)=>code switch
    {
        "forbidden" or "assigned_actor_required" or "independent_verification_required" or
        "independent_closure_required"=>Results.Forbid(),
        "not_found"=>Results.NotFound(),
        "invalid_report" or "invalid_assessment" or "invalid_investigation" or
        "invalid_review" or "invalid_cause" or "invalid_action" or "invalid_closure" or
        "invalid_assignee"=>Results.BadRequest(new{code}),
        "stop_before_report" or "invalid_state" or "concurrency_conflict" or
        "rca_exists" or "rca_not_closed" or "root_cause_required" or
        "actions_not_verified" or "investigation_required" or
        "constraint_conflict"=>Results.Conflict(new{code}),
        _=>Results.Problem(statusCode:500,title:"hemovigilance_error")
    };
}
