using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Infrastructure.Persistence.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

internal sealed class EfLinkingWorkspaceReader(QuranDashboardDbContext db) : ILinkingWorkspaceReader
{
    public async Task<LinkingWorkspaceDto> LoadAsync(int userId, CancellationToken cancellationToken)
    {
        var workspace = await db.LinkingWorkspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(workspace => workspace.UserId == userId, cancellationToken);

        return workspace is null
            ? LinkingWorkspaceProjection.Empty
            : await LinkingWorkspaceProjection.ProjectAsync(db, workspace, cancellationToken);
    }
}
