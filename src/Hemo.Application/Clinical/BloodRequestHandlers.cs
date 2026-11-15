using Hemo.Application.Abstractions;
using Hemo.Application.Cqrs;

namespace Hemo.Application.Clinical;

/// <summary>Clinical workflow access is enforced before database records are queried.</summary>
public sealed class ListProductTypesHandler(IBloodRequestStore store, IResourcePermission permission)
    : IRequestHandler<ListProductTypesQuery, Outcome<IReadOnlyList<ProductTypeOption>>>
{
    public async Task<Outcome<IReadOnlyList<ProductTypeOption>>> Handle(
        ListProductTypesQuery query, CancellationToken ct)
    {
        if (!await permission.CanAsync(query.ActorId, "request.read",
            query.FacilityId, query.DepartmentId, ct))
            return Outcome<IReadOnlyList<ProductTypeOption>>.Fail("forbidden");
        return Outcome<IReadOnlyList<ProductTypeOption>>.Ok(await store.ProductTypesAsync(ct));
    }
}
public sealed class ListBloodRequestsHandler(IBloodRequestStore store, IResourcePermission permission)
    : IRequestHandler<ListBloodRequestsQuery, Outcome<BloodRequestPage>>
{
    public async Task<Outcome<BloodRequestPage>> Handle(ListBloodRequestsQuery query, CancellationToken ct)
    {
        if (!await permission.CanAsync(query.ActorId, "request.read",
            query.FacilityId, query.DepartmentId, ct))
            return Outcome<BloodRequestPage>.Fail("forbidden");
        return Outcome<BloodRequestPage>.Ok(await store.ListAsync(query.FacilityId,
            query.DepartmentId, query.Page, query.Status, ct));
    }
}
public sealed class GetBloodRequestHandler(IBloodRequestStore store, IResourcePermission permission)
    : IRequestHandler<GetBloodRequestQuery, Outcome<BloodRequestView>>
{
    public async Task<Outcome<BloodRequestView>> Handle(GetBloodRequestQuery query, CancellationToken ct)
    {
        if (!await permission.CanAsync(query.ActorId, "request.read",
            query.FacilityId, query.DepartmentId, ct))
            return Outcome<BloodRequestView>.Fail("forbidden");
        var found = await store.GetAsync(query.FacilityId, query.DepartmentId,
            query.RequestId, ct);
        return found is null ? Outcome<BloodRequestView>.Fail("not_found")
            : Outcome<BloodRequestView>.Ok(found);
    }
}
public sealed class SubmitBloodRequestHandler(IBloodRequestStore store, IResourcePermission permission)
    : IRequestHandler<SubmitBloodRequestCommand, Outcome<BloodRequestView>>
{
    public async Task<Outcome<BloodRequestView>> Handle(SubmitBloodRequestCommand command, CancellationToken ct)
    {
        if (!await permission.CanAsync(command.ActorId, "request.create",
            command.FacilityId, command.DepartmentId, ct))
            return Outcome<BloodRequestView>.Fail("forbidden");
        return await store.SubmitAsync(command.ActorId, command.FacilityId,
            command.DepartmentId, command.Input, ct);
    }
}
public sealed class ReviewBloodRequestHandler(IBloodRequestStore store, IResourcePermission permission)
    : IRequestHandler<ReviewBloodRequestCommand, Outcome<BloodRequestView>>
{
    public async Task<Outcome<BloodRequestView>> Handle(ReviewBloodRequestCommand command, CancellationToken ct)
    {
        if (!await permission.CanAsync(command.ActorId, "request.approve",
            command.FacilityId, command.DepartmentId, ct))
            return Outcome<BloodRequestView>.Fail("forbidden");
        return await store.ReviewAsync(command.ActorId, command.FacilityId,
            command.DepartmentId, command.RequestId, command.Input, ct);
    }
}
