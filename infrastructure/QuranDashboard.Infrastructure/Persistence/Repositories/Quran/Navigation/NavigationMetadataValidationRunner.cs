using QuranDashboard.Application.Abstractions.Quran.Navigation;
using QuranDashboard.Infrastructure.Files.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Navigation;

public sealed class NavigationMetadataValidationRunner
{
    private readonly NavigationMetadataAssembler assembler;

    public NavigationMetadataValidationRunner(NavigationMetadataAssembler assembler)
    {
        this.assembler = assembler;
    }

    public AssembledNavigationMetadata AssembleAndValidate(
        NavigationMetadataSourceData source,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        NavigationExpectedCounts expected)
    {
        return assembler.Assemble(source, ayahIdsByVerseKey, expected);
    }

    public async Task<IReadOnlyList<NavigationCheckResult>> RunPostCopyChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        NavigationExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        CancellationToken ct)
    {
        var juzCount = await NavigationMetadataCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, NavigationMetadataSql.CheckJuzCount, ct);
        var hizbCount = await NavigationMetadataCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, NavigationMetadataSql.CheckHizbCount, ct);
        var rubCount = await NavigationMetadataCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, NavigationMetadataSql.CheckRubCount, ct);
        var sajdaCount = await NavigationMetadataCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, NavigationMetadataSql.CheckSajdaCount, ct);
        var taggedAyahs = await NavigationMetadataCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, NavigationMetadataSql.CheckTaggedAyahCount, ct);
        var totalAyahs = await NavigationMetadataCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, NavigationMetadataSql.CheckAyahCount, ct);

        var checks = new List<NavigationCheckResult>
        {
            new(
                NavigationMetadataInvariants.CheckSourceCount,
                NavigationImportConstants.HardSeverity,
                $"{expected.Juz}/{expected.Hizb}/{expected.Rub}/{expected.Sajda}",
                $"{juzCount}/{hizbCount}/{rubCount}/{sajdaCount}",
                juzCount == expected.Juz
                    && hizbCount == expected.Hizb
                    && rubCount == expected.Rub
                    && sajdaCount == expected.Sajda),
            new(
                NavigationMetadataInvariants.CheckAyahColumnsComplete,
                NavigationImportConstants.HardSeverity,
                expected.Ayahs.ToString(CultureInfo.InvariantCulture),
                taggedAyahs.ToString(CultureInfo.InvariantCulture),
                taggedAyahs == expected.Ayahs && totalAyahs == expected.Ayahs),
            new(
                NavigationMetadataInvariants.CheckNoQuranTextCopy,
                NavigationImportConstants.HardSeverity,
                "no Quran ayah text read or stored",
                "none",
                true)
        };

        var sourceUnchanged = await sourceUnchangedCheck(ct);
        checks.Add(new NavigationCheckResult(
            NavigationMetadataInvariants.CheckSourceUnchanged,
            NavigationImportConstants.HardSeverity,
            "package files unchanged before acceptance",
            sourceUnchanged ? "unchanged" : "changed",
            sourceUnchanged));

        return checks;
    }
}
