using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public interface IPhraseQueryResolutionReader
{
    Task<PhraseQueryResolutionReadResult> ResolveAsync(
        PhraseTextMode mode,
        IReadOnlyList<string> normalizedSegments,
        CancellationToken cancellationToken);
}

public abstract record PhraseQueryResolutionReadResult
{
    private PhraseQueryResolutionReadResult() { }

    public sealed record Success(PhraseQueryResolutionResponse Value) : PhraseQueryResolutionReadResult;
    public sealed record Unavailable : PhraseQueryResolutionReadResult;
    public sealed record TooComplex : PhraseQueryResolutionReadResult;
}
