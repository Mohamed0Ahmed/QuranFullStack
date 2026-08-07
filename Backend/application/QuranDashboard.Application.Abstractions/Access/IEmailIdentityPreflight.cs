namespace QuranDashboard.Application.Abstractions.Access;

public interface IEmailIdentityPreflight
{
    Task<EmailIdentityScanResult> ScanAsync(CancellationToken cancellationToken);

    Task<int> BackfillAsync(CancellationToken cancellationToken);
}

public sealed record EmailIdentityScanResult(
    int UserCount,
    IReadOnlyList<int> InvalidUserIds,
    IReadOnlyList<int> MissingNormalizedEmailUserIds,
    IReadOnlyList<int> MismatchedNormalizedEmailUserIds,
    IReadOnlyList<NormalizedEmailCollision> Collisions)
{
    public bool IsClean => InvalidUserIds.Count == 0
        && MissingNormalizedEmailUserIds.Count == 0
        && MismatchedNormalizedEmailUserIds.Count == 0
        && Collisions.Count == 0;
}

public sealed record NormalizedEmailCollision(
    string NormalizedEmail,
    IReadOnlyList<int> UserIds);
