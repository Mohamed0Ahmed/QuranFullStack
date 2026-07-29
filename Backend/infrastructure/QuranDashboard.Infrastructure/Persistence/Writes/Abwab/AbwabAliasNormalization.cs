namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

// One rule, one place. The doors writer diffs alias rows and the apply writer inserts them, but both
// must agree on what counts as the same alias — otherwise a copied door's aliases drift from a
// hand-authored door's the first time either side changes the rule.
internal static class AbwabAliasNormalization
{
    public static IReadOnlyList<string> Normalize(IReadOnlyList<string> aliases) =>
        [.. aliases.Select(a => a.Trim()).Where(a => a.Length > 0).Distinct()];
}
