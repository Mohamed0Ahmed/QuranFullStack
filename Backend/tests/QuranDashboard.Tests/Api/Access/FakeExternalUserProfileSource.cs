using System.Collections.Concurrent;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Tests.Api.Access;

/// <summary>
/// In-memory stand-in for the ONLY external boundary in the provisioning flow: the Logto Management API
/// that supplies a subject's server-verified profile. Everything else in the tests runs for real (the
/// pipeline, EF Core, Postgres). Profiles are deterministic per <c>sub</c> so a test can assert the
/// persisted email came from this trusted source rather than anything the caller sent. Call counts are
/// tracked so idempotency can be verified, and a single designated subject can be made to return a blank
/// email to exercise the provisioning failure path.
/// </summary>
public sealed class FakeExternalUserProfileSource : IExternalUserProfileSource
{
    private readonly ConcurrentDictionary<string, int> _callsBySub = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _emailOverridesBySub = new(StringComparer.Ordinal);
    private int _totalCalls;
    private volatile string? _blankEmailSub;

    /// <summary>The deterministic, server-verified email this fake reports for <paramref name="sub"/>.</summary>
    public static string EmailFor(string sub) => $"{sub}@example.test";

    /// <summary>The deterministic username this fake reports for <paramref name="sub"/>.</summary>
    public static string UserNameFor(string sub) => $"user-{sub}";

    /// <summary>The deterministic display name this fake reports for <paramref name="sub"/>.</summary>
    public static string DisplayNameFor(string sub) => $"Display {sub}";

    /// <summary>Total number of profile lookups across all subjects since the last <see cref="Reset"/>.</summary>
    public int TotalCalls => Volatile.Read(ref _totalCalls);

    /// <summary>Number of profile lookups for a specific subject since the last <see cref="Reset"/>.</summary>
    public int CallsFor(string sub) => _callsBySub.TryGetValue(sub, out var count) ? count : 0;

    /// <summary>Designates one subject whose profile carries a blank email (Logto held no primary email).</summary>
    public void ReturnBlankEmailFor(string sub) => _blankEmailSub = sub;

    /// <summary>
    /// Overrides the profile email reported for one subject — e.g. to simulate a subject deleted and
    /// recreated in the identity provider, which presents a new <c>sub</c> whose server-verified email
    /// collides with an existing, different local user's email.
    /// </summary>
    public void ReturnEmailFor(string sub, string email) => _emailOverridesBySub[sub] = email;

    /// <summary>Clears call counters and any blank-email/email-override designation for per-test isolation.</summary>
    public void Reset()
    {
        _callsBySub.Clear();
        _emailOverridesBySub.Clear();
        Interlocked.Exchange(ref _totalCalls, 0);
        _blankEmailSub = null;
    }

    public Task<ExternalUserProfile> GetProfileAsync(string logtoSub, CancellationToken ct)
    {
        Interlocked.Increment(ref _totalCalls);
        _callsBySub.AddOrUpdate(logtoSub, 1, (_, existing) => existing + 1);

        var email = string.Equals(logtoSub, _blankEmailSub, StringComparison.Ordinal)
            ? null
            : _emailOverridesBySub.GetValueOrDefault(logtoSub, EmailFor(logtoSub));

        return Task.FromResult(new ExternalUserProfile(email, UserNameFor(logtoSub), DisplayNameFor(logtoSub)));
    }
}
