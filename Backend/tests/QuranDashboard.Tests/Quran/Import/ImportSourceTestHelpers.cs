using System.Text.Json.Nodes;

namespace QuranDashboard.Tests.Quran.Import;

internal static class ImportSourceTestHelpers
{
    public static string CopySourceToTemp(string validSourceRoot)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"quran-import-test-{Guid.NewGuid():N}");
        CopyDirectory(validSourceRoot, tempDir);
        return tempDir;
    }

    public static void IntroduceDuplicateWordId(string sourceRoot)
    {
        var wordFiles = new[]
        {
            Path.Combine(sourceRoot, "mushaf", "qpc-v4.json"),
            Path.Combine(sourceRoot, "words", "uthmani.json"),
            Path.Combine(sourceRoot, "words", "uthmani-simple.json"),
            Path.Combine(sourceRoot, "words", "imlaei-simple.json")
        };

        var referencePath = wordFiles[0];
        var referenceRoot = JsonNode.Parse(File.ReadAllText(referencePath))!.AsObject();
        var locations = referenceRoot.Select(pair => pair.Key).Take(2).ToArray();

        if (locations.Length < 2)
        {
            throw new InvalidOperationException("Word source must contain at least two word records.");
        }

        var duplicateId = referenceRoot[locations[0]]!["id"]!.GetValue<int>();

        foreach (var wordFile in wordFiles)
        {
            var root = JsonNode.Parse(File.ReadAllText(wordFile))!.AsObject();
            root[locations[1]]!["id"] = duplicateId;
            File.WriteAllText(wordFile, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(directory, Path.Combine(destinationDir, Path.GetFileName(directory)));
        }
    }
}
