namespace Hemo.Domain.Security;

public sealed class HemoUserSession
{
    public long UserSessionId { get; set; }
    public long AccountUserId { get; set; }
    public byte[] RefreshTokenHash { get; set; } = [];
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ClientIp { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class AuditEvent
{
    public long AuditEventId { get; set; }
    public long? AccountUserId { get; set; }
    public long? FacilityId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string EntityType { get; set; } = "";
    public long? EntityId { get; set; }
    public string ActionType { get; set; } = "";
    public Guid? CorrelationId { get; set; }
    public string? RemoteAddress { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? EventDetails { get; set; }
}
