namespace QuranDashboard.Application.Abstractions.Abwab;

// Thrown by an Abwab writer when the per-sibling (or per-section) NULLS NOT DISTINCT unique index
// rejects a write, mirroring UserProvisioningEmailConflictException's exception-based 409 shape.
// name is null for batch writes (bulk-move), where the offending row isn't singled out.
public sealed class AbwabDuplicateNameException(string? name = null) : Exception(
    name is null
        ? "An Abwab entry name collides with an existing sibling in this scope."
        : $"An Abwab entry named '{name}' already exists in this scope.")
{
    public string? Name { get; } = name;
}
