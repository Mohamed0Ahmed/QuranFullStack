using QuranDashboard.Domain.Access;
using QuranDashboard.Application.Abstractions.Security.Permissions;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(MutableDatabaseCollection))]
public sealed class AccessCollectionResetContractTests(AccessTestFixture fixture)
    : AccessMutableWriterTest(fixture)
{
    [Fact]
    public async Task RestartScenarioAsync_ClearsAccessStateAndPreservesCatalogueFingerprintAndSequences()
    {
        var protectedFingerprint = await Fixture.ComputeProtectedStateFingerprintAsync();
        var roles = await ReadRolesAsync();
        var permissions = await ReadPermissionsAsync();
        var actorId = await Fixture.InsertPersonaAsync("Owner");
        var targetId = await Fixture.InsertPersonaAsync("ReadOnly");
        await DirtyEveryAccessMutableTableAsync(actorId, targetId);
        var sequenceValues = await ReadAccessSequenceValuesAsync();

        await Fixture.RestartScenarioAsync();

        await AssertAccessMutableTablesAreEmptyAsync();
        (await ReadRolesAsync()).Should().Equal(roles);
        (await ReadPermissionsAsync()).Should().Equal(permissions);
        (await ReadAccessSequenceValuesAsync()).Should().BeEquivalentTo(sequenceValues);
        (await Fixture.ComputeProtectedStateFingerprintAsync()).Should().Be(protectedFingerprint);
    }

    [Fact]
    public async Task RestartScenarioAsync_DoesNotRewindApplicationReturnedIdentifiers()
    {
        var firstUserId = await Fixture.InsertPersonaAsync("Owner");

        await Fixture.RestartScenarioAsync();
        var nextUserId = await Fixture.InsertPersonaAsync("Owner");

        nextUserId.Should().BeGreaterThan(firstUserId);
    }

    [Fact]
    public async Task EndScenarioAsync_AfterScenarioFailure_CleansStateAndPreservesProtectedState()
    {
        var protectedFingerprint = await Fixture.ComputeProtectedStateFingerprintAsync();
        var failure = await Record.ExceptionAsync(async () =>
        {
            try
            {
                await Fixture.InsertPersonaAsync("Owner");
                throw new InvalidOperationException("MutableWriter failure-cleanup probe.");
            }
            finally
            {
                await Fixture.EndScenarioAsync();
            }
        });

        failure.Should().BeOfType<InvalidOperationException>();
        await Fixture.BeginScenarioAsync();
        (await Fixture.GetUsersAsync()).Should().BeEmpty();
        (await Fixture.ComputeProtectedStateFingerprintAsync()).Should().Be(protectedFingerprint);
    }

    private async Task DirtyEveryAccessMutableTableAsync(int actorId, int targetId)
    {
        await using var scope = Fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var permission = await db.AccessPermissions.SingleAsync(candidate =>
            candidate.Code == AbwabPermissionCatalogue.All[0].Code);
        db.AccessUserPermissions.Add(new UserPermission
        {
            UserId = targetId,
            PermissionId = permission.Id,
            GrantedByUserId = actorId,
            GrantedAtUtc = DateTimeOffset.UtcNow,
        });
        db.AccessUserDeviceSessions.Add(new UserDeviceSession
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            TokenHash = new string('a', 64),
            CsrfTokenHash = new string('b', 64),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(90),
        });
        db.AccessAuditEvents.Add(new AccessAuditEvent(
            DateTimeOffset.UtcNow,
            AccessAuditActionType.PermissionGranted,
            AccessAuditActorType.User,
            actorId,
            targetId,
            "{\"sub\":\"actor\"}",
            "{\"sub\":\"target\"}",
            permission.Code,
            "{}",
            $"{{\"permission\":\"{permission.Code}\"}}",
            "reset contract",
            new AccessAuditMetadata(1)));
        await db.SaveChangesAsync();
    }

    private async Task AssertAccessMutableTablesAreEmptyAsync()
    {
        await using var scope = Fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await db.AccessUsers.CountAsync()).Should().Be(0);
        (await db.AccessUserPermissions.CountAsync()).Should().Be(0);
        (await db.AccessUserDeviceSessions.CountAsync()).Should().Be(0);
        (await db.AccessAuditEvents.CountAsync()).Should().Be(0);
    }

    private async Task<IReadOnlyList<(int Id, string Name)>> ReadRolesAsync()
    {
        await using var scope = Fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var roles = await db.AccessRoles.AsNoTracking()
            .OrderBy(role => role.Id)
            .Select(role => new { role.Id, role.Name })
            .ToListAsync();
        return roles.Select(role => (role.Id, role.Name)).ToArray();
    }

    private async Task<IReadOnlyList<(int Id, string Code, DateTimeOffset? RetiredAtUtc)>> ReadPermissionsAsync()
    {
        await using var scope = Fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var permissions = await db.AccessPermissions.AsNoTracking()
            .OrderBy(permission => permission.Id)
            .Select(permission => new
            {
                permission.Id,
                permission.Code,
                permission.RetiredAtUtc,
            })
            .ToListAsync();
        return permissions
            .Select(permission => (permission.Id, permission.Code, permission.RetiredAtUtc))
            .ToArray();
    }

    private async Task<IReadOnlyDictionary<string, long?>> ReadAccessSequenceValuesAsync()
    {
        await using var connection = new NpgsqlConnection(Fixture.ApplicationConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT sequencename, last_value
            FROM pg_catalog.pg_sequences
            WHERE schemaname = 'public'
              AND sequencename IN (
                  'roles_id_seq',
                  'permissions_id_seq',
                  'users_id_seq',
                  'user_permissions_id_seq',
                  'access_audit_events_id_seq')
            ORDER BY sequencename
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new Dictionary<string, long?>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetInt64(1));
        }

        return values;
    }
}
