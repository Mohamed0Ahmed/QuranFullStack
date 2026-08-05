namespace QuranDashboard.Domain.Access;

public enum AccessAuditActionType
{
    UserAccepted = 1,
    UserActivated = 2,
    UserDisabled = 3,
    UserReactivated = 4,
    PermissionGranted = 5,
    PermissionRevoked = 6,
    LogtoSubjectRelinked = 7,
    OwnerGrantedByReconciliation = 8,
    OwnerRemovedByReconciliation = 9,
}
