namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public sealed class WordTypeRowIdentity
{
    private WordTypeRowIdentity(int tashkeelWordId, string contextCode, string? @case, string? tense, string? voice)
    {
        TashkeelWordId = tashkeelWordId;
        ContextCode = contextCode;
        Case = @case;
        Tense = tense;
        Voice = voice;
    }

    public int TashkeelWordId { get; }
    public string ContextCode { get; }
    public string? Case { get; }
    public string? Tense { get; }
    public string? Voice { get; }

    public static WordTypeRowIdentity? Create(int tashkeelWordId, string? contextCode, string? @case, string? tense, string? voice)
    {
        var normalizedContextCode = WordTypeScope.NormalizeOptional(contextCode);
        var normalizedCase = WordTypeScope.NormalizeOptional(@case);
        var normalizedTense = WordTypeScope.NormalizeOptional(tense);
        var normalizedVoice = WordTypeScope.NormalizeOptional(voice);

        return tashkeelWordId <= 0
            || normalizedContextCode is null
            || !WordTypeScope.AreValidIdentitySecondaryValues(@case, tense, voice)
                ? null
                : new WordTypeRowIdentity(tashkeelWordId, normalizedContextCode, normalizedCase, normalizedTense, normalizedVoice);
    }
}
