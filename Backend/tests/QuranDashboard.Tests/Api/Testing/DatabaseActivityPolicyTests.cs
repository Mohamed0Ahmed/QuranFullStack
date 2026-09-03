using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using QuranDashboard.Api.Testing.DatabaseActivity;
using QuranDashboard.Infrastructure.Background;
using QuranDashboard.Infrastructure.Testing.DatabaseActivity;

namespace QuranDashboard.Tests.Api.Testing;

public sealed class DatabaseActivityPolicyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("readonly")]
    [InlineData("Unknown")]
    public void Testing_RequiresAnExactKnownProfile(string? profile)
    {
        var configuration = Configuration(profile);

        var resolve = () => TestingDatabaseActivityPolicyResolver.Resolve(
            configuration,
            new TestHostEnvironment("Testing"));

        resolve.Should().Throw<InvalidOperationException>()
            .WithMessage("*Testing:DatabaseActivity:Profile*");
    }

    [Fact]
    public void ReadOnly_RejectsBackgroundActivitySelections()
    {
        var configuration = Configuration(
            "ReadOnly",
            "LinkingPreparedPreflightProcessor");

        var resolve = () => TestingDatabaseActivityPolicyResolver.Resolve(
            configuration,
            new TestHostEnvironment("Testing"));

        resolve.Should().Throw<InvalidOperationException>()
            .WithMessage("*ReadOnly*background activity*");
    }

    [Theory]
    [InlineData("LinkingPreparedPreflightProcessor", typeof(LinkingPreparedPreflightProcessorService))]
    [InlineData("LinkingConfirmationJobProcessor", typeof(LinkingConfirmationJobProcessorService))]
    [InlineData("LinkingPreparedPreflightCleanup", typeof(LinkingPreparedPreflightCleanupService))]
    [InlineData("LinkingConfirmationJobCleanup", typeof(LinkingConfirmationJobCleanupService))]
    public void Mutable_RegistersOnlyExplicitlySelectedBackgroundActivity(
        string activity,
        Type expectedService)
    {
        var policy = TestingDatabaseActivityPolicyResolver.Resolve(
            Configuration("Mutable", activity),
            new TestHostEnvironment("Testing"));
        var services = new ServiceCollection();

        services.AddApplication();
        services.AddInfrastructure(DatabaseConfiguration(), policy);

        var hostedServices = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
            .Select(descriptor => descriptor.ImplementationType)
            .ToArray();
        hostedServices.Should().Equal(expectedService);
        policy.AllowPermissionCatalogueSynchronization.Should().BeFalse();
    }

    [Fact]
    public void DestructiveRehearsal_RejectsAnUnvalidatedScratchTarget()
    {
        var policy = TestingDatabaseActivityPolicyResolver.Resolve(
            Configuration("DestructiveRehearsal"),
            new TestHostEnvironment("Testing"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] =
                    "Host=localhost;Database=quran_test_scratch_run;Username=test;Password=test",
            })
            .Build();
        var services = new ServiceCollection();

        var register = () => services.AddInfrastructure(configuration, policy);

        register.Should().Throw<InvalidOperationException>()
            .WithMessage("*unvalidated database target*validated scratch or full Rehearsal Database*");
    }

    [Fact]
    public void DestructiveRehearsal_RejectsATargetThatDoesNotMatchItsValidationReceipt()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Testing:DatabaseActivity:Profile"] = "DestructiveRehearsal",
                ["Testing:DatabaseActivity:ValidatedRehearsalTarget:Kind"] = "scratch-empty",
                ["Testing:DatabaseActivity:ValidatedRehearsalTarget:Database"] = "quran_test_scratch_expected",
                ["Testing:DatabaseActivity:ValidatedRehearsalTarget:Subtype"] = "migration",
            })
            .Build();
        var policy = TestingDatabaseActivityPolicyResolver.Resolve(
            configuration,
            new TestHostEnvironment("Testing"));
        var services = new ServiceCollection();

        var register = () => services.AddInfrastructure(
            DatabaseConfiguration("quran_test_scratch_other"),
            policy);

        register.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not match the TestRuntime validation receipt*");
    }

    [Fact]
    public void ReadOnly_UsesReaderRoleAndReadOnlyTransactions()
    {
        var policy = TestingDatabaseActivityPolicyResolver.Resolve(
            Configuration("ReadOnly"),
            new TestHostEnvironment("Testing"));
        var services = new ServiceCollection();
        services.AddInfrastructure(DatabaseConfiguration(), policy);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var connectionString = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>()
            .Database.GetConnectionString();
        var connection = new NpgsqlConnectionStringBuilder(connectionString);

        connection.Options.Should().Contain("role=quran_dashboard_test_reader");
        connection.Options.Should().Contain("default_transaction_read_only=on");
        policy.AllowPermissionCatalogueSynchronization.Should().BeFalse();
    }

    [Fact]
    public void Production_KeepsTheExistingConnectionAndAllBackgroundServices()
    {
        var policy = TestingDatabaseActivityPolicyResolver.Resolve(
            new ConfigurationBuilder().Build(),
            new TestHostEnvironment("Production"));
        var configuration = DatabaseConfiguration();
        var services = new ServiceCollection();

        services.AddApplication();
        services.AddInfrastructure(configuration, policy);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>()
            .Database.GetConnectionString().Should().Be(configuration.GetConnectionString("QuranDashboardDb"));
        services.Count(descriptor => descriptor.ServiceType == typeof(IHostedService)).Should().Be(4);
        policy.AllowPermissionCatalogueSynchronization.Should().BeTrue();
    }

    private static IConfiguration Configuration(string? profile, params string[] activities)
    {
        var values = new Dictionary<string, string?>();
        if (profile is not null)
        {
            values["Testing:DatabaseActivity:Profile"] = profile;
        }

        for (var index = 0; index < activities.Length; index++)
        {
            values[$"Testing:DatabaseActivity:EnabledBackgroundActivities:{index}"] = activities[index];
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static IConfiguration DatabaseConfiguration(string database = "quran_dashboard_test") =>
        new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:QuranDashboardDb"] =
                $"Host=localhost;Database={database};Username=test;Password=test",
        })
        .Build();

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "QuranDashboard.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
