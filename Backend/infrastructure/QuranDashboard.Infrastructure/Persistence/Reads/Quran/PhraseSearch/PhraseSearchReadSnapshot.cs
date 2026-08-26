using Microsoft.EntityFrameworkCore.Storage;
using QuranDashboard.Domain.Quran.PhraseSearch;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

internal sealed class PhraseSearchReadSnapshot : IAsyncDisposable
{
    private readonly IDbContextTransaction transaction;
    private bool completed;

    private PhraseSearchReadSnapshot(
        IDbContextTransaction transaction,
        Guid activeBuildId,
        bool exactReady,
        bool similarityReady)
    {
        this.transaction = transaction;
        ActiveBuildId = activeBuildId;
        ExactReady = exactReady;
        SimilarityReady = similarityReady;
    }

    internal Guid ActiveBuildId { get; }
    internal bool ExactReady { get; }
    internal bool SimilarityReady { get; }

    internal static async Task<PhraseSearchReadSnapshot?> OpenAsync(
        QuranDashboardDbContext db,
        CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken);

        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "SET TRANSACTION READ ONLY",
                cancellationToken);

            var state = await db.QuranPhraseIndexStates
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == PhraseIndexState.SingletonId,
                    cancellationToken);

            if (state is null
                || state.IsStale
                || state.SourceFingerprint is null
                || state.ActiveBuildId is null)
            {
                await transaction.CommitAsync(cancellationToken);
                await transaction.DisposeAsync();
                return null;
            }

            var build = await db.QuranPhraseIndexBuilds
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == state.ActiveBuildId.Value,
                    cancellationToken);

            if (build is null
                || build.Status != PhraseIndexBuildStatus.Active
                || build.FormatVersion != PhraseIndexBuildConstants.FormatVersion
                || !build.ExactReady
                || !build.SimilarityReady
                || build.SourceRevision != state.SourceRevision
                || !string.Equals(
                    build.SourceFingerprint,
                    state.SourceFingerprint,
                    StringComparison.Ordinal))
            {
                await transaction.CommitAsync(cancellationToken);
                await transaction.DisposeAsync();
                return null;
            }

            return new PhraseSearchReadSnapshot(
                transaction,
                build.Id,
                build.ExactReady,
                build.SimilarityReady);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    internal async Task CompleteAsync(CancellationToken cancellationToken)
    {
        await transaction.CommitAsync(cancellationToken);
        completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!completed)
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }

        await transaction.DisposeAsync();
    }
}
