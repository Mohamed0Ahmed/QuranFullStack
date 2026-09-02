using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

internal static class PhraseLinkingAyahSelectionModeParser
{
    internal static bool TryParse(
        string? value,
        out PhraseLinkingAyahSelectionMode mode)
    {
        mode = value switch
        {
            "only" => PhraseLinkingAyahSelectionMode.Only,
            "all-except" => PhraseLinkingAyahSelectionMode.AllExcept,
            _ => default,
        };
        return mode != default;
    }
}
