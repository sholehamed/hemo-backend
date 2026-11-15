using Hemo.Application.Cqrs;

namespace Hemo.Application.Clinical;

public sealed record NewBloodRequest(
    long EncounterId, long ProductTypeId, decimal Quantity, string Priority,
    string Indication, string? Diagnosis, string? SpecialRequirements,
    decimal? Hemoglobin, bool? PreviousReaction);
public sealed record ReviewBloodRequest(string Decision, string? Reason, string RowVersion);
public sealed record ProductTypeOption(long Id, string Code, string Name);
public sealed record BloodRequestView(
    long BloodRequestId, string RequestNo, long EncounterId, long FacilityId,
    long DepartmentId, long ProductTypeId, string ProductCode, decimal Quantity,
    string Priority, string Status, string Indication, string? Diagnosis,
    long RequestedByUserId, DateTime RequestedAt, string RowVersion);
public sealed record BloodRequestPage(IReadOnlyList<BloodRequestView> Items, int Page,
    int PageSize, int Total);

public sealed record ListProductTypesQuery(long ActorId, long FacilityId, long DepartmentId)
    : IQuery<Outcome<IReadOnlyList<ProductTypeOption>>>;
public sealed record ListBloodRequestsQuery(long ActorId, long FacilityId, long DepartmentId,
    int Page, string? Status) : IQuery<Outcome<BloodRequestPage>>;
public sealed record GetBloodRequestQuery(long ActorId, long FacilityId, long DepartmentId,
    long RequestId) : IQuery<Outcome<BloodRequestView>>;
public sealed record SubmitBloodRequestCommand(long ActorId, long FacilityId, long DepartmentId,
    NewBloodRequest Input) : ICommand<Outcome<BloodRequestView>>;
public sealed record ReviewBloodRequestCommand(long ActorId, long FacilityId, long DepartmentId,
    long RequestId, ReviewBloodRequest Input) : ICommand<Outcome<BloodRequestView>>;

public interface IBloodRequestStore
{
    Task<IReadOnlyList<ProductTypeOption>> ProductTypesAsync(CancellationToken ct);
    Task<BloodRequestPage> ListAsync(long facilityId, long departmentId,
        int page, string? status, CancellationToken ct);
    Task<BloodRequestView?> GetAsync(long facilityId, long departmentId,
        long requestId, CancellationToken ct);
    Task<Outcome<BloodRequestView>> SubmitAsync(long actorId, long facilityId,
        long departmentId, NewBloodRequest input, CancellationToken ct);
    Task<Outcome<BloodRequestView>> ReviewAsync(long actorId, long facilityId,
        long departmentId, long requestId, ReviewBloodRequest input, CancellationToken ct);
}
