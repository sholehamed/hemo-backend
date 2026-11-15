using Hemo.Application.Abstractions;
using Hemo.Application.Cqrs;

namespace Hemo.Application.Patients;

/// <summary>
/// CQRS use cases: authorization and orchestration are independent of ASP.NET and EF.
/// No handler relies on client-provided roles or an unverified facility claim.
/// </summary>
public sealed class ListAdmissionsHandler(IAdmissionStore store, IResourcePermission access)
    : IRequestHandler<ListAdmissionsQuery, AdmissionOperation<AdmissionPage>>
{
    public async Task<AdmissionOperation<AdmissionPage>> Handle(
        ListAdmissionsQuery query, CancellationToken ct)
    {
        if (!await access.CanAsync(query.ActorId, "patient.read",
            query.FacilityId, query.DepartmentId, ct))
            return AdmissionOperation<AdmissionPage>.Fail("forbidden");
        return AdmissionOperation<AdmissionPage>.Ok(
            await store.ListAsync(query.FacilityId, query.DepartmentId,
                query.Search, query.Page, ct));
    }
}

public sealed class GetAdmissionHandler(IAdmissionStore store, IResourcePermission access)
    : IRequestHandler<GetAdmissionQuery, AdmissionOperation<AdmissionView>>
{
    public async Task<AdmissionOperation<AdmissionView>> Handle(
        GetAdmissionQuery query, CancellationToken ct)
    {
        if (!await access.CanAsync(query.ActorId, "patient.read",
            query.FacilityId, query.DepartmentId, ct))
            return AdmissionOperation<AdmissionView>.Fail("forbidden");
        var data = await store.GetAsync(query.FacilityId, query.DepartmentId,
            query.EncounterId, ct);
        return data is null
            ? AdmissionOperation<AdmissionView>.Fail("not_found")
            : AdmissionOperation<AdmissionView>.Ok(data);
    }
}

public sealed class CreateAdmissionHandler(IAdmissionStore store, IResourcePermission access)
    : IRequestHandler<CreateAdmissionCommand, AdmissionOperation<AdmissionView>>
{
    public async Task<AdmissionOperation<AdmissionView>> Handle(
        CreateAdmissionCommand command, CancellationToken ct)
    {
        if (!await access.CanAsync(command.ActorId, "patient.create",
            command.FacilityId, command.DepartmentId, ct))
            return AdmissionOperation<AdmissionView>.Fail("forbidden");
        return await store.CreateAsync(command.FacilityId, command.DepartmentId,
            command.ActorId, command.Input, ct);
    }
}

public sealed class UpdatePatientHandler(IAdmissionStore store, IResourcePermission access)
    : IRequestHandler<UpdatePatientCommand, AdmissionOperation<AdmissionView>>
{
    public async Task<AdmissionOperation<AdmissionView>> Handle(
        UpdatePatientCommand command, CancellationToken ct)
    {
        if (!await access.CanAsync(command.ActorId, "patient.update",
            command.FacilityId, command.DepartmentId, ct))
            return AdmissionOperation<AdmissionView>.Fail("forbidden");
        return await store.UpdatePatientAsync(command.FacilityId,
            command.DepartmentId, command.EncounterId, command.ActorId,
            command.Input, ct);
    }
}

public sealed class IssueWristbandHandler(IAdmissionStore store, IResourcePermission access)
    : IRequestHandler<IssueWristbandCommand, AdmissionOperation<WristbandView>>
{
    public async Task<AdmissionOperation<WristbandView>> Handle(
        IssueWristbandCommand command, CancellationToken ct)
    {
        if (!await access.CanAsync(command.ActorId, "patient.update",
            command.FacilityId, command.DepartmentId, ct))
            return AdmissionOperation<WristbandView>.Fail("forbidden");
        return await store.IssueWristbandAsync(command.FacilityId,
            command.DepartmentId, command.EncounterId, command.ActorId, ct);
    }
}

public sealed class VerifyIdentityHandler(IAdmissionStore store, IResourcePermission access)
    : IRequestHandler<VerifyIdentityCommand, AdmissionOperation<IdentityVerificationView>>
{
    public async Task<AdmissionOperation<IdentityVerificationView>> Handle(
        VerifyIdentityCommand command, CancellationToken ct)
    {
        if (!await access.CanAsync(command.ActorId, "patient.identity.verify",
            command.FacilityId, command.DepartmentId, ct))
            return AdmissionOperation<IdentityVerificationView>.Fail("forbidden");
        return await store.VerifyIdentityAsync(command.FacilityId,
            command.DepartmentId, command.EncounterId, command.ActorId,
            command.Input, ct);
    }
}
