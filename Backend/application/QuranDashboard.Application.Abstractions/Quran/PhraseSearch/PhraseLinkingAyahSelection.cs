namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public sealed class PhraseLinkingAyahSelection
{
    private PhraseLinkingAyahSelection(
        PhraseLinkingAyahSelectionMode mode,
        int[] overrideAyahIds) =>
        (Mode, OverrideAyahIds) = (mode, Array.AsReadOnly(overrideAyahIds));

    public PhraseLinkingAyahSelectionMode Mode { get; }

    public IReadOnlyList<int> OverrideAyahIds { get; }

    public static bool TryCreate(
        PhraseLinkingAyahSelectionMode mode,
        IReadOnlyList<int>? overrideAyahIds,
        out PhraseLinkingAyahSelection? selection)
    {
        selection = null;
        var distinctAyahIds = new HashSet<int>();
        if (!Enum.IsDefined(mode)
            || overrideAyahIds is null
            || overrideAyahIds.Any(ayahId => ayahId <= 0 || !distinctAyahIds.Add(ayahId)))
        {
            return false;
        }

        selection = new PhraseLinkingAyahSelection(mode, overrideAyahIds.ToArray());
        return true;
    }
}

public enum PhraseLinkingAyahSelectionMode : byte
{
    Only = 1,
    AllExcept = 2,
}
