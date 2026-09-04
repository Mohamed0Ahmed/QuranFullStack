using QuranDashboard.TestRuntime;

namespace QuranDashboard.Tests.Quran.PhraseSearch;

[Collection(nameof(PhraseIndexFullCanonicalRehearsalCollection))]
public sealed class PhraseIndexFullCanonicalRehearsalTests
{
    [Fact]
    public void RunnerContext_BindsTheManualFullRehearsalAndNeverTheAuthoritativeDatabases()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            TestRuntimeCommand.FullRehearsalConnectionStringEnvironmentVariable);
        var runId = Environment.GetEnvironmentVariable("QURAN_DASHBOARD_TEST_RUN_ID");
        var command = Environment.GetEnvironmentVariable("QURAN_DASHBOARD_TEST_LOCK_COMMAND");
        var subtype = Environment.GetEnvironmentVariable("QURAN_DASHBOARD_TEST_FULL_REHEARSAL_SUBTYPE");

        connectionString.Should().NotBeNullOrWhiteSpace(
            "full-data PhraseSearch rehearsals require ConnectionStrings__QuranDashboardRehearsal from scripts/test");
        runId.Should().NotBeNullOrWhiteSpace();
        command.Should().Be("full-rehearsal");
        subtype.Should().Be("phrase-search-index-build");

        var database = new NpgsqlConnectionStringBuilder(connectionString).Database;
        database.Should().NotBe("quran_dashboard");
        database.Should().NotBe("quran_dashboard_test");
        database.Should().NotStartWith("quran_test_scratch_");
        database.Should().NotStartWith("quran_dashboard_test_refresh_");
    }
}

public sealed class PhraseIndexFullCanonicalRehearsalFixture;

[CollectionDefinition(nameof(PhraseIndexFullCanonicalRehearsalCollection), DisableParallelization = true)]
public sealed class PhraseIndexFullCanonicalRehearsalCollection
    : ICollectionFixture<PhraseIndexFullCanonicalRehearsalFixture>;
