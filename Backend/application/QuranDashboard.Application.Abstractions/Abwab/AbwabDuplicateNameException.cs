namespace QuranDashboard.Application.Abstractions.Abwab;

// Thrown by an Abwab writer when the per-sibling (or per-section) NULLS NOT DISTINCT unique index
// rejects a write, mirroring UserProvisioningEmailConflictException's exception-based 409 shape.
public sealed class AbwabDuplicateNameException(string name) : Exception(
    $"An Abwab entry named '{name}' already exists in this scope.")
{
    public string Name { get; } = name;
}
