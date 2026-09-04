using QuranDashboard.Infrastructure.Testing.DatabaseActivity;
using QuranDashboard.Tests.Api.Access;

namespace QuranDashboard.Tests.Abwab;

public abstract class AbwabMutableWriterTest(
    AccessTestFixture fixture,
    bool enableLinkingProcessors = false) : IAsyncLifetime
{
    private static readonly DatabaseBackgroundActivity[] LinkingProcessors =
    [
        DatabaseBackgroundActivity.LinkingPreparedPreflightProcessor,
        DatabaseBackgroundActivity.LinkingConfirmationJobProcessor,
    ];

    protected AccessTestFixture Fixture { get; } = fixture;

    public Task InitializeAsync() => Fixture.BeginScenarioAsync(
        enableLinkingProcessors ? LinkingProcessors : []);

    public Task DisposeAsync() => Fixture.EndScenarioAsync();
}
