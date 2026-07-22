namespace QuranDashboard.DataImporter.Import.Safety;

public enum SourceIdentityStatus
{
    Accepted,
    MissingManifest,
    Forbidden,
    WrongIdentity,
    UnstableIds
}

public sealed record SourceIdentityResult(SourceIdentityStatus Status, string Message)
{
    public bool Accepted => Status == SourceIdentityStatus.Accepted;

    public static SourceIdentityResult Accept(string message) =>
        new(SourceIdentityStatus.Accepted, message);

    public static SourceIdentityResult Reject(SourceIdentityStatus status, string message) =>
        new(status, message);
}
