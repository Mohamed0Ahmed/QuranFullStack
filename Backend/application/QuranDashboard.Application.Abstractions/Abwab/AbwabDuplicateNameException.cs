namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabDuplicateNameException(string? name = null) : Exception(
    name is null
        ? "An Abwab entry name collides with an existing sibling in this scope."
        : $"An Abwab entry named '{name}' already exists in this scope.")
{
    public string? Name { get; } = name;
}
