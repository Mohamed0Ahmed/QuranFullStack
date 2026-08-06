using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Application.Access.Queries.GetAccessUser;

public sealed class GetAccessUserHandler(IAccessUserReader reader)
{
    public async Task<AccessOperationResult<AccessUserDetail>> HandleAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        if (userId < 1)
        {
            return AccessOperationResult<AccessUserDetail>.Failed(AccessOperationFailure.InvalidRequest);
        }

        var user = await reader.GetAsync(userId, cancellationToken);
        return user is null
            ? AccessOperationResult<AccessUserDetail>.Failed(AccessOperationFailure.NotFound)
            : AccessOperationResult<AccessUserDetail>.Success(user);
    }
}
