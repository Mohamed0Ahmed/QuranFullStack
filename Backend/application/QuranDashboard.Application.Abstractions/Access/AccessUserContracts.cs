using QuranDashboard.Application.Abstractions.Common.Paging;

namespace QuranDashboard.Application.Abstractions.Access;

public sealed record AccessUserListQuery(
    string? Status,
    bool? IsOwner,
    string? Search,
    int Page,
    int PageSize);

public static class AccessUserPaging
{
    public const int MinimumPage = 1;

    public const int MaximumPage = int.MaxValue;

    public const int MinimumPageSize = 1;

    public const int DefaultPageSize = 25;

    public const int MaximumPageSize = 100;
}

public sealed record AccessUserSummary(
    int Id,
    string Email,
    string? DisplayName,
    string Status,
    bool IsOwner,
    int PermissionCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record AccessUserDetail(
    int Id,
    string Sub,
    string Email,
    string NormalizedEmail,
    string? UserName,
    string? DisplayName,
    string? Title,
    string Status,
    bool IsOwner,
    IReadOnlyList<string> PermissionCodes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record AccessUserPermissions(
    int UserId,
    string Status,
    bool IsOwner,
    uint Version,
    IReadOnlyList<string> PermissionCodes);

public sealed record AcceptAccessUserCommand(
    int UserId,
    uint ExpectedVersion,
    IReadOnlyList<string>? PermissionCodes,
    string Reason);

public sealed record DisableAccessUserCommand(int UserId, uint ExpectedVersion, string Reason);

public sealed record ReactivateAccessUserCommand(int UserId, uint ExpectedVersion, string Reason);

public sealed record ReplaceUserPermissionsCommand(
    int UserId,
    uint ExpectedVersion,
    IReadOnlyList<string> PermissionCodes,
    string Reason);

public enum AccessOperationFailure
{
    InvalidRequest,
    ActorNoLongerOwner,
    NotFound,
    TargetIsOwner,
    InvalidTransition,
    StaleVersion,
    InvalidPermissionCodes,
    SubjectAlreadyLinked,
    NormalizedEmailCollision,
    InvalidRelinkEvidence,
    OwnerConfigurationNotReconciled,
    ConfirmationRequired,
}

public sealed record AccessOperationResult<T>(T? Data, AccessOperationFailure? Failure)
{
    public bool IsSuccess => Failure is null;

    public static AccessOperationResult<T> Success(T data) => new(data, null);

    public static AccessOperationResult<T> Failed(AccessOperationFailure failure) => new(default, failure);
}

public interface IAccessUserReader
{
    Task<PagedResult<AccessUserSummary>> ListAsync(AccessUserListQuery query, CancellationToken cancellationToken);

    Task<AccessUserDetail?> GetAsync(int userId, CancellationToken cancellationToken);

    Task<AccessUserPermissions?> GetPermissionsAsync(int userId, CancellationToken cancellationToken);
}

public interface IAccessUserMutationService
{
    Task<AccessOperationResult<AccessUserDetail>> AcceptAsync(
        string actorSub,
        AcceptAccessUserCommand command,
        CancellationToken cancellationToken);

    Task<AccessOperationResult<AccessUserDetail>> DisableAsync(
        string actorSub,
        DisableAccessUserCommand command,
        CancellationToken cancellationToken);

    Task<AccessOperationResult<AccessUserDetail>> ReactivateAsync(
        string actorSub,
        ReactivateAccessUserCommand command,
        CancellationToken cancellationToken);

    Task<AccessOperationResult<AccessUserPermissions>> ReplacePermissionsAsync(
        string actorSub,
        ReplaceUserPermissionsCommand command,
        CancellationToken cancellationToken);
}
