using System.Security.Cryptography;
using QuranDashboard.Application.Abstractions.Quran.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

/// <summary>
/// Writes synthetic navigation source packages for unit/integration tests without PostgreSQL.
/// </summary>
internal static class NavigationSyntheticPackageWriter
{
    public static async Task<string> WritePackageAsync(
        NavigationExpectedCounts expectedCounts,
        SyntheticNavigationPackageSpec? spec = null,
        Action<string>? registerTempDir = null)
    {
        spec ??= NavigationSyntheticSeed.DefaultPackageSpec;

        var packageDir = Path.Combine(Path.GetTempPath(), $"quran-navigation-{Guid.NewGuid():N}");
        registerTempDir?.Invoke(packageDir);
        Directory.CreateDirectory(packageDir);
        Directory.CreateDirectory(Path.Combine(packageDir, "sources"));

        var sourceFiles = new List<object>();

        await WriteDivisionFileAsync(packageDir, "sources/quran-metadata-juz.json", spec.Juz, "juz_number", sourceFiles, "juz");
        await WriteDivisionFileAsync(packageDir, "sources/quran-metadata-hizb.json", spec.Hizb, "hizb_number", sourceFiles, "hizb");
        await WriteDivisionFileAsync(packageDir, "sources/quran-metadata-rub.json", spec.Rub, "rub_number", sourceFiles, "rub");
        await WriteSajdaFileAsync(packageDir, spec.Sajda, sourceFiles);

        var manifest = new
        {
            packageType = NavigationImportConstants.ManifestType,
            isFinalImportManifest = true,
            expectedCounts = new
            {
                juz = expectedCounts.Juz,
                hizb = expectedCounts.Hizb,
                rub = expectedCounts.Rub,
                sajda = expectedCounts.Sajda
            },
            sourceFiles
        };

        await WriteJsonAsync(Path.Combine(packageDir, "manifest.json"), manifest);
        return packageDir;
    }

    private static async Task WriteDivisionFileAsync(
        string packageDir,
        string relativePath,
        IReadOnlyList<SyntheticNavigationDivisionSpec> divisions,
        string numberField,
        List<object> sourceFiles,
        string datasetKey)
    {
        var fullPath = Path.Combine(packageDir, relativePath);
        var payload = new Dictionary<string, object>();
        foreach (var division in divisions)
        {
            payload[division.Number.ToString()] = new Dictionary<string, object>
            {
                [numberField] = division.Number,
                ["verses_count"] = division.VersesCount,
                ["first_verse_key"] = division.FirstVerseKey,
                ["last_verse_key"] = division.LastVerseKey,
                ["verse_mapping"] = division.VerseMapping
            };
        }

        await WriteJsonAsync(fullPath, payload);
        var fileBytes = await File.ReadAllBytesAsync(fullPath);
        sourceFiles.Add(new
        {
            relativePath,
            datasetKey,
            recordCount = divisions.Count,
            sha256 = Convert.ToHexString(SHA256.HashData(fileBytes)),
            sizeBytes = fileBytes.LongLength
        });
    }

    private static async Task WriteSajdaFileAsync(
        string packageDir,
        IReadOnlyList<SyntheticNavigationSajdaSpec> sajdas,
        List<object> sourceFiles)
    {
        const string relativePath = "sources/quran-metadata-sajda.json";
        var fullPath = Path.Combine(packageDir, relativePath);
        var payload = new Dictionary<string, object>();
        foreach (var sajda in sajdas)
        {
            payload[sajda.SajdahNumber.ToString()] = new
            {
                sajdah_number = sajda.SajdahNumber,
                verse_key = sajda.VerseKey,
                sajdah_type = sajda.SajdahType
            };
        }

        await WriteJsonAsync(fullPath, payload);
        var fileBytes = await File.ReadAllBytesAsync(fullPath);
        sourceFiles.Add(new
        {
            relativePath,
            datasetKey = "sajda",
            recordCount = sajdas.Count,
            sha256 = Convert.ToHexString(SHA256.HashData(fileBytes)),
            sizeBytes = fileBytes.LongLength
        });
    }

    private static async Task WriteJsonAsync(string path, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }
}

/// <summary>
/// Deletes temp package directories created during a unit test.
/// </summary>
internal sealed class NavigationTempPackageScope : IAsyncDisposable
{
    private readonly List<string> tempDirs = new();

    public void Register(string packageDir) => tempDirs.Add(packageDir);

    public ValueTask DisposeAsync()
    {
        foreach (var dir in tempDirs)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch (IOException)
            {
            }
        }

        return ValueTask.CompletedTask;
    }
}
