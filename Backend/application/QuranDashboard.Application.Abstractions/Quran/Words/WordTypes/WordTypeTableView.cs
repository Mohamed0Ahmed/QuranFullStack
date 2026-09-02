namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public sealed class WordTypeTableView
{
    private const string Words = "words";
    private const string Roots = "roots";
    private const string Stems = "stems";
    private const string Lemmas = "lemmas";

    private WordTypeTableView(string key) => Key = key;

    public string Key { get; }

    public static WordTypeTableView? Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new WordTypeTableView(Words);
        }

        var key = value.Trim().ToLowerInvariant();
        return key switch
        {
            Words or Roots or Stems or Lemmas =>
                new WordTypeTableView(key),
            _ => null,
        };
    }
}
