using QuranDashboard.Domain.Abwab.Notifications;
using QuranDashboard.Infrastructure.Abwab.Notifications;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Notifications._Support;

namespace QuranDashboard.Tests.Abwab.Notifications;

// T065 / FR-033 / SC-009: the notification storage writer JOINS a caller's domain transaction — it writes
// through the caller's DbContext and never opens, commits, or rolls back a transaction of its own. So a
// rolled-back caller leaves NO notification row (a notification can never commit for a rolled-back action),
// while a committed caller persists exactly one row. Proven against real PostgreSQL.
[Collection(nameof(AbwabDbCollection))]
public sealed class TransactionJoinTests
{
    private readonly PostgresFixture _fixture;

    public TransactionJoinTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task WriteAsync_WhenCallerRollsBack_LeavesNoNotificationRow()
    {
        await NotificationTestHarness.ResetAsync(_fixture);

        await using (var db = NotificationTestHarness.CreateContext(_fixture))
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            var outcome = await NotificationTestHarness.Writer(db)
                .WriteAsync(NotificationTestHarness.Request("source-rollback"), CancellationToken.None);

            outcome.Outcome.Should().Be(NotificationWriteOutcome.Stored);

            // The caller — not the writer — owns the transaction boundary. Rolling back must undo the row.
            await transaction.RollbackAsync();
        }

        var rows = await NotificationTestHarness.CountRecordsAsync(_fixture);
        rows.Should().Be(0, "a notification written inside a rolled-back caller transaction must not survive");
    }

    [Fact]
    public async Task WriteAsync_WhenCallerCommits_PersistsExactlyOneRow()
    {
        await NotificationTestHarness.ResetAsync(_fixture);

        NotificationWriteResult outcome;
        await using (var db = NotificationTestHarness.CreateContext(_fixture))
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            outcome = await NotificationTestHarness.Writer(db)
                .WriteAsync(NotificationTestHarness.Request("source-commit", payload: "hello"), CancellationToken.None);

            await transaction.CommitAsync();
        }

        outcome.Outcome.Should().Be(NotificationWriteOutcome.Stored);
        (await NotificationTestHarness.CountRecordsAsync(_fixture)).Should().Be(1);

        await using var verify = NotificationTestHarness.CreateContext(_fixture);
        var stored = await verify.Set<NotificationRecord>()
            .AsNoTracking()
            .SingleAsync();
        stored.Id.Should().Be(outcome.NotificationId);
        stored.SourceIdentity.Should().Be("source-commit");
        stored.Payload.Should().Be("hello");
    }

    [Fact]
    public async Task WriteAsync_WithNoAmbientTransaction_PersistsThroughTheImplicitUnitOfWork()
    {
        await NotificationTestHarness.ResetAsync(_fixture);

        await using (var db = NotificationTestHarness.CreateContext(_fixture))
        {
            await NotificationTestHarness.Writer(db)
                .WriteAsync(NotificationTestHarness.Request("source-standalone"), CancellationToken.None);
        }

        // No caller-owned transaction: SaveChanges' own implicit unit of work commits the row. This proves the
        // writer joins WHATEVER the caller provides rather than forcing its own transaction.
        (await NotificationTestHarness.CountRecordsAsync(_fixture)).Should().Be(1);
    }
}
