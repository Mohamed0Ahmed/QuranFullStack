using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Security.Owners;
using QuranDashboard.Domain.Security.Owners;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Ownership;

[Collection(nameof(AbwabDbCollection))]
public sealed class BootstrapTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Issuer = "https://logto.test";
    private const string Subject = "founder";

    public Task InitializeAsync() => SecurityTestHarness.ResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Bootstrap_CreatesTheFirstOwner_AndIsPermanentlyAudited()
    {
        var result = await SecurityTestHarness.BootstrapOwnerAsync(fixture, Command());

        result.Audited.Should().BeTrue();
        (await SecurityTestHarness.ActiveOwnerCountAsync(fixture)).Should().Be(1);
        (await SecurityTestHarness.ReadSecurityEventCountAsync(fixture)).Should().Be(1);
    }

    [Fact]
    public async Task Bootstrap_IsIdempotent_ForTheSameIdentity_WithNoExtraAudit()
    {
        await SecurityTestHarness.BootstrapOwnerAsync(fixture, Command());
        var second = await SecurityTestHarness.BootstrapOwnerAsync(fixture, Command());

        second.Audited.Should().BeFalse("re-running with the same identity is a no-op");
        (await SecurityTestHarness.ActiveOwnerCountAsync(fixture)).Should().Be(1);
        (await SecurityTestHarness.ReadSecurityEventCountAsync(fixture)).Should().Be(1);
    }

    [Fact]
    public async Task Bootstrap_ConcurrentAttempts_MintExactlyOneOwner()
    {
        var first = SecurityTestHarness.BootstrapOwnerAsync(fixture, Command());
        var second = SecurityTestHarness.BootstrapOwnerAsync(fixture, Command());
        await Task.WhenAll(first, second);

        (await SecurityTestHarness.ActiveOwnerCountAsync(fixture)).Should().Be(1);
        (await SecurityTestHarness.ReadSecurityEventCountAsync(fixture)).Should().Be(1);
    }

    [Fact]
    public async Task Bootstrap_WrongIssuer_IsRejected()
    {
        await AssertRejectsAsync(Command() with { Issuer = "https://attacker.test" }, SystemOwnerBootstrapFailure.WrongIssuer);
    }

    [Fact]
    public async Task Bootstrap_UnverifiedEmail_IsRejected()
    {
        await AssertRejectsAsync(Command() with { EmailVerified = false }, SystemOwnerBootstrapFailure.UnverifiedEmail);
    }

    [Fact]
    public async Task Bootstrap_DisabledAccount_IsRejected()
    {
        await AssertRejectsAsync(Command() with { AccountEnabled = false }, SystemOwnerBootstrapFailure.DisabledAccount);
    }

    [Fact]
    public async Task Bootstrap_DuplicateMismatchedIdentity_IsRejected()
    {
        await SecurityTestHarness.BootstrapOwnerAsync(fixture, Command());

        await AssertRejectsAsync(Command() with { Subject = "someone-else" }, SystemOwnerBootstrapFailure.DuplicateMismatch);
        (await SecurityTestHarness.ActiveOwnerCountAsync(fixture)).Should().Be(1);
    }

    private async Task AssertRejectsAsync(BootstrapSystemOwnerCommand command, SystemOwnerBootstrapFailure expected)
    {
        var act = () => SecurityTestHarness.BootstrapOwnerAsync(fixture, command);
        (await act.Should().ThrowAsync<SystemOwnerBootstrapException>()).Which.Failure.Should().Be(expected);
    }

    private static BootstrapSystemOwnerCommand Command() => new(
        Issuer, Subject, Issuer, EmailVerified: true, AccountEnabled: true, ExpectedTimelineGeneration.Of(0), "actor");
}
