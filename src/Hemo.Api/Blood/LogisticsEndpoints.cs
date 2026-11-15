using System.Security.Claims;
using Hemo.Api.Security;
using Hemo.Application.Blood;
using Hemo.Application.Cqrs;
using Microsoft.AspNetCore.Antiforgery;

namespace Hemo.Api.Blood;

/// <summary>Minimally mapped bank and ward handoffs; no clinical decisions at transport boundary.</summary>
public static class LogisticsEndpoints
{
    public static void MapLogisticsEndpoints(this WebApplication app)
    {
        var bank=app.MapGroup("/api/v1/facilities/{facilityId:long}/logistics")
            .RequireAuthorization().WithTags("Blood Logistics");

        bank.MapPost("/issues",async(long facilityId,IssueUnitRequest input,HttpContext http,
            IAntiforgery anti,IRequestHandler<IssueUnitCommand,Outcome<IssueView>> handler,
            CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))
                return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,input),ct));
        });
        bank.MapPost("/dispatches",async(long facilityId,CreateDispatchRequest input,
            HttpContext http,IAntiforgery anti,
            IRequestHandler<DispatchIssueCommand,Outcome<DispatchView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))
                return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,input),ct));
        });
        bank.MapPost("/returns/{returnId:long}/review",async(
            long facilityId,long returnId,ReviewReturnRequest input,HttpContext http,
            IAntiforgery anti,
            IRequestHandler<ReviewReturnCommand,Outcome<ReturnView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))
                return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,returnId,input),ct));
        });

        var ward=app.MapGroup(
            "/api/v1/facilities/{facilityId:long}/departments/{departmentId:long}/logistics")
            .RequireAuthorization().WithTags("Ward Handoffs");
        ward.MapGet("/issues",async(long facilityId,long departmentId,HttpContext http,
            IRequestHandler<ListIssuesQuery,Outcome<IReadOnlyList<IssueView>>> handler,
            CancellationToken ct)=>Reply(await handler.Handle(new(
                Actor(http),facilityId,departmentId),ct)));
        ward.MapPost("/issues/{issueId:long}/receive",async(
            long facilityId,long departmentId,long issueId,ReceiveIssueRequest input,
            HttpContext http,IAntiforgery anti,
            IRequestHandler<ReceiveIssueCommand,Outcome<IssueView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))
                return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,departmentId,
                issueId,input),ct));
        });
        ward.MapPost("/issues/{issueId:long}/return",async(
            long facilityId,long departmentId,long issueId,ReturnIssueRequest input,
            HttpContext http,IAntiforgery anti,
            IRequestHandler<ReturnIssueCommand,Outcome<ReturnView>> handler,CancellationToken ct)=>{
            if(!await MutationCsrf.ValidAsync(http,anti))
                return Results.BadRequest(new{code="invalid_csrf"});
            return Reply(await handler.Handle(new(Actor(http),facilityId,departmentId,
                issueId,input),ct));
        });
    }
    private static long Actor(HttpContext http)=>
        long.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static IResult Reply<T>(Outcome<T> result)=>result.IsSuccess
        ?Results.Ok(result.Value):Error(result.Error!);
    private static IResult Error(string code)=>code switch
    {
        "forbidden" or "independent_review_required"=>Results.Forbid(),
        "not_found" or "unit_not_found"=>Results.NotFound(),
        "invalid_issue" or "invalid_dispatch" or "invalid_receipt" or
        "invalid_return" or "invalid_rowversion" or "invalid_review" =>
            Results.BadRequest(new{code}),
        "request_not_approved" or "crossmatch_not_approved" or "missing_reservation" or
        "unit_unavailable" or "already_issued" or "already_dispatched" or
        "already_returned" or "concurrency_conflict" or "invalid_state" or
        "constraint_conflict"=>Results.Conflict(new{code}),
        _=>Results.Problem(statusCode:500,title:"logistics_error")
    };
}
