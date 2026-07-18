namespace QuranDashboard.Domain.Access;

// Values are pinned explicitly and stored as-is (see UserConfiguration): reordering or inserting a
// member must never shift an existing value and silently corrupt already-persisted rows.
public enum UserStatus
{
    Pending = 1,
    Active = 2,
    Disabled = 3
}
