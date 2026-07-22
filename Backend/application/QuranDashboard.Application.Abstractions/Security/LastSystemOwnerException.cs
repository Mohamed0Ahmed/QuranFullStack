using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Security;

// Raised when an owner removal/disable would leave zero active enabled System Owners. The final-owner
// invariant fails closed under the serialized commit; mapped to 409 / abwab.last_system_owner.
public sealed class LastSystemOwnerException()
    : Exception("The last active System Owner cannot be removed; at least one active owner must remain.")
{
    public string Code => AbwabConflictCodes.LastSystemOwner;
}
