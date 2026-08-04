namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

internal static class AbwabAliasNormalization
{
    public static IReadOnlyList<string> Normalize(IReadOnlyList<string> aliases) =>
        [.. aliases.Select(a => a.Trim()).Where(a => a.Length > 0).Distinct()];
}
