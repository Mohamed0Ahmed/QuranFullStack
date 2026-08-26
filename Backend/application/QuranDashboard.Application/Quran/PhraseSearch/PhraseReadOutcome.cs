namespace QuranDashboard.Application.Quran.PhraseSearch;

public abstract record PhraseReadOutcome<T>
{
    private PhraseReadOutcome() { }

    public sealed record Success(T Response) : PhraseReadOutcome<T>;
    public sealed record Invalid(PhraseRequestInvalidKind Kind) : PhraseReadOutcome<T>;
    public sealed record Unavailable : PhraseReadOutcome<T>;
    public sealed record BuildChanged : PhraseReadOutcome<T>;
    public sealed record NotFound : PhraseReadOutcome<T>;
}

public enum PhraseRequestInvalidKind
{
    Mode,
    Query,
    QueryEncoding,
    QueryTooLong,
    Reference,
    Cursor,
    Paging,
    Length,
    Threshold,
    MinimumMatchedWords,
}
