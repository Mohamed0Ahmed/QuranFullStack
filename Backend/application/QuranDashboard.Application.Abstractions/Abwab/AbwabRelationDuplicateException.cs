namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabRelationDuplicateException(IReadOnlyList<string> doorNames) : Exception(
    doorNames.Count == 0
        ? "A relation of this type already exists for one of these door pairs."
        : $"A relation of this type already exists with: {string.Join(", ", doorNames)}.")
{
    public IReadOnlyList<string> DoorNames { get; } = doorNames;
}
