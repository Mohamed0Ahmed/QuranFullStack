namespace QuranDashboard.Application.Abstractions.Abwab;

public static class AbwabConflictCodes
{
    public const string TimelineGenerationStale = "abwab.timeline_generation_stale";

    public const string WriteBarrierClosed = "abwab.write_barrier_closed";

    public const string StabilizationActive = "abwab.stabilization_active";

    public const string PermissionAssignmentStale = "abwab.permission_assignment_stale";

    public const string PermissionBaselineLocked = "abwab.permission_baseline_locked";

    public const string LastSystemOwner = "abwab.last_system_owner";

    public const string SectionNameConflict = "abwab.section_name_conflict";

    public const string SectionNotEmpty = "abwab.section_not_empty";

    public const string PermanentDefaultSection = "abwab.permanent_default_section";

    public const string CategoryNameConflict = "abwab.category_name_conflict";

    public const string CategoryAliasConflict = "abwab.category_alias_conflict";

    public const string CategoryCycle = "abwab.category_cycle";

    public const string CategoryOverlappingMove = "abwab.category_overlapping_move";

    public const string CategoryUnavailable = "abwab.category_unavailable";

    public const string CategoryReservedByPending = "abwab.category_reserved_by_pending";

    public const string ManualProtection = "abwab.manual_protection";

    public const string ManualProtectionScopeConflict = "abwab.manual_protection_scope_conflict";

    public const string OrdinaryProtection = "abwab.ordinary_protection";

    public const string RowStale = "abwab.row_stale";

    public const string TreeRevisionStale = "abwab.tree_revision_stale";
}
