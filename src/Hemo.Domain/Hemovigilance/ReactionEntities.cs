namespace Hemo.Domain.Hemovigilance;

public sealed class AdverseEvent
{
    public long AdverseEventId { get; set; }
    public long TransfusionId { get; set; }
    public string EventNo { get; set; } = "";
    public DateTime DetectedAt { get; set; }
    public DateTime ReportedAt { get; set; }
    public long ReportedByUserId { get; set; }
    public byte? SeverityGrade { get; set; }
    public string Status { get; set; } = "Reported";
    public bool TransfusionStopped { get; set; }
    public string? SymptomsSummary { get; set; }
    public string? ImmediateActions { get; set; }
    public string? Outcome { get; set; }
    public string? Narrative { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class EventSymptom
{
    public long EventSymptomId { get; set; }
    public long AdverseEventId { get; set; }
    public string SymptomCode { get; set; } = "";
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class PhysicianEventAssessment
{
    public long PhysicianEventAssessmentId { get; set; }
    public long AdverseEventId { get; set; }
    public long AssessedByUserId { get; set; }
    public DateTime AssessedAt { get; set; }
    public string? DiagnosisCode { get; set; }
    public string DiagnosisText { get; set; } = "";
    public byte? SeverityGrade { get; set; }
    public string? Causality { get; set; }
    public string? IsbtCode { get; set; }
    public string? Interventions { get; set; }
    public string? Recommendations { get; set; }
    public string? Outcome { get; set; }
    public bool NationalReportRequired { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class EventInvestigation
{
    public long EventInvestigationId { get; set; }
    public long AdverseEventId { get; set; }
    public string InvestigationType { get; set; } = "";
    public DateTime InvestigationAt { get; set; }
    public long PerformedByUserId { get; set; }
    public string? ResultText { get; set; }
    public string? ResultJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class RcaCase
{
    public long RcaCaseId { get; set; }
    public long? AdverseEventId { get; set; }
    public long? EquipmentIncidentId { get; set; }
    public string CaseNo { get; set; } = "";
    public DateTime OpenedAt { get; set; }
    public long OpenedByUserId { get; set; }
    public string Status { get; set; } = "Open";
    public string? RootCauseSummary { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class RcaCause
{
    public long RcaCauseId { get; set; }
    public long RcaCaseId { get; set; }
    public string CauseType { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsRootCause { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class CorrectiveAction
{
    public long CorrectiveActionId { get; set; }
    public long? RcaCaseId { get; set; }
    public long? EquipmentIncidentId { get; set; }
    public long? AuditId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public long AssignedToUserId { get; set; }
    public DateTime? DueAt { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime? CompletedAt { get; set; }
    public long? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
