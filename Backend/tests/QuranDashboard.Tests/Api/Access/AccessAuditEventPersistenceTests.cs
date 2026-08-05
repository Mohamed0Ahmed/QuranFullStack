using QuranDashboard.Domain.Access;
using QuranDashboard.Application.Abstractions.Security.Permissions;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class AccessAuditEventPersistenceTests(AccessTestFixture fixture)
{
    [Fact]
    public async Task AuditEvent_CanBeAppendedAndReadWithImmutableSnapshots()
    {
        await fixture.ResetAsync();
        var actorId = await fixture.InsertPersonaAsync("Owner");
        var targetId = await fixture.InsertPersonaAsync("ReadOnly");

        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            db.AccessAuditEvents.Add(new AccessAuditEvent(
                DateTimeOffset.UtcNow,
                AccessAuditActionType.PermissionGranted,
                AccessAuditActorType.User,
                actorId,
                targetId,
                "{\"sub\":\"actor\"}",
                "{\"sub\":\"target\"}",
                AbwabPermissionCatalogue.All[0].Code,
                "{}",
                "{\"permission\":\"abwab.doors.create\"}",
                "test",
                new AccessAuditMetadata(1)));
            await db.SaveChangesAsync();
        }

        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            var audit = await db.AccessAuditEvents.SingleAsync();
            JsonDocument.Parse(audit.ActorSnapshotJson).RootElement.GetProperty("sub").GetString()
                .Should().Be("actor");
            JsonDocument.Parse(audit.TargetSnapshotJson).RootElement.GetProperty("sub").GetString()
                .Should().Be("target");
            audit.PermissionCode.Should().Be(AbwabPermissionCatalogue.All[0].Code);
        }
    }

    [Fact]
    public async Task AuditEvent_RejectsUpdatesAndDeletes()
    {
        await fixture.ResetAsync();
        var actorId = await fixture.InsertPersonaAsync("Owner");
        var targetId = await fixture.InsertPersonaAsync("ReadOnly");
        long auditId;

        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            var audit = new AccessAuditEvent(
                DateTimeOffset.UtcNow,
                AccessAuditActionType.UserAccepted,
                AccessAuditActorType.System,
                null,
                targetId,
                "{\"actor\":\"system\"}",
                "{\"sub\":\"target\"}",
                null,
                null,
                "{\"status\":\"pending\"}",
                "test",
                new AccessAuditMetadata(1));
            db.AccessAuditEvents.Add(audit);
            await db.SaveChangesAsync();
            auditId = audit.Id;
        }

        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            var audit = await db.AccessAuditEvents.SingleAsync(eventItem => eventItem.Id == auditId);
            db.Entry(audit).Property(nameof(AccessAuditEvent.Reason)).CurrentValue = "changed";
            var act = () => db.SaveChangesAsync();
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            var audit = await db.AccessAuditEvents.SingleAsync(eventItem => eventItem.Id == auditId);
            db.AccessAuditEvents.Remove(audit);
            var act = () => db.SaveChangesAsync();
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AuditMetadata_RejectsANonPositiveSchemaVersion(int schemaVersion)
    {
        var act = () => new AccessAuditMetadata(schemaVersion);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task AuditMetadata_RoundTripsItsVersionCorrelationAndProvenance()
    {
        await fixture.ResetAsync();
        var targetId = await fixture.InsertPersonaAsync("ReadOnly");
        var metadata = new AccessAuditMetadata(
            1,
            "correlation-1",
            new Dictionary<string, string> { ["operation"] = "reconciliation" });

        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            db.AccessAuditEvents.Add(CreateAuditEvent(targetId, metadata: metadata));
            await db.SaveChangesAsync();
        }

        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            var persisted = await db.AccessAuditEvents.AsNoTracking().SingleAsync();

            persisted.Metadata.SchemaVersion.Should().Be(1);
            persisted.Metadata.CorrelationId.Should().Be("correlation-1");
            persisted.Metadata.Provenance.Should().Contain("operation", "reconciliation");
        }
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"schemaVersion\":0}")]
    [InlineData("{\"correlationId\":\"c\"}")]
    [InlineData("[]")]
    [InlineData("{\"schemaVersion\":1.0}")]
    [InlineData("{\"schemaVersion\":1.5}")]
    [InlineData("{\"schemaVersion\":2147483648}")]
    [InlineData("{\"schemaVersion\":-2}")]
    public async Task AuditMetadata_RejectsRawWritesWithoutAPositiveIntegerSchemaVersion(string metadataJson)
    {
        await fixture.ResetAsync();
        var targetId = await fixture.InsertPersonaAsync("ReadOnly");

        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        const string emptyDocument = "{}";
        var act = () => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO access_audit_events
                (occurred_at, action_type, actor_type, target_user_id,
                 actor_snapshot, target_snapshot, metadata)
            VALUES ({DateTimeOffset.UtcNow}, 'UserAccepted', 'System', {targetId},
                {emptyDocument}::jsonb, {emptyDocument}::jsonb, {metadataJson}::jsonb);
            """);

        await act.Should().ThrowAsync<PostgresException>();
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("\"actor\"")]
    [InlineData("{")]
    public async Task AuditEvent_RejectsMalformedOrNonObjectSnapshotsAtTheDatabase(string actorSnapshotJson)
    {
        await fixture.ResetAsync();
        var targetId = await fixture.InsertPersonaAsync("ReadOnly");

        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        db.AccessAuditEvents.Add(CreateAuditEvent(targetId, actorSnapshotJson: actorSnapshotJson));

        var act = () => db.SaveChangesAsync();

        var assertion = await act.Should().ThrowAsync<DbUpdateException>();
        assertion.WithInnerException<PostgresException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AuditEvent_RejectsBlankDocuments(string actorSnapshotJson)
    {
        var act = () => CreateAuditEvent(1, actorSnapshotJson: actorSnapshotJson);

        act.Should().Throw<ArgumentException>();
    }

    private static AccessAuditEvent CreateAuditEvent(
        int targetUserId,
        string actorSnapshotJson = "{\"actor\":\"system\"}",
        AccessAuditMetadata? metadata = null)
    {
        return new AccessAuditEvent(
            DateTimeOffset.UtcNow,
            AccessAuditActionType.UserAccepted,
            AccessAuditActorType.System,
            null,
            targetUserId,
            actorSnapshotJson,
            "{\"sub\":\"target\"}",
            null,
            null,
            "{\"status\":\"pending\"}",
            "test",
            metadata ?? new AccessAuditMetadata(1));
    }
}
