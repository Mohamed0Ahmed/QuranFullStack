using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Relationships;

[Collection(nameof(AbwabDbCollection))]
public sealed class RelationshipCycleRaceTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TwoConcurrentEdgesThatEachValidateAlone_ButTogetherCloseACycle_LeaveZeroCyclesPersisted()
    {
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 2);

        var (portA, dbA) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        var (portB, dbB) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _a = dbA;
        await using var _b = dbB;

        var forward = portA.AddAsync(Edge(endpoints[0].CategoryId, endpoints[1].CategoryId, "actor-a"), CancellationToken.None);
        var backward = portB.AddAsync(Edge(endpoints[1].CategoryId, endpoints[0].CategoryId, "actor-b"), CancellationToken.None);

        var outcomes = await Task.WhenAll(Outcome(forward), Outcome(backward));

        outcomes.Count(outcome => outcome == "ok").Should().Be(1, "exactly one of the two racing edges commits");
        outcomes.Should().Contain(AbwabConflictCodes.RelationshipCycle, "the loser must be refused as a cycle, not silently persisted");

        var edges = await dbA.Set<CategoryRelationship>().AsNoTracking()
            .Where(r => !r.IsDeleted && r.RelationshipType == RelationshipType.BroaderNarrower)
            .Select(r => new { r.SourceCategoryId, r.TargetCategoryId })
            .ToListAsync();

        edges.Should().HaveCount(1);
        CountCycles(edges.Select(e => (e.SourceCategoryId!.Value, e.TargetCategoryId!.Value)).ToList()).Should().Be(0);
    }

    [Fact]
    public async Task ThreeConcurrentEdgesThatWouldFormALongerCycle_LeaveZeroCyclesPersisted()
    {
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 3);

        var writers = Enumerable.Range(0, 3)
            .Select(_ => AbwabWriterTestHarness.CreateRelationshipWritePort(fixture))
            .ToList();

        try
        {
            var pending = new[]
            {
                writers[0].WritePort.AddAsync(Edge(endpoints[0].CategoryId, endpoints[1].CategoryId, "actor-a"), CancellationToken.None),
                writers[1].WritePort.AddAsync(Edge(endpoints[1].CategoryId, endpoints[2].CategoryId, "actor-b"), CancellationToken.None),
                writers[2].WritePort.AddAsync(Edge(endpoints[2].CategoryId, endpoints[0].CategoryId, "actor-c"), CancellationToken.None),
            };

            var outcomes = await Task.WhenAll(pending.Select(Outcome));

            outcomes.Should().NotContain("unexpected-error");
            outcomes.Count(outcome => outcome == "ok").Should().Be(2, "only the two edges that cannot close the cycle may commit");

            var edges = await writers[0].Db.Set<CategoryRelationship>().AsNoTracking()
                .Where(r => !r.IsDeleted && r.RelationshipType == RelationshipType.BroaderNarrower)
                .Select(r => new { r.SourceCategoryId, r.TargetCategoryId })
                .ToListAsync();

            CountCycles(edges.Select(e => (e.SourceCategoryId!.Value, e.TargetCategoryId!.Value)).ToList()).Should().Be(0);
        }
        finally
        {
            foreach (var writer in writers)
            {
                await writer.Db.DisposeAsync();
            }
        }
    }

    private static AddRelationshipCommand Edge(Guid broader, Guid narrower, string actor) =>
        new(RelationshipType.BroaderNarrower, broader, narrower, ExpectedTimelineGeneration.Of(0), actor);

    private static async Task<string> Outcome(Task task)
    {
        try
        {
            await task;
            return "ok";
        }
        catch (AbwabWriteConflictException conflict)
        {
            return conflict.Code;
        }
        catch
        {
            return "unexpected-error";
        }
    }

    private static int CountCycles(IReadOnlyList<(Guid Source, Guid Target)> edges)
    {
        var nodes = edges.SelectMany(edge => new[] { edge.Source, edge.Target }).Distinct().ToList();
        return nodes.Count(node => Reaches(edges, node, node));
    }

    private static bool Reaches(IReadOnlyList<(Guid Source, Guid Target)> edges, Guid from, Guid target)
    {
        var visited = new HashSet<Guid>();
        var frontier = new Queue<Guid>(edges.Where(edge => edge.Source == from).Select(edge => edge.Target));

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == target)
            {
                return true;
            }

            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var next in edges.Where(edge => edge.Source == current).Select(edge => edge.Target))
            {
                frontier.Enqueue(next);
            }
        }

        return false;
    }
}
