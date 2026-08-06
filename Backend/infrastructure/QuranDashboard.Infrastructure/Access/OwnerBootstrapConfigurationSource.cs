using Microsoft.Extensions.Options;
using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Infrastructure.Access;

public sealed class OwnerBootstrapConfigurationSource(
    IOptionsMonitor<OwnerBootstrapOptions> options) : IOwnerBootstrapConfigurationSource
{
    public OwnerBootstrapConfiguration GetCurrent()
    {
        var current = options.CurrentValue;
        return new OwnerBootstrapConfiguration(current.NormalizedEmails, current.ConfigurationFingerprint);
    }
}
