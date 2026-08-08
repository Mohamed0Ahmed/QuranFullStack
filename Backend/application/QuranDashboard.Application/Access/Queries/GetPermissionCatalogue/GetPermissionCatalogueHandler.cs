using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Application.Access.Queries.GetPermissionCatalogue;

public sealed class GetPermissionCatalogueHandler(IPermissionCatalogueReader reader)
{
    public Task<PermissionCatalogueResponse> HandleAsync(CancellationToken cancellationToken) =>
        reader.GetActiveAsync(cancellationToken);
}
