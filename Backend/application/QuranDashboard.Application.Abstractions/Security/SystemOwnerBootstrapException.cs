using QuranDashboard.Domain.Security.Owners;

namespace QuranDashboard.Application.Abstractions.Security;

public sealed class SystemOwnerBootstrapException(SystemOwnerBootstrapFailure failure)
    : Exception($"System Owner bootstrap rejected: {failure}.")
{
    public SystemOwnerBootstrapFailure Failure { get; } = failure;
}
