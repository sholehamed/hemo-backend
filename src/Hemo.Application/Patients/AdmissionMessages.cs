using Hemo.Application.Cqrs;

namespace Hemo.Application.Patients;

public sealed record ListAdmissionsQuery(
    long ActorId, long FacilityId, long DepartmentId, string? Search, int Page)
    : IQuery<AdmissionOperation<AdmissionPage>>;

public sealed record GetAdmissionQuery(
    long ActorId, long FacilityId, long DepartmentId, long EncounterId)
    : IQuery<AdmissionOperation<AdmissionView>>;

public sealed record CreateAdmissionCommand(
    long ActorId, long FacilityId, long DepartmentId, CreateAdmissionRequest Input)
    : ICommand<AdmissionOperation<AdmissionView>>;

public sealed record UpdatePatientCommand(
    long ActorId, long FacilityId, long DepartmentId, long EncounterId, UpdatePatientRequest Input)
    : ICommand<AdmissionOperation<AdmissionView>>;

public sealed record IssueWristbandCommand(
    long ActorId, long FacilityId, long DepartmentId, long EncounterId)
    : ICommand<AdmissionOperation<WristbandView>>;

public sealed record VerifyIdentityCommand(
    long ActorId, long FacilityId, long DepartmentId, long EncounterId,
    IdentityVerificationRequest Input)
    : ICommand<AdmissionOperation<IdentityVerificationView>>;
