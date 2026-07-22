using QuranDashboard.Domain.Security.Owners;

namespace QuranDashboard.Application.Abstractions.Security;

// Raised by the zero-to-one bootstrap for any of the four sanctioned rejections. The bootstrap is an
// operational path (no dashboard surface); the failure carries the exact reason for the operator.
public sealed class SystemOwnerBootstrapException(SystemOwnerBootstrapFailure failure)
    : Exception($"System Owner bootstrap rejected: {failure}.")
{
    public SystemOwnerBootstrapFailure Failure { get; } = failure;
}
