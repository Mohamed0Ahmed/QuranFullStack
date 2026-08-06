using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Application.Access.Queries.GetUserPermissions;

public sealed class GetUserPermissionsHandler(IAccessUserReader reader)
{
    public async Task<AccessOperationResult<AccessUserPermissions>> HandleAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        if (userId < 1)
        {
            return AccessOperationResult<AccessUserPermissions>.Failed(AccessOperationFailure.InvalidRequest);
        }

        var permissions = await reader.GetPermissionsAsync(userId, cancellationToken);
        return permissions is null
            ? AccessOperationResult<AccessUserPermissions>.Failed(AccessOperationFailure.NotFound)
            : AccessOperationResult<AccessUserPermissions>.Success(permissions);
    }
}
