namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;

public sealed class JsonQulRootReader
{
    public async Task<IReadOnlyDictionary<string, string>> ReadAsync(
        string filePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return await ReadLocationMapAsync(filePath, ct);
    }

    internal static async Task<IReadOnlyDictionary<string, string>> ReadLocationMapAsync(
        string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var map = new Dictionary<string, string>(document.RootElement.EnumerateObject().Count());

        foreach (var entry in document.RootElement.EnumerateObject())
        {
            var location = entry.Name;
            var value = entry.Value.ValueKind == JsonValueKind.String
                ? entry.Value.GetString()
                : entry.Value.GetRawText().Trim('"');

            if (!string.IsNullOrWhiteSpace(value))
            {
                map[location] = value;
            }
        }

        return map;
    }
}

public sealed class JsonQulLemmaReader
{
    public async Task<IReadOnlyDictionary<string, string>> ReadAsync(
        string filePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return await JsonQulRootReader.ReadLocationMapAsync(filePath, ct);
    }
}

public sealed class JsonQulStemReader
{
    public async Task<IReadOnlyDictionary<string, string>> ReadAsync(
        string filePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return await JsonQulRootReader.ReadLocationMapAsync(filePath, ct);
    }
}
