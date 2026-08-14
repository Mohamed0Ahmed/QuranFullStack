using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;

public sealed class LinkingPreparedPreflightLifecycleException(
    LinkingPreparedPreflightFailureCode failureCode,
    bool expired = false)
    : Exception("The prepared linking preflight lifecycle does not allow this operation.")
{
    public LinkingPreparedPreflightFailureCode FailureCode { get; } = failureCode;

    public bool Expired { get; } = expired;
}
