using QuranDashboard.Domain.Abwab.Notifications;
using QuranDashboard.Domain.Abwab.Persistence;
using QuranDashboard.Infrastructure.Abwab.Notifications;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Notifications._Support;

namespace QuranDashboard.Tests.Abwab.Notifications;

[Collection(nameof(AbwabDbCollection))]
public sealed class DedupAndReadStateTests
{
    private readonly PostgresFixture _fixture;

    public DedupAndReadStateTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SecondWriteWithSameSourceIdentity_IsDeduplicated()
    {
        await NotificationTestHarness.ResetAsync(_fixture);

        Guid firstId;
        await using (var db = NotificationTestHarness.CreateContext(_fixture))
        {
            firstId = (await NotificationTestHarness.Writer(db)
                .WriteAsync(NotificationTestHarness.Request("dup-source", payload: "first"), CancellationToken.None))
                .NotificationId;
        }

        NotificationWriteResult second;
        await using (var db = NotificationTestHarness.CreateContext(_fixture))
        {
            second = await NotificationTestHarness.Writer(db)
                .WriteAsync(NotificationTestHarness.Request("dup-source", payload: "second"), CancellationToken.None);
        }

        second.Outcome.Should().Be(NotificationWriteOutcome.DuplicateIgnored);
        second.NotificationId.Should().Be(firstId, "dedup returns the existing notification, not a new one");
        (await NotificationTestHarness.CountRecordsAsync(_fixture)).Should().Be(1);
    }

    [Fact]
    public async Task UniqueSourceIdentityIndex_RejectsARawDuplicateInsert()
    {
        await NotificationTestHarness.ResetAsync(_fixture);

        await using (var db = NotificationTestHarness.CreateContext(_fixture))
        {
            await NotificationTestHarness.Writer(db)
                .WriteAsync(NotificationTestHarness.Request("index-source"), CancellationToken.None);
        }

        var rawDuplicate = async () => await NotificationTestHarness.ExecuteRawAsync(
            _fixture,
            "INSERT INTO abwab_notification_records (id, recipient_subject, source_identity, payload, created_at) "
            + "VALUES (gen_random_uuid(), 'recipient-1', 'index-source', 'payload', now())");

        var exception = await rawDuplicate.Should().ThrowAsync<PostgresException>();
        exception.Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        (await NotificationTestHarness.CountRecordsAsync(_fixture)).Should().Be(1);
    }

    [Fact]
    public void NotificationEntities_AreNotPartOfTheProductAuditEnvelope()
    {
        typeof(IAbwabAuditable).IsAssignableFrom(typeof(NotificationRecord)).Should().BeFalse();
        typeof(IAbwabAuditable).IsAssignableFrom(typeof(NotificationReadState)).Should().BeFalse();
    }

    [Fact]
    public async Task TogglingReadState_ProducesNoAuditAndDoesNotAdvanceTheHead()
    {
        await NotificationTestHarness.ResetAsync(_fixture);

        Guid notificationId;
        await using (var db = NotificationTestHarness.CreateContext(_fixture))
        {
            notificationId = (await NotificationTestHarness.Writer(db)
                .WriteAsync(NotificationTestHarness.Request("read-state-source"), CancellationToken.None))
                .NotificationId;
        }

        await using (var db = NotificationTestHarness.CreateContext(_fixture))
        {
            var repository = NotificationTestHarness.ReadStates(db);
            var read = await repository.MarkReadAsync(notificationId, "recipient-1", CancellationToken.None);
            read.IsRead.Should().BeTrue();
            read.ReadAtUtc.Should().NotBeNull();

            var unread = await repository.MarkUnreadAsync(notificationId, "recipient-1", CancellationToken.None);
            unread.IsRead.Should().BeFalse("read state is a plain mutable toggle, not append-only");
            unread.ReadAtUtc.Should().BeNull();
        }

        (await NotificationTestHarness.CountReadStatesAsync(_fixture)).Should().Be(1);
        (await NotificationTestHarness.CountChangeSetsAsync(_fixture)).Should().Be(0);
        (await NotificationTestHarness.CountAuditEventsAsync(_fixture)).Should().Be(0);
        (await NotificationTestHarness.CountSecurityAuditEventsAsync(_fixture)).Should().Be(0);
        (await NotificationTestHarness.ReadAuditHeadAsync(_fixture)).Should().Be(0);
    }

    [Fact]
    public async Task ReadStateTable_IsMutable_UnlikeTheAppendOnlyAuditTables()
    {
        await NotificationTestHarness.ResetAsync(_fixture);

        Guid notificationId;
        await using (var db = NotificationTestHarness.CreateContext(_fixture))
        {
            notificationId = (await NotificationTestHarness.Writer(db)
                .WriteAsync(NotificationTestHarness.Request("mutable-source"), CancellationToken.None))
                .NotificationId;
            await NotificationTestHarness.ReadStates(db).MarkReadAsync(notificationId, "recipient-1", CancellationToken.None);
        }

        var mutate = async () =>
        {
            await NotificationTestHarness.ExecuteRawAsync(
                _fixture, "UPDATE abwab_notification_read_states SET is_read = true");
            await NotificationTestHarness.ExecuteRawAsync(
                _fixture, "DELETE FROM abwab_notification_read_states");
        };

        await mutate.Should().NotThrowAsync();
        (await NotificationTestHarness.CountReadStatesAsync(_fixture)).Should().Be(0);
    }
}
