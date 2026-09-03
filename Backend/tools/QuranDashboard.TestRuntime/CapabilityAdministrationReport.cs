namespace QuranDashboard.TestRuntime;

internal sealed record CapabilityRoleReport(
    string Name,
    bool Exists,
    bool NoLogin,
    bool ExpectedAttributes,
    bool SelectedLoginIsOnlyMember,
    bool HasNoInheritedRoles,
    bool OwnsDevelopmentDatabase,
    bool OwnsTestDatabase,
    bool CanCreateInDevelopmentDatabase,
    bool CanMutateDevelopmentDatabase,
    bool PrivilegesMatch);

internal sealed record CapabilityAdministrationReport(
    string Mode,
    string SelectedLogin,
    bool Applied,
    bool Compliant,
    IReadOnlyList<string> PlannedOperations,
    IReadOnlyDictionary<string, CapabilityRoleReport> Roles,
    IReadOnlyDictionary<string, MarkerState> ManagedMarkers);
