using System.Text.Json;

namespace QuranDashboard.Application.Abwab;

internal static class AbwabAuditPayload
{
    public static string Serialize(string type, object data) => JsonSerializer.Serialize(new { type, data });
}
