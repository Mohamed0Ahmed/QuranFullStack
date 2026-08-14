using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Linking;

internal sealed class EfLinkingDataRevisionReader(ILinkingDataRevisionReadScope readScope)
    : ILinkingDataRevisionReader
{
    public Task<long> ReadAsync(CancellationToken cancellationToken) =>
        readScope.ExecuteAsync(
            1,
            static (revision, _) => Task.FromResult(revision),
            cancellationToken);
}
