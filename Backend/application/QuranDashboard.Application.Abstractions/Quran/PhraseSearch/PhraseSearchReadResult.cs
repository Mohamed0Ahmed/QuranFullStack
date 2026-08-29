namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public abstract record PhraseSearchReadResult<T>
{
    private PhraseSearchReadResult() { }

    public sealed record Success(T Value) : PhraseSearchReadResult<T>;
    public sealed record Unavailable : PhraseSearchReadResult<T>;
    public sealed record BuildChanged : PhraseSearchReadResult<T>;
    public sealed record NotFound : PhraseSearchReadResult<T>;
    public sealed record InvalidReference : PhraseSearchReadResult<T>;
    public sealed record InvalidSelection : PhraseSearchReadResult<T>;
}
