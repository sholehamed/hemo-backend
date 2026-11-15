using Hemo.Application.Cqrs;

namespace Hemo.Application.Blood;

public sealed record IssueUnitRequest(long UnitId,long RequestId,long DepartmentId,
    string? ConditionNote);
public sealed record CreateDispatchRequest(long IssueId,string ContainerType,
    decimal? TemperatureAtExitC,string? Note);
public sealed record ReceiveIssueRequest(string RowVersion,string? ConditionNote);
public sealed record ReturnIssueRequest(long ReturnedByUserId,string ReasonCode,string? ConditionCode,
    decimal? TemperatureC,string? Notes);
public sealed record ReviewReturnRequest(string Decision,string RowVersion,string? Note);
public sealed record IssueView(long Id,long UnitId,string UnitCode,long RequestId,
    long DepartmentId,string Status,DateTime IssuedAt,string RowVersion);
public sealed record DispatchView(long Id,long IssueId,string Status,string DispatchNo);
public sealed record ReturnView(long Id,long IssueId,string Status,string? Decision,string RowVersion);
public sealed record ListReturnsQuery(long Actor,long FacilityId)
    : IQuery<Outcome<IReadOnlyList<ReturnView>>>;
public sealed record ListIssuesQuery(long Actor,long FacilityId,long DepartmentId)
    : IQuery<Outcome<IReadOnlyList<IssueView>>>;
public sealed record IssueUnitCommand(long Actor,long FacilityId,IssueUnitRequest Input)
    : ICommand<Outcome<IssueView>>;
public sealed record DispatchIssueCommand(long Actor,long FacilityId,CreateDispatchRequest Input)
    : ICommand<Outcome<DispatchView>>;
public sealed record ReceiveIssueCommand(long Actor,long FacilityId,long DepartmentId,
    long IssueId,ReceiveIssueRequest Input) : ICommand<Outcome<IssueView>>;
public sealed record ReturnIssueCommand(long Actor,long FacilityId,long DepartmentId,long IssueId,
    ReturnIssueRequest Input) : ICommand<Outcome<ReturnView>>;
public sealed record ReviewReturnCommand(long Actor,long FacilityId,long ReturnId,
    ReviewReturnRequest Input) : ICommand<Outcome<ReturnView>>;

public interface ILogisticsStore
{
    Task<IReadOnlyList<ReturnView>> ReturnsAsync(long facility,CancellationToken ct);
    Task<IReadOnlyList<IssueView>> ListAsync(long facility,long department,CancellationToken ct);
    Task<Outcome<IssueView>> IssueAsync(long actor,long facility,IssueUnitRequest input,CancellationToken ct);
    Task<Outcome<DispatchView>> DispatchAsync(long actor,long facility,CreateDispatchRequest input,CancellationToken ct);
    Task<Outcome<IssueView>> ReceiveAsync(long actor,long facility,long department,long issue,
        ReceiveIssueRequest input,CancellationToken ct);
    Task<Outcome<ReturnView>> ReturnAsync(long actor,long facility,long department,long issue,
        ReturnIssueRequest input,CancellationToken ct);
    Task<Outcome<ReturnView>> ReviewReturnAsync(long actor,long facility,long returnId,
        ReviewReturnRequest input,CancellationToken ct);
}
