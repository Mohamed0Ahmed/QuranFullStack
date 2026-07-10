namespace QuranDashboard.Domain.Quran.Words.Morphology.Irab;

public enum I3rabStatus
{
    Approved,
    NeedsReview,
    Unsupported
}

public static class I3rabStatusMapping
{
    public const string Approved = "approved";
    public const string NeedsReview = "needs_review";
    public const string Unsupported = "unsupported";

    public static string ToStorageValue(I3rabStatus status) =>
        status switch
        {
            I3rabStatus.Approved => Approved,
            I3rabStatus.NeedsReview => NeedsReview,
            I3rabStatus.Unsupported => Unsupported,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
}
