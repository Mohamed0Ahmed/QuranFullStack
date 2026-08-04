using Microsoft.Extensions.Logging.Abstractions;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Application.Abwab.Commands.Doors.CreateDoor;

namespace QuranDashboard.Tests.Abwab;

// A ROOT door create renumbers sibling roots (MaintainGlobalOrderAsync), so the writer's save can
// surface AbwabStaleVersionException even though the door row itself is an INSERT. The handler must
// map it to its own outcome — without the catch it escapes to the global handler as a 500 where the
// contract says 409.
public sealed class CreateDoorHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenWriterRaisesStaleVersion_ReturnsStaleVersionOutcome()
    {
        var handler = new CreateDoorHandler(
            NullLogger<CreateDoorHandler>.Instance, new StaleVersionRaisingDoorsWriter());

        var outcome = await handler.HandleAsync(
            new CreateDoorCommand(1, null, "باب يخسر سباق الترتيب", null, null, []),
            CancellationToken.None);

        outcome.Should().BeOfType<CreateDoorOutcome.StaleVersion>();
    }

    private sealed class StaleVersionRaisingDoorsWriter : IAbwabDoorsWriter
    {
        public Task<AbwabDoorDto> CreateAsync(
            int? sectionId,
            int? parentId,
            string name,
            string? description,
            string? representativeAyahText,
            IReadOnlyList<string> aliases,
            CancellationToken cancellationToken) => throw new AbwabStaleVersionException();

        public Task<AbwabDoorDto?> EditAsync(
            int id,
            string name,
            string? description,
            string? representativeAyahText,
            IReadOnlyList<string> aliases,
            uint expectedVersion,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<AbwabDoorDto?> MoveAsync(
            int id,
            int? targetSectionId,
            int? targetParentId,
            uint expectedVersion,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<AbwabDoorDto?> ReorderAsync(
            int id,
            int position,
            AbwabReorderScope scope,
            uint expectedVersion,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<AbwabDoorDto>> BulkMoveAsync(
            IReadOnlyList<AbwabBulkDoorRef> doors,
            int? targetSectionId,
            int? targetParentId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<int>> BulkArchiveAsync(
            IReadOnlyList<AbwabBulkDoorRef> doors,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> DeleteAsync(
            int id, uint expectedVersion, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AbwabDoorDto?> RestoreAsync(
            int id,
            int? sectionId,
            uint expectedVersion,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
