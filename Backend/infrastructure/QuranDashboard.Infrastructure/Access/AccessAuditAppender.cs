using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Access;

public sealed class AccessAuditAppender(
    QuranDashboardDbContext db,
    IAccessRequestContext requestContext) : IAccessAuditAppender
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void Append(AccessAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        db.AccessAuditEvents.Add(new AccessAuditEvent(
            DateTimeOffset.UtcNow,
            entry.ActionType,
            AccessAuditActorType.User,
            entry.ActorUserId,
            entry.Target.Id,
            Serialize(entry.Actor),
            Serialize(entry.Target),
            entry.PermissionCode,
            Serialize(entry.Before),
            Serialize(entry.After),
            entry.Reason,
            new AccessAuditMetadata(
                1,
                requestContext.CorrelationId,
                new Dictionary<string, string>
                {
                    ["operation"] = entry.Operation,
                })));
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, SerializerOptions);
}
