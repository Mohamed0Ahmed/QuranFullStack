using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Security;

public sealed class LastSystemOwnerException()
    : Exception("The last active System Owner cannot be removed; at least one active owner must remain.")
{
    public string Code => AbwabConflictCodes.LastSystemOwner;
}
