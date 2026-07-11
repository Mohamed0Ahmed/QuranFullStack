namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public enum WordTypeTableView
{
    Words,
    Roots,
    Stems,
    Lemmas,
}

public static class WordTypeTableViewKeys
{
    public const string Words = "words";
    public const string Roots = "roots";
    public const string Stems = "stems";
    public const string Lemmas = "lemmas";
}

public static class WordTypeTableViewParser
{
    // Missing/blank tableView defaults to Words; only a non-blank unknown value is a controlled failure.
    public static bool TryParse(string? value, out WordTypeTableView tableView)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            tableView = WordTypeTableView.Words;
            return true;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case WordTypeTableViewKeys.Words:
                tableView = WordTypeTableView.Words;
                return true;
            case WordTypeTableViewKeys.Roots:
                tableView = WordTypeTableView.Roots;
                return true;
            case WordTypeTableViewKeys.Stems:
                tableView = WordTypeTableView.Stems;
                return true;
            case WordTypeTableViewKeys.Lemmas:
                tableView = WordTypeTableView.Lemmas;
                return true;
            default:
                // Same contract as WordTypeSortParser: callers MUST check the bool. `tableView` on
                // failure is unspecified (currently Words, the enum's default) and must not be read.
                tableView = default;
                return false;
        }
    }
}
