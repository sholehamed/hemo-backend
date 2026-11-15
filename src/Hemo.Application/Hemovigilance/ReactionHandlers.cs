using Hemo.Application.Abstractions;
using Hemo.Application.Cqrs;

namespace Hemo.Application.Hemovigilance;

// Each clinical use case enforces permissions BEFORE using SQL ports.
public sealed class ListReactionsHandler(IReactionStore store,IResourcePermission permission)
    : IRequestHandler<ListReactionsQuery,Outcome<IReadOnlyList<ReactionView>>>
{
    public async Task<Outcome<IReadOnlyList<ReactionView>>> Handle(ListReactionsQuery q,CancellationToken ct)
    {
        if(!await permission.CanAsync(q.Actor,"transfusion.read",q.Facility,q.Department,ct))
            return Outcome<IReadOnlyList<ReactionView>>.Fail("forbidden");
        return Outcome<IReadOnlyList<ReactionView>>.Ok(await store.ListAsync(q.Facility,q.Department,ct));
    }
}

public sealed class GetReactionHandler(IReactionStore store,IResourcePermission permission)
    : IRequestHandler<GetReactionQuery,Outcome<ReactionDetails>>
{
    public async Task<Outcome<ReactionDetails>> Handle(GetReactionQuery q,CancellationToken ct)
    {
        if(!await permission.CanAsync(q.Actor,"transfusion.read",q.Facility,q.Department,ct))
            return Outcome<ReactionDetails>.Fail("forbidden");
        return await store.GetAsync(q.Facility,q.Department,q.Id,ct);
    }
}

public sealed class ReportReactionHandler(IReactionStore store,IResourcePermission permission)
    : IRequestHandler<ReportReactionCommand,Outcome<ReactionView>>
{
    public async Task<Outcome<ReactionView>> Handle(ReportReactionCommand q,CancellationToken ct)
    {
        if(!await permission.CanAsync(q.Actor,"reaction.report",q.Facility,q.Department,ct))
            return Outcome<ReactionView>.Fail("forbidden");
        return await store.ReportAsync(q.Actor,q.Facility,q.Department,q.Input,ct);
    }
}

public sealed class AssessReactionHandler(IReactionStore store,IResourcePermission permission)
    : IRequestHandler<AssessReactionCommand,Outcome<ReactionView>>
{
    public async Task<Outcome<ReactionView>> Handle(AssessReactionCommand q,CancellationToken ct)
    {
        if(!await permission.CanAsync(q.Actor,"reaction.diagnose",q.Facility,q.Department,ct))
            return Outcome<ReactionView>.Fail("forbidden");
        return await store.AssessAsync(q.Actor,q.Facility,q.Department,q.Id,q.Input,ct);
    }
}

public sealed class InvestigateReactionHandler(IReactionStore store,IResourcePermission permission)
    : IRequestHandler<InvestigateReactionCommand,Outcome<ReactionView>>
{
    public async Task<Outcome<ReactionView>> Handle(InvestigateReactionCommand q,CancellationToken ct)
    {
        if(!await permission.CanAsync(q.Actor,"rca.manage",q.Facility,null,ct))
            return Outcome<ReactionView>.Fail("forbidden");
        return await store.InvestigateAsync(q.Actor,q.Facility,q.Id,q.Input,ct);
    }
}

public sealed class ReviewReactionHandler(IReactionStore store,IResourcePermission permission)
    : IRequestHandler<ReviewReactionCommand,Outcome<ReactionView>>
{
    public async Task<Outcome<ReactionView>> Handle(ReviewReactionCommand q,CancellationToken ct)
    {
        if(!await permission.CanAsync(q.Actor,"rca.manage",q.Facility,null,ct))
            return Outcome<ReactionView>.Fail("forbidden");
        return await store.ReviewAsync(q.Actor,q.Facility,q.Id,q.Input,ct);
    }
}

public sealed class OpenRcaHandler(IReactionStore store,IResourcePermission permission)
    : IRequestHandler<OpenRcaCommand,Outcome<RcaView>>
{
    public async Task<Outcome<RcaView>> Handle(OpenRcaCommand q,CancellationToken ct)
    {
        if(!await permission.CanAsync(q.Actor,"rca.manage",q.Facility,null,ct))
            return Outcome<RcaView>.Fail("forbidden");
        return await store.OpenRcaAsync(q.Actor,q.Facility,q.EventId,ct);
    }
}

public sealed class AddRcaCauseHandler(IReactionStore store,IResourcePermission permission)
    : IRequestHandler<AddRcaCauseCommand,Outcome<RcaView>>
{
    public async Task<Outcome<RcaView>> Handle(AddRcaCauseCommand q,CancellationToken ct)
    {
        if(!await permission.CanAsync(q.Actor,"rca.manage",q.Facility,null,ct))
            return Outcome<RcaView>.Fail("forbidden");
        return await store.CauseAsync(q.Actor,q.Facility,q.RcaId,q.Input,ct);
    }
}

public sealed class AssignCorrectiveActionHandler(IReactionStore store,IResourcePermission permission)
    : IRequestHandler<AssignCorrectiveActionCommand,Outcome<CorrectiveActionView>>
{
    public async Task<Outcome<CorrectiveActionView>> Handle(AssignCorrectiveActionCommand q,CancellationToken ct)
    {
        if(!await permission.CanAsync(q.Actor,"rca.manage",q.Facility,null,ct))
            return Outcome<CorrectiveActionView>.Fail("forbidden");
        return await store.AssignAsync(q.Actor,q.Facility,q.RcaId,q.Input,ct);
    }
}

public sealed class CompleteCorrectiveActionHandler(IReactionStore store,IResourcePermission permission)
    : IRequestHandler<CompleteCorrectiveActionCommand,Outcome<CorrectiveActionView>>
{
    public async Task<Outcome<CorrectiveActionView>> Handle(CompleteCorrectiveActionCommand q,CancellationToken ct)
    {
        if(!await permission.CanAsync(q.Actor,"rca.manage",q.Facility,null,ct))
            return Outcome<CorrectiveActionView>.Fail("forbidden");
        return await store.CompleteActionAsync(q.Actor,q.Facility,q.ActionId,q.Input,ct);
    }
}

public sealed class VerifyCorrectiveActionHandler(IReactionStore store,IResourcePermission permission)
    : IRequestHandler<VerifyCorrectiveActionCommand,Outcome<CorrectiveActionView>>
{
    public async Task<Outcome<CorrectiveActionView>> Handle(VerifyCorrectiveActionCommand q,CancellationToken ct)
    {
        if(!await permission.CanAsync(q.Actor,"rca.manage",q.Facility,null,ct))
            return Outcome<CorrectiveActionView>.Fail("forbidden");
        return await store.VerifyActionAsync(q.Actor,q.Facility,q.ActionId,q.Input,ct);
    }
}

public sealed class CloseRcaHandler(IReactionStore store,IResourcePermission permission)
    : IRequestHandler<CloseRcaCommand,Outcome<RcaView>>
{
    public async Task<Outcome<RcaView>> Handle(CloseRcaCommand q,CancellationToken ct)
    {
        if(!await permission.CanAsync(q.Actor,"rca.manage",q.Facility,null,ct))
            return Outcome<RcaView>.Fail("forbidden");
        return await store.CloseRcaAsync(q.Actor,q.Facility,q.RcaId,q.Input,ct);
    }
}

