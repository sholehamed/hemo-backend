using Hemo.Application.Abstractions;
using Hemo.Application.Cqrs;

namespace Hemo.Application.Blood;

public sealed class ListIssuesHandler(ILogisticsStore store,IResourcePermission access)
    : IRequestHandler<ListIssuesQuery,Outcome<IReadOnlyList<IssueView>>>
{
    public async Task<Outcome<IReadOnlyList<IssueView>>> Handle(ListIssuesQuery q,CancellationToken ct) =>
        !await access.CanAsync(q.Actor,"issue.receive",q.FacilityId,q.DepartmentId,ct)
        && !await access.CanAsync(q.Actor,"issue.create",q.FacilityId,null,ct)
        ? Outcome<IReadOnlyList<IssueView>>.Fail("forbidden")
        : Outcome<IReadOnlyList<IssueView>>.Ok(await store.ListAsync(q.FacilityId,q.DepartmentId,ct));
}
public sealed class IssueUnitHandler(ILogisticsStore store,IResourcePermission access)
    : IRequestHandler<IssueUnitCommand,Outcome<IssueView>>
{
    public async Task<Outcome<IssueView>> Handle(IssueUnitCommand c,CancellationToken ct) =>
        !await access.CanAsync(c.Actor,"issue.create",c.FacilityId,null,ct)
        ? Outcome<IssueView>.Fail("forbidden")
        : await store.IssueAsync(c.Actor,c.FacilityId,c.Input,ct);
}
public sealed class DispatchIssueHandler(ILogisticsStore store,IResourcePermission access)
    : IRequestHandler<DispatchIssueCommand,Outcome<DispatchView>>
{
    public async Task<Outcome<DispatchView>> Handle(DispatchIssueCommand c,CancellationToken ct) =>
        !await access.CanAsync(c.Actor,"dispatch.manage",c.FacilityId,null,ct)
        ? Outcome<DispatchView>.Fail("forbidden")
        : await store.DispatchAsync(c.Actor,c.FacilityId,c.Input,ct);
}
public sealed class ReceiveIssueHandler(ILogisticsStore store,IResourcePermission access)
    : IRequestHandler<ReceiveIssueCommand,Outcome<IssueView>>
{
    public async Task<Outcome<IssueView>> Handle(ReceiveIssueCommand c,CancellationToken ct) =>
        !await access.CanAsync(c.Actor,"issue.receive",c.FacilityId,c.DepartmentId,ct)
        ? Outcome<IssueView>.Fail("forbidden")
        : await store.ReceiveAsync(c.Actor,c.FacilityId,c.DepartmentId,c.IssueId,c.Input,ct);
}
public sealed class ReturnIssueHandler(ILogisticsStore store,IResourcePermission access)
    : IRequestHandler<ReturnIssueCommand,Outcome<ReturnView>>
{
    public async Task<Outcome<ReturnView>> Handle(ReturnIssueCommand c,CancellationToken ct) =>
        !await access.CanAsync(c.Actor,"return.manage",c.FacilityId,null,ct)
        ? Outcome<ReturnView>.Fail("forbidden")
        : await store.ReturnAsync(c.Actor,c.FacilityId,c.DepartmentId,c.IssueId,c.Input,ct);
}
public sealed class ReviewReturnHandler(ILogisticsStore store,IResourcePermission access)
    : IRequestHandler<ReviewReturnCommand,Outcome<ReturnView>>
{
    public async Task<Outcome<ReturnView>> Handle(ReviewReturnCommand c,CancellationToken ct) =>
        !await access.CanAsync(c.Actor,"return.manage",c.FacilityId,null,ct)
        ? Outcome<ReturnView>.Fail("forbidden")
        : await store.ReviewReturnAsync(c.Actor,c.FacilityId,c.ReturnId,c.Input,ct);
}

public sealed class ListReturnsHandler(ILogisticsStore store,IResourcePermission access)
    : IRequestHandler<ListReturnsQuery,Outcome<IReadOnlyList<ReturnView>>>
{
    public async Task<Outcome<IReadOnlyList<ReturnView>>> Handle(ListReturnsQuery q,CancellationToken ct) =>
        !await access.CanAsync(q.Actor,"return.manage",q.FacilityId,null,ct)
        ? Outcome<IReadOnlyList<ReturnView>>.Fail("forbidden")
        : Outcome<IReadOnlyList<ReturnView>>.Ok(await store.ReturnsAsync(q.FacilityId,ct));
}
