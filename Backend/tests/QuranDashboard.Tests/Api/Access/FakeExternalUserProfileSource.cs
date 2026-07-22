using System.Collections.Concurrent;

namespace QuranDashboard.Tests.Api.Access;

public sealed class FakeExternalUserProfileSource : IExternalUserProfileSource
{
    private readonly ConcurrentDictionary<string, int> _callsBySub = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _emailOverridesBySub = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, bool> _unverifiedSubs = new(StringComparer.Ordinal);
    private int _totalCalls;
    private volatile string? _blankEmailSub;

    public static string EmailFor(string sub) => $"{sub}@example.test";

    public static string UserNameFor(string sub) => $"user-{sub}";

    public static string DisplayNameFor(string sub) => $"Display {sub}";

    public int TotalCalls => Volatile.Read(ref _totalCalls);

    public int CallsFor(string sub) => _callsBySub.TryGetValue(sub, out var count) ? count : 0;

    public void ReturnBlankEmailFor(string sub) => _blankEmailSub = sub;

    public void ReturnEmailFor(string sub, string email) => _emailOverridesBySub[sub] = email;

    public void ReturnUnverifiedFor(string sub) => _unverifiedSubs[sub] = true;

    public void Reset()
    {
        _callsBySub.Clear();
        _emailOverridesBySub.Clear();
        _unverifiedSubs.Clear();
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

        var emailVerified = !string.IsNullOrWhiteSpace(email) && !_unverifiedSubs.ContainsKey(logtoSub);

        return Task.FromResult(
            new ExternalUserProfile(email, UserNameFor(logtoSub), DisplayNameFor(logtoSub), emailVerified));
    }
}
