using Microsoft.Extensions.Logging.Abstractions;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Application.Abwab.Commands.Relations.AddDoorRelations;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Tests.Abwab;

// The handler owns direction validity for the API: a Comprehensiveness add without a direction is a
// 400 refused before the writer seam is reached. AbwabRelationWriteBehaviorTests pins the writer's
// own refusal of the same input, so both layers of the contract are asserted.
public sealed class AddDoorRelationsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ComprehensivenessWithoutDirection_ReturnsInvalidDirectionWithoutReachingWriter()
    {
        var writer = new RecordingRelationsWriter();
        var handler = new AddDoorRelationsHandler(NullLogger<AddDoorRelationsHandler>.Instance, writer);

        var outcome = await handler.HandleAsync(
            new AddDoorRelationsCommand(1, AbwabRelationType.Comprehensiveness, null, [2]),
            CancellationToken.None);

        outcome.Should().BeOfType<AddDoorRelationsOutcome.InvalidDirection>();
        writer.AddCalls.Should().Be(0);
    }

    private sealed class RecordingRelationsWriter : IAbwabRelationsWriter
    {
        public int AddCalls { get; private set; }

        public Task<IReadOnlyList<AbwabDoorRelationDto>> AddAsync(
            int doorId,
            AbwabRelationType type,
            AbwabRelationDirection? direction,
            IReadOnlyList<int> targetDoorIds,
            CancellationToken cancellationToken)
        {
            AddCalls++;
            return Task.FromResult<IReadOnlyList<AbwabDoorRelationDto>>([]);
        }

        public Task<bool> DeleteAsync(int relationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
