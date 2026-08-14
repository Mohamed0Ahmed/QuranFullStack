using QuranDashboard.Application.Abstractions.Linking.Preflight;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

internal static class LinkingPreparedSchemaConstraints
{
    public static IReadOnlyList<string> ClassificationTokens { get; } =
        Enum.GetValues<LinkingPreflightClassification>()
            .Select(LinkingPreflightTokens.ToToken)
            .ToList();

    public static IReadOnlyList<string> InvalidReasonTokens { get; } =
        Enum.GetValues<LinkingPreflightInvalidReason>()
            .Select(reason => LinkingPreflightTokens.ToToken(reason)!)
            .ToList();

    public static string FixedBinaryHash(string column) =>
        $"octet_length({column}) = 32";

    public static string RequiredHexHash(string column) =>
        $"{column} ~ '^[0-9a-f]{{64}}$'";

    public static string OptionalHexHash(string column) =>
        $"{column} IS NULL OR {RequiredHexHash(column)}";

    public static string JsonbSchemaVersionMatches(string documentColumn, string versionColumn) =>
        $"""
        {LinkingDescriptorCheckConstraints.JsonbSchemaVersion(documentColumn)}
        AND ({documentColumn} ->> 'schemaVersion')::integer = {versionColumn}
        """;
}
