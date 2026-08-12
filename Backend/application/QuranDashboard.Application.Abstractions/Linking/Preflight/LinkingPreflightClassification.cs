namespace QuranDashboard.Application.Abstractions.Linking.Preflight;

public enum LinkingPreflightClassification
{
    NewSource = 1,
    NewAyah = 2,
    OverlapOtherSource = 3,
    Unchanged = 4,
    Update = 5,
    Remove = 6,
    Invalid = 7,
}

public enum LinkingPreflightInvalidReason
{
    DoorArchived = 1,
    AyahOutsideSource = 2,
    WordIsAyahMarker = 3,
    WordOutsideAyah = 4,
}

public static class LinkingPreflightTokens
{
    private static readonly Dictionary<LinkingPreflightClassification, string> ClassificationToTokenMap = new()
    {
        [LinkingPreflightClassification.NewSource] = "NEW_SOURCE",
        [LinkingPreflightClassification.NewAyah] = "NEW_AYAH",
        [LinkingPreflightClassification.OverlapOtherSource] = "OVERLAP_OTHER_SOURCE",
        [LinkingPreflightClassification.Unchanged] = "UNCHANGED",
        [LinkingPreflightClassification.Update] = "UPDATE",
        [LinkingPreflightClassification.Remove] = "REMOVE",
        [LinkingPreflightClassification.Invalid] = "INVALID",
    };

    private static readonly Dictionary<LinkingPreflightInvalidReason, string> InvalidReasonToTokenMap = new()
    {
        [LinkingPreflightInvalidReason.DoorArchived] = "DOOR_ARCHIVED",
        [LinkingPreflightInvalidReason.AyahOutsideSource] = "AYAH_OUTSIDE_SOURCE",
        [LinkingPreflightInvalidReason.WordIsAyahMarker] = "WORD_IS_AYAH_MARKER",
        [LinkingPreflightInvalidReason.WordOutsideAyah] = "WORD_OUTSIDE_AYAH",
    };

    public static string ToToken(LinkingPreflightClassification classification) =>
        ClassificationToTokenMap.TryGetValue(classification, out var token)
            ? token
            : throw new ArgumentOutOfRangeException(
                nameof(classification), classification, "Unknown linking preflight classification.");

    public static string? ToToken(LinkingPreflightInvalidReason? reason) =>
        reason is null
            ? null
            : InvalidReasonToTokenMap.TryGetValue(reason.Value, out var token)
                ? token
                : throw new ArgumentOutOfRangeException(
                    nameof(reason), reason, "Unknown linking preflight invalid reason.");
}
