namespace Hemo.Application.Patients;

/// <summary>
/// Persistence port. The application layer knows nothing about DbContext, SQL or HTTP.
/// </summary>
public interface IAdmissionStore
{
    Task<AdmissionPage> ListAsync(long facilityId, long departmentId,
        string? search, int page, CancellationToken ct);
    Task<AdmissionView?> GetAsync(long facilityId, long departmentId,
        long encounterId, CancellationToken ct);
    Task<AdmissionOperation<AdmissionView>> CreateAsync(
        long facilityId, long departmentId, long actor, CreateAdmissionRequest input,
        CancellationToken ct);
    Task<AdmissionOperation<AdmissionView>> UpdatePatientAsync(
        long facilityId, long departmentId, long encounterId, long actor,
        UpdatePatientRequest input, CancellationToken ct);
    Task<AdmissionOperation<WristbandView>> IssueWristbandAsync(
        long facilityId, long departmentId, long encounterId, long actor, CancellationToken ct);
    Task<AdmissionOperation<IdentityVerificationView>> VerifyIdentityAsync(
        long facilityId, long departmentId, long encounterId, long actor,
        IdentityVerificationRequest input, CancellationToken ct);
}
