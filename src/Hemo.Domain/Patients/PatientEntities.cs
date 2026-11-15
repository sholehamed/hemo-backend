namespace Hemo.Domain.Patients;

public sealed class Patient
{
    public long PatientId { get; set; }
    public string? NationalId { get; set; }
    public string? UnknownIdentityCode { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? FatherName { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string Sex { get; set; } = "U";
    public string? Mobile { get; set; }
    public bool IsUnknown { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class PatientIdentifier
{
    public long PatientIdentifierId { get; set; }
    public long PatientId { get; set; }
    public long? FacilityId { get; set; }
    public string IdentifierType { get; set; } = "";
    public string IdentifierValue { get; set; } = "";
    public string? SourceSystem { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class Encounter
{
    public long EncounterId { get; set; }
    public long PatientId { get; set; }
    public long FacilityId { get; set; }
    public string? HisEncounterNo { get; set; }
    public string AdmissionType { get; set; } = "Routine";
    public DateTime AdmittedAt { get; set; }
    public DateTime? DischargedAt { get; set; }
    public long? AttendingPhysicianId { get; set; }
    public string Status { get; set; } = "Admitted";
    public string? AdmissionNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class EncounterLocation
{
    public long EncounterLocationId { get; set; }
    public long EncounterId { get; set; }
    public long DepartmentId { get; set; }
    public long? BedId { get; set; }
    public DateTime FromAt { get; set; }
    public DateTime? ToAt { get; set; }
    public long? RecordedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class Wristband
{
    public long WristbandId { get; set; }
    public long EncounterId { get; set; }
    public string Barcode { get; set; } = "";
    public DateTime PrintedAt { get; set; }
    public long PrintedByUserId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class IdentityVerification
{
    public long IdentityVerificationId { get; set; }
    public long EncounterId { get; set; }
    public long? WristbandId { get; set; }
    public long VerifiedByUserId { get; set; }
    public DateTime VerifiedAt { get; set; }
    public string FirstIdentifier { get; set; } = "";
    public string SecondIdentifier { get; set; } = "";
    public bool WristbandMatched { get; set; }
    public string Status { get; set; } = "";
    public string? MismatchReason { get; set; }
    public string? ChecklistJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
