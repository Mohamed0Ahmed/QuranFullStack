using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal static class LinkingPreparedSnapshotCodec
{
    private const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string EncodeDescriptor(
        LinkingSourceDescriptor descriptor,
        IReadOnlyList<string> manualVerseKeys)
    {
        var form = LinkingSourceStorage.Encode(descriptor);
        return JsonSerializer.Serialize(
            new DescriptorDocument(
                SchemaVersion,
                LinkingSourceTokens.ToToken(form.Kind),
                form.Label,
                form.SourceIdentity,
                form.ScopeJson,
                form.RootId,
                form.LemmaId,
                form.StemId,
                form.UniqueSimpleWordId,
                form.UniqueTashkeelWordId,
                form.WordTypeTashkeelWordId,
                manualVerseKeys),
            JsonOptions);
    }

    public static LinkingSourceDescriptor DecodeDescriptor(string json)
    {
        var document = JsonSerializer.Deserialize<DescriptorDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("The prepared linking descriptor document is empty.");
        if (document.SchemaVersion != SchemaVersion
            || !LinkingSourceTokens.TryParseKind(document.Kind, out var kind))
        {
            throw new InvalidOperationException("The prepared linking descriptor schema is unsupported.");
        }

        var source = new LinkingWorkspaceSource
        {
            SourceKind = kind,
            SourceIdentity = document.SourceIdentity,
            SourceIdentityHash = LinkingSourceIdentity.HashOf(document.SourceIdentity),
            Label = document.Label,
            ScopeJson = document.ScopeJson,
            RootId = document.RootId,
            LemmaId = document.LemmaId,
            StemId = document.StemId,
            UniqueSimpleWordId = document.UniqueSimpleWordId,
            UniqueTashkeelWordId = document.UniqueTashkeelWordId,
            WordTypeTashkeelWordId = document.WordTypeTashkeelWordId,
        };
        return LinkingSourceStorage.Decode(source, document.ManualVerseKeys);
    }

    public static string EncodeConfiguration(LinkingWorkspaceConfigurationInput configuration) =>
        JsonSerializer.Serialize(
            new ConfigurationDocument(
                SchemaVersion,
                LinkingWorkspaceTokens.ToToken(configuration.InclusionMode),
                [.. configuration.AyahOverrides.Distinct().Order()],
                [.. configuration.SelectedWords
                    .Distinct()
                    .OrderBy(word => word.AyahId)
                    .ThenBy(word => word.QuranWordId)],
                configuration.AutomaticWordMatchesEnabled,
                configuration.ManualLinkShape is null
                    ? null
                    : LinkingWorkspaceTokens.ToToken(configuration.ManualLinkShape.Value),
                [.. configuration.Descriptions
                    .OrderBy(description => description.AyahId)
                    .ThenBy(description => description.OrderValue)]),
            JsonOptions);

    public static LinkingWorkspaceConfigurationInput DecodeConfiguration(string json, string label)
    {
        var document = JsonSerializer.Deserialize<ConfigurationDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("The prepared linking configuration document is empty.");
        if (document.SchemaVersion != SchemaVersion
            || !LinkingWorkspaceTokens.TryParseInclusionMode(document.InclusionMode, out var inclusionMode))
        {
            throw new InvalidOperationException("The prepared linking configuration schema is unsupported.");
        }

        LinkingManualLinkShape? shape = null;
        if (document.ManualLinkShape is not null)
        {
            if (!LinkingWorkspaceTokens.TryParseManualLinkShape(document.ManualLinkShape, out var parsed))
            {
                throw new InvalidOperationException("The prepared linking manual shape is unsupported.");
            }

            shape = parsed;
        }

        return new LinkingWorkspaceConfigurationInput(
            label,
            inclusionMode,
            document.AyahOverrideIds,
            document.SelectedWords,
            document.AutomaticWordMatchesEnabled,
            shape,
            document.Descriptions);
    }

    private sealed record DescriptorDocument(
        int SchemaVersion,
        string Kind,
        string Label,
        string SourceIdentity,
        string ScopeJson,
        int? RootId,
        int? LemmaId,
        int? StemId,
        int? UniqueSimpleWordId,
        int? UniqueTashkeelWordId,
        int? WordTypeTashkeelWordId,
        IReadOnlyList<string> ManualVerseKeys);

    private sealed record ConfigurationDocument(
        int SchemaVersion,
        string InclusionMode,
        IReadOnlyList<int> AyahOverrideIds,
        IReadOnlyList<LinkingWorkspaceSelectedWordInput> SelectedWords,
        bool? AutomaticWordMatchesEnabled,
        string? ManualLinkShape,
        IReadOnlyList<LinkingWorkspaceDescriptionInput> Descriptions);
}
