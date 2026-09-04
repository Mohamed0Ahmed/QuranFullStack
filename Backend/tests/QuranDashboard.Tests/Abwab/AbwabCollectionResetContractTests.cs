using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Tests.Api.Access;

namespace QuranDashboard.Tests.Abwab;

[Collection(nameof(MutableDatabaseCollection))]
public sealed class AbwabCollectionResetContractTests(AccessTestFixture fixture)
    : AbwabMutableWriterTest(fixture)
{
    [Fact]
    public async Task RestartScenarioAsync_ClearsAbwabStateAndPreservesCatalogueFingerprintAndSequences()
    {
        var protectedFingerprint = await Fixture.ComputeProtectedStateFingerprintAsync();
        await DirtyEveryAbwabMutableTableAsync();
        var sequenceValues = await ReadAbwabSequenceValuesAsync();

        await Fixture.RestartScenarioAsync();

        await AssertAbwabMutableTablesAreEmptyAsync();
        (await ReadAbwabSequenceValuesAsync()).Should().BeEquivalentTo(sequenceValues);
        (await Fixture.ComputeProtectedStateFingerprintAsync()).Should().Be(protectedFingerprint);
    }

    [Fact]
    public async Task RestartScenarioAsync_DoesNotRewindApplicationReturnedIdentifiers()
    {
        await using (var scope = Fixture.Services.CreateAsyncScope())
        {
            var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();
            var first = await sections.CreateAsync("قسم تجربة تراجع المعرفات 1", CancellationToken.None);

            await Fixture.RestartScenarioAsync();

            await using var nextScope = Fixture.Services.CreateAsyncScope();
            var nextSections = nextScope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();
            var next = await nextSections.CreateAsync("قسم تجربة تراجع المعرفات 2", CancellationToken.None);

            next.Id.Should().BeGreaterThan(first.Id);
        }
    }

    [Fact]
    public async Task EndScenarioAsync_AfterScenarioFailure_CleansStateAndPreservesProtectedState()
    {
        var protectedFingerprint = await Fixture.ComputeProtectedStateFingerprintAsync();
        var failure = await Record.ExceptionAsync(async () =>
        {
            try
            {
                await using var scope = Fixture.Services.CreateAsyncScope();
                var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();
                await sections.CreateAsync("قسم تجربة فشل السيناريو", CancellationToken.None);
                throw new InvalidOperationException("MutableWriter failure-cleanup probe.");
            }
            finally
            {
                await Fixture.EndScenarioAsync();
            }
        });

        failure.Should().BeOfType<InvalidOperationException>();
        await Fixture.BeginScenarioAsync();
        await AssertAbwabMutableTablesAreEmptyAsync();
        (await Fixture.ComputeProtectedStateFingerprintAsync()).Should().Be(protectedFingerprint);
    }

    private async Task DirtyEveryAbwabMutableTableAsync()
    {
        var actor = await Fixture.CreateActiveNonOwnerAsync("abwab-reset-contract-actor");
        await using var scope = Fixture.Services.CreateAsyncScope();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var relations = scope.ServiceProvider.GetRequiredService<IAbwabRelationsWriter>();
        var templates = scope.ServiceProvider.GetRequiredService<IAbwabTemplatesWriter>();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var section = await sections.CreateAsync("قسم تجربة إعادة الضبط", CancellationToken.None);
        var doorA = await doors.CreateAsync(
            section.Id, null, "باب تجربة إعادة الضبط أ", null, null, ["مرادف أ"], CancellationToken.None);
        var doorB = await doors.CreateAsync(
            section.Id, null, "باب تجربة إعادة الضبط ب", null, null, ["مرادف ب"], CancellationToken.None);
        await relations.AddAsync(
            doorA.Id, AbwabRelationType.Similarity, null, [doorB.Id], CancellationToken.None);

        var now = DateTimeOffset.UtcNow;
        var sourceUnit = new LinkingUnit
        {
            DoorId = doorB.Id,
            Identity = "abwab-reset-source-unit",
            IdentityHash = [1],
            CreatedAtUtc = now,
            CreatedBy = actor.UserId,
        };
        var targetUnit = new LinkingUnit
        {
            DoorId = doorA.Id,
            Identity = "abwab-reset-target-unit",
            IdentityHash = [2],
            CreatedAtUtc = now,
            CreatedBy = actor.UserId,
        };
        db.LinkingUnits.AddRange(sourceUnit, targetUnit);
        await db.SaveChangesAsync();

        var template = await templates.CreateAsync(
            "قالب تجربة إعادة الضبط", null, null, ["وسم"], CancellationToken.None);
        var rootNode = template.Nodes.Single(node => node.ParentNodeId is null);
        await templates.AddNodeAsync(
            template.Id, rootNode.Id, "عقدة قالب تجربة إعادة الضبط", null, null, [], CancellationToken.None);

        var inclusion = new AbwabDoorInclusion
        {
            TargetDoorId = doorA.Id,
            SourceDoorId = doorB.Id,
            CreatedBy = actor.UserId,
            CreatedAtUtc = now,
            UpdatedBy = actor.UserId,
            UpdatedAtUtc = now,
        };
        db.AbwabDoorInclusions.Add(inclusion);
        await db.SaveChangesAsync();

        db.AbwabDoorInclusionUnitSyncs.Add(new AbwabDoorInclusionUnitSync
        {
            DoorInclusionId = inclusion.Id,
            SourceUnitId = sourceUnit.Id,
            TargetUnitId = targetUnit.Id,
            State = AbwabDoorInclusionSyncState.Active,
            SourceFingerprint = [3],
            CreatedBy = actor.UserId,
            CreatedAtUtc = now,
            UpdatedBy = actor.UserId,
            UpdatedAtUtc = now,
        });
        await db.SaveChangesAsync();
    }

    private async Task AssertAbwabMutableTablesAreEmptyAsync()
    {
        await using var scope = Fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await db.AbwabSections.CountAsync()).Should().Be(0);
        (await db.AbwabDoors.CountAsync()).Should().Be(0);
        (await db.AbwabDoorAliases.CountAsync()).Should().Be(0);
        (await db.AbwabDoorRelations.CountAsync()).Should().Be(0);
        (await db.AbwabDoorInclusions.CountAsync()).Should().Be(0);
        (await db.AbwabDoorInclusionUnitSyncs.CountAsync()).Should().Be(0);
        (await db.AbwabTemplates.CountAsync()).Should().Be(0);
        (await db.AbwabTemplateNodes.CountAsync()).Should().Be(0);
        (await db.LinkingUnits.CountAsync()).Should().Be(0);
    }

    private async Task<IReadOnlyDictionary<string, long?>> ReadAbwabSequenceValuesAsync()
    {
        await using var connection = new NpgsqlConnection(Fixture.ApplicationConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT sequencename, last_value
            FROM pg_catalog.pg_sequences
            WHERE schemaname = 'public'
              AND sequencename IN (
                  'abwab_sections_id_seq',
                  'abwab_doors_id_seq',
                  'abwab_door_aliases_id_seq',
                  'abwab_door_relations_id_seq',
                  'abwab_door_inclusions_id_seq',
                  'abwab_door_inclusion_unit_syncs_id_seq',
                  'abwab_templates_id_seq',
                  'abwab_template_nodes_id_seq')
            ORDER BY sequencename;
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
