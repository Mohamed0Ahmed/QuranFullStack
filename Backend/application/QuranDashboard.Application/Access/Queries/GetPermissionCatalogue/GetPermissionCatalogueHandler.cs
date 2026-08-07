using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Application.Access.Queries.GetPermissionCatalogue;

public sealed class GetPermissionCatalogueHandler(IPermissionCatalogueReader reader)
{
    public Task<IReadOnlyList<PermissionCatalogueItem>> HandleAsync(CancellationToken cancellationToken) =>
        reader.GetActiveAsync(cancellationToken);
}
