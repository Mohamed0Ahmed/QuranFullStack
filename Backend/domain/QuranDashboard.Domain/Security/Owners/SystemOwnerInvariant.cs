namespace QuranDashboard.Domain.Security.Owners;

// The final-owner invariant (SC-005): a removal/disable is permitted only while at least one OTHER active
// owner survives it. Callers evaluate this under the serialized security-audit commit (barrier +
// AbwabRevisionState row locks), so concurrent removals are ordered and can never race the count to zero.
public static class SystemOwnerInvariant
{
    public static int ActiveOwnerCount(IEnumerable<SystemOwnerMembership> owners) =>
        owners.Count(owner => owner.IsActiveOwner);

    // A deactivation of `targetId` is allowed only if at least one other active owner remains afterwards.
    public static bool AllowsDeactivation(IEnumerable<SystemOwnerMembership> owners, Guid targetId) =>
        owners.Count(owner => owner.Id != targetId && owner.IsActiveOwner) >= 1;
}
