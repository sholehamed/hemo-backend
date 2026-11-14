using Hemo.Infrastructure.Persistence;

namespace Hemo.Infrastructure.Security;

/// <summary>Only operational metadata belongs here; never include patient data or passwords.</summary>
public sealed class HemoAuditWriter(HemoDbContext db)
{
    public void Record(long? actorId, long? facilityId, string type, long? entityId,
        string action, string? remoteAddress = null)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            AccountUserId = actorId,
            FacilityId = facilityId,
            EntityType = type,
            EntityId = entityId,
            ActionType = action,
            RemoteAddress = remoteAddress,
            OccurredAt = DateTime.UtcNow
        });
    }
}
