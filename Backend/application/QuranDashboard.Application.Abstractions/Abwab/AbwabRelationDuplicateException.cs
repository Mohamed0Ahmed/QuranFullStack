namespace QuranDashboard.Application.Abstractions.Abwab;

// The pair is stored canonically for every type, so this fires for the opposite direction of an
// existing comprehensiveness row too — A cannot be both more and less comprehensive than B.
// DoorNames is empty when the race backstop catches 23505: Postgres does not say which row lost.
public sealed class AbwabRelationDuplicateException(IReadOnlyList<string> doorNames) : Exception(
    doorNames.Count == 0
        ? "A relation of this type already exists for one of these door pairs."
        : $"A relation of this type already exists with: {string.Join(", ", doorNames)}.")
{
    public IReadOnlyList<string> DoorNames { get; } = doorNames;
}
