using QuranDashboard.Application.Abstractions.Quran.FullI3rab;
using QuranDashboard.Infrastructure.Files.Quran.FullI3rab;

namespace QuranDashboard.Tests.Quran.FullI3rab;

public sealed class FullI3rabAssemblerTests : IDisposable
{
    private readonly FullI3rabAssembler assembler = new();
    private readonly FullI3rabManifestReader manifestReader = new();
    private readonly JsonFullI3rabSourceReader sourceReader = new();
    private readonly FullI3rabSyntheticPackage packages = new();

    [Fact]
    public async Task Assemble_resolves_verse_keys_stores_grouped_html_once_and_expands_all_three_value_kinds()
    {
        var packageDir = await packages.WriteAsync();
        var manifest = await manifestReader.ReadAsync(packageDir, CancellationToken.None);
        var parsedSource = await sourceReader.ReadAsync(
            packages.GetSourceFilePath(packageDir, "test-muyassar"),
            CancellationToken.None);

        var ayahIds = FullI3rabSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey,
            pair => pair.Id,
            StringComparer.Ordinal);

        var result = assembler.Assemble(
            manifest,
            new Dictionary<string, ParsedFullI3rabSourceFile>(StringComparer.Ordinal)
            {
                ["test-muyassar"] = parsedSource
            },
            ayahIds,
            expectedAyahsPerSource: 3);

        result.Sources.Should().ContainSingle();
        result.Entries.Should().HaveCount(2);
        result.AyahEntries.Should().HaveCount(3);

        var groupedEntry = result.Entries.Single(entry => entry.SourceEntryKey == "900:1");
        groupedEntry.SourceShape.Should().Be(FullI3rabImportConstants.SourceShapeGroupedLeader);
        groupedEntry.CoveredAyahCount.Should().Be(2);
        groupedEntry.I3rabHtml.Should().Be(FullI3rabSyntheticSeed.SyntheticHtml("900:1"));
        groupedEntry.TextHash.Should().Be(FullI3rabAssembler.ComputeTextHash(groupedEntry.I3rabHtml));

        result.Entries.Count(entry => entry.I3rabHtml == groupedEntry.I3rabHtml).Should().Be(1);

        var flatEntry = result.Entries.Single(entry => entry.SourceEntryKey == "900:3");
        flatEntry.SourceShape.Should().Be(FullI3rabImportConstants.SourceShapeFlat);
        flatEntry.CoveredAyahCount.Should().Be(1);

        result.AyahEntries.Should().Contain(entry =>
            entry.VerseKey == "900:2"
            && entry.SourceValueKind == FullI3rabImportConstants.ValueKindMemberPointer
            && entry.SourceLeaderVerseKey == "900:1"
            && !entry.IsGroupLeader);

        result.AyahEntries.Should().Contain(entry =>
            entry.VerseKey == "900:1"
            && entry.SourceValueKind == FullI3rabImportConstants.ValueKindLeader
            && entry.IsGroupLeader);

        result.AyahEntries.Should().Contain(entry =>
            entry.VerseKey == "900:3"
            && entry.SourceValueKind == FullI3rabImportConstants.ValueKindFlat
            && !entry.IsGroupLeader);

        result.AyahEntries.Select(entry => entry.AyahId).Distinct().Should().HaveCount(3);
    }

    [Fact]
    public async Task Assemble_canonicalizes_resource_kind_and_provenance_on_source_dto()
    {
        var packageDir = await packages.WriteAsync();
        var manifest = await manifestReader.ReadAsync(packageDir, CancellationToken.None);
        var parsedSource = await sourceReader.ReadAsync(
            packages.GetSourceFilePath(packageDir, "test-muyassar"),
            CancellationToken.None);
        var ayahIds = FullI3rabSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey, pair => pair.Id, StringComparer.Ordinal);

        var result = assembler.Assemble(
            manifest,
            new Dictionary<string, ParsedFullI3rabSourceFile>(StringComparer.Ordinal)
            {
                ["test-muyassar"] = parsedSource
            },
            ayahIds,
            expectedAyahsPerSource: 3);

        var source = result.Sources.Single();
        source.ResourceKind.Should().Be(FullI3rabImportConstants.ResourceKind);
        source.MarkupFormat.Should().Be(FullI3rabImportConstants.MarkupFormatHtml);
        source.LicenseStatus.Should().Be(FullI3rabImportConstants.LicenseStatus);
        source.ProvenanceStatus.Should().Be(FullI3rabImportConstants.ProvenanceStatus);
        source.UsageScope.Should().Be(FullI3rabImportConstants.UsageScope);
        source.HasQuranQuotationMarkup.Should().BeTrue();
    }

    [Fact]
    public async Task AssembleSource_emits_html_allowlist_warning_for_disallowed_markup_without_blocking()
    {
        var sourceKey = "test-disallowed";
        var spec = new SyntheticFullI3rabSourceSpec(
            SourceKey: sourceKey,
            HasQuranQuotationMarkup: false,
            CanonicalAyahCoverage: FullI3rabInvariants.ExpectedAyahsPerSource,
            Entries: new Dictionary<string, object>
            {
                ["900:1"] = new { text = "<table class=\"ar\"><tr><td>إعراب غير آمن</td></tr></table>" },
                ["900:2"] = new { text = "<div class=\"ar\"><p>إعراب اختباري 900:2</p></div>" },
                ["900:3"] = new { text = "<div class=\"ar\"><p>إعراب اختباري 900:3</p></div>" }
            },
            ObservedHtmlTags: new Dictionary<string, int>(),
            ObservedHtmlClasses: new Dictionary<string, int>());

        var packageDir = await packages.WriteAsync(sources: [spec]);
        var manifest = await manifestReader.ReadAsync(packageDir, CancellationToken.None);
        var parsedSource = await sourceReader.ReadAsync(
            packages.GetSourceFilePath(packageDir, sourceKey),
            CancellationToken.None);
        var ayahIds = FullI3rabSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey, pair => pair.Id, StringComparer.Ordinal);

        var assembled = assembler.AssembleSource(
            manifest.ApprovedSources.Single(),
            manifest,
            parsedSource,
            ayahIds,
            new HashSet<(string SourceKey, int AyahId)>(),
            expectedAyahsPerSource: 3);

        assembled.Entries.Should().HaveCount(3);
        assembled.Warnings.Should().NotBeEmpty();
        assembled.Warnings.Should().OnlyContain(warning =>
            warning.Id == FullI3rabInvariants.CheckHtmlAllowlist
            && warning.Severity == FullI3rabImportConstants.WarningSeverity);
    }

    [Fact]
    public async Task Assemble_emits_no_warning_when_html_stays_within_allowlist()
    {
        var packageDir = await packages.WriteAsync();
        var manifest = await manifestReader.ReadAsync(packageDir, CancellationToken.None);
        var parsedSource = await sourceReader.ReadAsync(
            packages.GetSourceFilePath(packageDir, "test-muyassar"),
            CancellationToken.None);
        var ayahIds = FullI3rabSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey, pair => pair.Id, StringComparer.Ordinal);

        var assembled = assembler.AssembleSource(
            manifest.ApprovedSources.Single(),
            manifest,
            parsedSource,
            ayahIds,
            new HashSet<(string SourceKey, int AyahId)>(),
            expectedAyahsPerSource: 3);

        assembled.Warnings.Should().BeEmpty();
    }

    public void Dispose() => packages.Dispose();
}
