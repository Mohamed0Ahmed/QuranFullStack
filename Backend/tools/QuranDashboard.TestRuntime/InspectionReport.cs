namespace QuranDashboard.TestRuntime;

internal sealed record ContractReport(
    int Version,
    int CapabilityMetadataVersion,
    int MappedTableCount,
    int SchemaTableCount,
    string? ExpectedMigrationHead);

internal sealed record TargetReport(
    string? Database,
    string? EndpointKind,
    string? ServerAddress,
    int? ServerPort,
    string? SessionUser,
    string? CurrentUser,
    string? PostgreSqlVersion,
    int? PostgreSqlMajorVersion,
    bool? InRecovery);

internal sealed record MigrationReport(
    bool HistoryTablePresent,
    string? ExpectedHead,
    string? DatabaseHead,
    int ExpectedCount,
    int AppliedCount,
    string State);

internal sealed record CatalogueReport(
    bool Available,
    bool Healthy,
    int RoleCount,
    int PermissionCount,
    IReadOnlyList<ContractViolation> Violations);

internal sealed record MarkerState(bool Present, bool? MatchesExpected);

internal sealed record MarkerReport(
    bool Healthy,
    IReadOnlyDictionary<string, MarkerState> States);

internal sealed record ExpectedRoleReport(bool Exists, bool SessionUserIsMember);

internal sealed record DataClassPrivilegeReport(
    int TableCount,
    bool CanSelectAll,
    bool CanInsertAny,
    bool CanUpdateAny,
    bool CanDeleteAny,
    bool CanTruncateAny);

internal sealed record PrivilegeReport(
    bool CanConnect,
    bool CanCreateDatabaseObjects,
    bool CanCreateTemporaryTables,
    bool CanUsePublicSchema,
    bool CanCreateInPublicSchema,
    IReadOnlyDictionary<string, ExpectedRoleReport> ExpectedRoles,
    IReadOnlyDictionary<string, DataClassPrivilegeReport> DataClasses);

internal sealed record TestRuntimeReport(
    string Command,
    bool Succeeded,
    ContractReport? Contract,
    TargetReport? Target,
    MigrationReport? Migration,
    CatalogueReport? Catalogue,
    MarkerReport? Markers,
    PrivilegeReport? Privileges,
    IReadOnlyList<ContractViolation> Violations,
    string? FailureType = null,
    CapabilityAdministrationReport? Administration = null,
    AdvisoryLockReport? AdvisoryLock = null,
    CapabilityRefreshReport? Refresh = null);

internal sealed record CapabilityRefreshReport(
    string Mode,
    bool Applied,
    bool Confirmed,
    string TargetDatabase,
    string? StagedDatabase,
    string? ReplacedDatabase,
    string Pipeline,
    IReadOnlyList<string> PlannedStages,
    IReadOnlyList<CapabilityRefreshStageReport> Stages,
    IReadOnlyList<CapabilityRefreshSessionReport> TargetSessions,
    string? CanonicalInputProvenance,
    string? CanonicalQuranFingerprint,
    string? SystemCatalogueFingerprint,
    string? SchemaStateFingerprint,
    string? ProtectedStateFingerprint,
    bool SwapRolledBack,
    bool ReplacedDatabaseRemoved,
    string? RefreshedAtUtc,
    string? Reason);

internal sealed record CapabilityRefreshStageReport(
    string Stage,
    string Status,
    long DurationMilliseconds = 0,
    string? FailureType = null);

internal sealed record CapabilityRefreshSessionReport(
    int ProcessId,
    string ApplicationName,
    string State);
