namespace QuranDashboard.Domain.Security.Owners;

public static class SystemOwnerInvariant
{
    public static int ActiveOwnerCount(IEnumerable<SystemOwnerMembership> owners) =>
        owners.Count(owner => owner.IsActiveOwner);

    public static bool AllowsDeactivation(IEnumerable<SystemOwnerMembership> owners, Guid targetId) =>
        owners.Count(owner => owner.Id != targetId && owner.IsActiveOwner) >= 1;
}
