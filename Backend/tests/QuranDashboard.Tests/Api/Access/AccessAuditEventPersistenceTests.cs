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
                "{\"schemaVersion\":1}"));
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
                "{}");
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
}
