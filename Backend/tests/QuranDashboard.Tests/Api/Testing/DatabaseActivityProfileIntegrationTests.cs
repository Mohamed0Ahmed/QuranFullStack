using Microsoft.Extensions.Hosting;
using QuranDashboard.Infrastructure.Background;

namespace QuranDashboard.Tests.Api.Testing;

[Collection(nameof(DatabaseActivityProfileCollection))]
public sealed class DatabaseActivityProfileIntegrationTests(DatabaseActivityProfileFixture fixture)
{
    private static readonly Type[] LinkingHostedServiceTypes =
    [
        typeof(LinkingPreparedPreflightProcessorService),
        typeof(LinkingPreparedPreflightCleanupService),
        typeof(LinkingConfirmationJobProcessorService),
        typeof(LinkingConfirmationJobCleanupService),
    ];

    [Fact]
    public async Task ReadOnly_UsesReaderRoleAndRejectsDatabaseWritesWithoutStartupWriters()
    {
        (await fixture.CountPermissionsAsync()).Should().Be(0);
        await using var factory = fixture.BuildFactory("ReadOnly");

        var session = await ReadSessionAsync(factory.Services);

        session.CurrentUser.Should().Be("quran_dashboard_test_reader");
        session.DefaultTransactionReadOnly.Should().Be("on");
        session.TransactionReadOnly.Should().Be("on");
        GetLinkingHostedServiceTypes(factory.Services).Should().BeEmpty();
        (await fixture.CountPermissionsAsync()).Should().Be(0);

        var write = async () => await ExecuteThroughApiServicesAsync(
            factory.Services,
            "UPDATE public.users SET id = id WHERE false");
        await write.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == PostgresErrorCodes.ReadOnlySqlTransaction
                || exception.SqlState == PostgresErrorCodes.InsufficientPrivilege);
    }

    [Fact]
    public async Task Mutable_UsesApplicationRoleAndEnablesOnlyTheSelectedProcessor()
    {
        (await fixture.CountPermissionsAsync()).Should().Be(0);
        fixture.CommandCapture.Reset();
        await using var factory = fixture.BuildFactory(
            "Mutable",
            "LinkingConfirmationJobProcessor");

        var session = await ReadSessionAsync(factory.Services);

        session.CurrentUser.Should().Be("quran_dashboard_test_application");
        session.DefaultTransactionReadOnly.Should().Be("off");
        session.TransactionReadOnly.Should().Be("off");
        GetLinkingHostedServiceTypes(factory.Services)
            .Should().Equal(typeof(LinkingConfirmationJobProcessorService));
        await WaitForDatabaseCommandAsync("FROM linking_confirmation_jobs");
        fixture.CommandCapture.CommandTexts.Should().NotContain(command =>
            command.Contains("FROM linking_prepared_preflights", StringComparison.Ordinal));
        (await fixture.CountPermissionsAsync()).Should().Be(0);
        await ExecuteThroughApiServicesAsync(
            factory.Services,
            "UPDATE public.users SET id = id WHERE false");
    }

    [Fact]
    public async Task Mutable_PooledConnectionsRestoreTheApplicationRoleAndWritableTransactions()
    {
        await using var factory = fixture.BuildFactory("Mutable");
        GetLinkingHostedServiceTypes(factory.Services).Should().BeEmpty();
        await ExecuteThroughApiServicesAsync(
            factory.Services,
            "RESET ROLE; SET default_transaction_read_only = on");

        var session = await ReadSessionAsync(factory.Services);

        session.CurrentUser.Should().Be("quran_dashboard_test_application");
        session.DefaultTransactionReadOnly.Should().Be("off");
        session.TransactionReadOnly.Should().Be("off");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Unknown")]
    public void TestingHost_MissingOrUnknownProfileFailsStartup(string? profile)
    {
        using var factory = fixture.BuildFactory(profile);

        var start = () => _ = factory.Services;

        start.Should().Throw<InvalidOperationException>()
            .WithMessage("*Testing:DatabaseActivity:Profile*");
    }

    [Fact]
    public void DestructiveRehearsal_RefusesThePersistentTestDatabase()
    {
        using var factory = fixture.BuildFactory("DestructiveRehearsal");

        var start = () => _ = factory.Services;

        start.Should().Throw<InvalidOperationException>()
            .WithMessage("*persistent Test Database*quran_dashboard_test*");
    }

    [Fact]
    public async Task DestructiveRehearsal_AcceptsAValidatedScratchTargetWithWritableTransactions()
    {
        await using var factory = fixture.BuildValidatedScratchFactory();

        var session = await ReadSessionAsync(factory.Services);

        session.CurrentUser.Should().Be(fixture.Login);
        session.DefaultTransactionReadOnly.Should().Be("off");
        session.TransactionReadOnly.Should().Be("off");
        GetLinkingHostedServiceTypes(factory.Services).Should().BeEmpty();
        await ExecuteThroughApiServicesAsync(
            factory.Services,
            "UPDATE public.users SET id = id WHERE false");
    }

    [Fact]
    public async Task DestructiveRehearsal_AcceptsAValidatedFullTargetWithWritableTransactions()
    {
        await using var factory = fixture.BuildValidatedFullRehearsalFactory();

        var session = await ReadSessionAsync(factory.Services);

        session.CurrentUser.Should().Be(fixture.Login);
        session.DefaultTransactionReadOnly.Should().Be("off");
        session.TransactionReadOnly.Should().Be("off");
        GetLinkingHostedServiceTypes(factory.Services).Should().BeEmpty();
        await ExecuteThroughApiServicesAsync(
            factory.Services,
            "UPDATE public.users SET id = id WHERE false");
    }

    [Fact]
    public void DestructiveRehearsal_RejectsSessionOptionsThatSpoofDatabaseMarkers()
    {
        using var factory = fixture.BuildSpoofedUnmarkedScratchFactory();

        var start = () => _ = factory.Services;

        start.Should().Throw<InvalidOperationException>()
            .WithMessage("*TestRuntime rehearsal markers*");
    }

    private static async Task<DatabaseSession> ReadSessionAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            "SELECT current_user, current_setting('default_transaction_read_only'), "
            + "current_setting('transaction_read_only')";
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return new DatabaseSession(reader.GetString(0), reader.GetString(1), reader.GetString(2));
    }

    private static IEnumerable<Type> GetLinkingHostedServiceTypes(IServiceProvider services) =>
        services.GetServices<IHostedService>()
            .Select(service => service.GetType())
            .Where(LinkingHostedServiceTypes.Contains);

    private static async Task ExecuteThroughApiServicesAsync(IServiceProvider services, string sql)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task WaitForDatabaseCommandAsync(string expectedSql)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!fixture.CommandCapture.CommandTexts.Any(command =>
                   command.Contains(expectedSql, StringComparison.Ordinal)))
        {
            await Task.Delay(25, timeout.Token);
        }
    }

    private sealed record DatabaseSession(
        string CurrentUser,
        string DefaultTransactionReadOnly,
        string TransactionReadOnly);
}
