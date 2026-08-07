using System.Collections.Concurrent;

namespace QuranDashboard.Tests.Api.Access;

public sealed class FakeExternalUserProfileSource : IExternalUserProfileSource
{
    private readonly ConcurrentDictionary<string, int> _callsBySub = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _emailOverridesBySub = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, bool> _unavailableSubs = new(StringComparer.Ordinal);
    private ProfileBlock? _profileBlock;
    private int _totalCalls;
    private volatile string? _blankEmailSub;

    public static string EmailFor(string sub) => $"{sub}@example.test";

    public static string UserNameFor(string sub) => $"user-{sub}";

    public static string DisplayNameFor(string sub) => $"Display {sub}";

    public int TotalCalls => Volatile.Read(ref _totalCalls);

    public int CallsFor(string sub) => _callsBySub.TryGetValue(sub, out var count) ? count : 0;

    public void ReturnBlankEmailFor(string sub) => _blankEmailSub = sub;

    public void ReturnEmailFor(string sub, string email) => _emailOverridesBySub[sub] = email;

    public void ReturnUnavailableFor(string sub) => _unavailableSubs[sub] = true;

    public ProfileBlock BlockNextProfileFor(string sub)
    {
        var profileBlock = new ProfileBlock(sub);
        if (Interlocked.CompareExchange(ref _profileBlock, profileBlock, null) is not null)
        {
            throw new InvalidOperationException("A profile block is already active.");
        }

        return profileBlock;
    }

    public void Reset()
    {
        _callsBySub.Clear();
        _emailOverridesBySub.Clear();
        _unavailableSubs.Clear();
        Interlocked.Exchange(ref _profileBlock, null)?.Release();
        Interlocked.Exchange(ref _totalCalls, 0);
        _blankEmailSub = null;
    }

    public async Task<ExternalUserProfile> GetProfileAsync(string logtoSub, CancellationToken ct)
    {
        Interlocked.Increment(ref _totalCalls);
        _callsBySub.AddOrUpdate(logtoSub, 1, (_, existing) => existing + 1);

        if (_unavailableSubs.ContainsKey(logtoSub))
        {
            throw new HttpRequestException("Logto provider is unavailable.");
        }

        var profileBlock = Volatile.Read(ref _profileBlock);
        if (profileBlock is not null && string.Equals(profileBlock.Sub, logtoSub, StringComparison.Ordinal))
        {
            profileBlock.Enter();
            await profileBlock.WaitForReleaseAsync(ct);
            Interlocked.CompareExchange(ref _profileBlock, null, profileBlock);
        }

        var email = string.Equals(logtoSub, _blankEmailSub, StringComparison.Ordinal)
            ? null
            : _emailOverridesBySub.GetValueOrDefault(logtoSub, EmailFor(logtoSub));

        return new ExternalUserProfile(email, UserNameFor(logtoSub), DisplayNameFor(logtoSub));
    }

    public sealed class ProfileBlock(string sub)
    {
        private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string Sub { get; } = sub;

        public Task WaitUntilEnteredAsync() => entered.Task;

        public void Enter() => entered.TrySetResult();

        public void Release() => released.TrySetResult();

        public Task WaitForReleaseAsync(CancellationToken cancellationToken)
            => released.Task.WaitAsync(cancellationToken);
    }
}
