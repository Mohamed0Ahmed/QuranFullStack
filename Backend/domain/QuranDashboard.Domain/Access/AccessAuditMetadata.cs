namespace QuranDashboard.Domain.Access;

public sealed class AccessAuditMetadata
{
    private static readonly IReadOnlyDictionary<string, string> EmptyProvenance =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public AccessAuditMetadata(
        int schemaVersion,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? provenance = null)
    {
        if (schemaVersion < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(schemaVersion),
                schemaVersion,
                "Audit metadata requires a positive schema version.");
        }

        if (correlationId is not null && string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException(
                "Audit correlation identifier must be null or non-blank.",
                nameof(correlationId));
        }

        SchemaVersion = schemaVersion;
        CorrelationId = correlationId;
        Provenance = provenance is null
            ? EmptyProvenance
            : new Dictionary<string, string>(provenance, StringComparer.Ordinal);
    }

    public int SchemaVersion { get; }
    public string? CorrelationId { get; }
    public IReadOnlyDictionary<string, string> Provenance { get; }
}
