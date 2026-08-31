using System.Text.Json;
using System.Text.Json.Nodes;

namespace QuranDashboard.Tests.Quran.QuranTopicsBook;

internal static class QuranTopicsBookSyntheticPackageWriter
{
    internal static async Task<QuranTopicsBookSyntheticPackage> WriteAsync(
        string directory,
        Action<JsonObject>? configure = null)
    {
        Directory.CreateDirectory(directory);
        const string fileName = "quran-topics-book.json";
        var sourcePath = Path.Combine(directory, fileName);
        var document = JsonNode.Parse(
            """
            {
              "format": "quran-dashboard-quran-topics-book",
              "formatVersion": 1,
              "title": "Synthetic Quran topics book",
              "source": {
                "fileName": "synthetic-quran-topics-source.pdf",
                "sha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "pdfPageFrom": 2,
                "pdfPageTo": 4
              },
              "policy": {
                "parentAyahPolicy": "direct_only",
                "groupingPolicy": "consecutive_ranges_grouped"
              },
              "sections": [
                {
                  "key": "section-01",
                  "name": "Synthetic section",
                  "order": 1,
                  "doors": [
                    {
                      "key": "section-01.door-01",
                      "parentKey": null,
                      "name": "Synthetic root",
                      "order": 1,
                      "globalOrder": 1,
                      "pdfPages": [2],
                      "ayahGroups": [
                        { "order": 1, "kind": "single", "verseKeys": ["1:1"] }
                      ]
                    },
                    {
                      "key": "section-01.door-02",
                      "parentKey": "section-01.door-01",
                      "name": "Synthetic child",
                      "order": 1,
                      "globalOrder": null,
                      "pdfPages": [3],
                      "ayahGroups": [
                        {
                          "order": 1,
                          "kind": "consecutive_range",
                          "verseKeys": ["1:2", "1:3"]
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """)!.AsObject();
        configure?.Invoke(document);

        await File.WriteAllTextAsync(
            sourcePath,
            document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        await WriteChecksumAsync(sourcePath);
        return new QuranTopicsBookSyntheticPackage(sourcePath);
    }

    internal static async Task WriteChecksumAsync(string sourcePath)
    {
        var sourceBytes = await File.ReadAllBytesAsync(sourcePath);
        var checksum = Convert.ToHexStringLower(SHA256.HashData(sourceBytes));
        await WriteChecksumSidecarAsync(sourcePath, checksum);
    }

    internal static Task WriteChecksumSidecarAsync(
        string sourcePath,
        string checksum,
        string? fileName = null) =>
        File.WriteAllTextAsync(
            sourcePath + ".sha256",
            $"{checksum}  {fileName ?? Path.GetFileName(sourcePath)}{Environment.NewLine}");
}

internal sealed record QuranTopicsBookSyntheticPackage(string SourcePath);
