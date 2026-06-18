using System.Text.RegularExpressions;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetWordAnalysis;

public sealed partial class GetWordAnalysisHandler(IWordAnalysisReader wordAnalysisReader)
{
    public async Task<GetWordAnalysisOutcome> HandleAsync(GetWordAnalysisQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!WordLocationPattern().IsMatch(query.WordLocation))
        {
            return new GetWordAnalysisOutcome.InvalidWordLocation();
        }

        var outcome = await wordAnalysisReader.GetWordAnalysisAsync(query.WordLocation, ct);

        return outcome switch
        {
            WordAnalysisOutcome.Found found => new GetWordAnalysisOutcome.Success(found.Response),
            WordAnalysisOutcome.NotFound => new GetWordAnalysisOutcome.NotFound(),
            WordAnalysisOutcome.NotAnalyzable => new GetWordAnalysisOutcome.NotAnalyzable(),
            WordAnalysisOutcome.IncompleteData => new GetWordAnalysisOutcome.IncompleteData(),
            _ => throw new InvalidOperationException($"Unhandled {nameof(WordAnalysisOutcome)} variant."),
        };
    }

    [GeneratedRegex(@"^\d+:\d+:\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex WordLocationPattern();
}
