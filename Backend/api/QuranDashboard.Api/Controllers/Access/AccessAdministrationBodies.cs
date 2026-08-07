namespace QuranDashboard.Api.Controllers.Access;

public sealed record AcceptAccessUserBody(
    uint ExpectedVersion,
    IReadOnlyList<string>? PermissionCodes,
    string Reason);

public sealed record DisableAccessUserBody(uint ExpectedVersion, string Reason);

public sealed record ReactivateAccessUserBody(uint ExpectedVersion, string Reason);

public sealed record ReplaceUserPermissionsBody(
    uint ExpectedVersion,
    IReadOnlyList<string>? PermissionCodes,
    string Reason);

public sealed record PreviewLogtoSubjectRelinkBody(string NewSub, string EvidenceToken);

public sealed record ConfirmLogtoSubjectRelinkBody(
    uint ExpectedVersion,
    string OldSub,
    string NewSub,
    string Reason,
    bool Confirmed,
    string EvidenceToken);
