namespace Hemo.Application.Patients;

public sealed record CreateAdmissionRequest(
    string? NationalId, bool IsUnknown, string? FirstName, string? LastName,
    string? FatherName, DateOnly? BirthDate, string Sex, string? Mobile,
    string AdmissionType, long? BedId, string? AdmissionNote);

public sealed record UpdatePatientRequest(
    string FirstName, string LastName, string? FatherName,
    DateOnly? BirthDate, string Sex, string? Mobile, string RowVersion);

public sealed record AdmissionView(
    long EncounterId, long PatientId, long FacilityId, long DepartmentId,
    long? BedId, string FirstName, string LastName, string? NationalId,
    string? UnknownIdentityCode, bool IsUnknown, DateOnly? BirthDate,
    string Sex, string AdmissionType, string Status, DateTime AdmittedAt,
    string PatientRowVersion, string? ActiveBarcode);

public sealed record AdmissionPage(IReadOnlyList<AdmissionView> Items, int Page,
    int PageSize, int Total);

public sealed record WristbandView(long WristbandId, string Barcode, DateTime IssuedAt,
    string Status);

public sealed record IdentityVerificationRequest(string Barcode,
    string FirstIdentifier, string SecondIdentifier, bool ChecklistConfirmed,
    string? MismatchReason);

public sealed record IdentityVerificationView(long Id, long EncounterId,
    string Status, bool WristbandMatched, DateTime VerifiedAt);

public sealed record AdmissionOperation<T>(T? Value, string? Error)
{
    public bool IsSuccess => Error is null;
    public static AdmissionOperation<T> Ok(T value) => new(value, null);
    public static AdmissionOperation<T> Fail(string error) => new(default, error);
}
