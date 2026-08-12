using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Tests.Abwab;

// The writer owns the pair-and-direction contract: a Comprehensiveness add
// whose direction is null must be refused at the seam, never silently stored as "target is broader".
// The handler's own 400 for the same input is pinned by AddDoorRelationsHandlerTests; this asserts
// the writer does not depend on that validation two layers up.
[Collection(nameof(AbwabSchemaTestCollection))]
public sealed class AbwabRelationWriteBehaviorTests(AbwabSchemaFixture fixture)
{
    [Fact]
    public async Task AddAsync_ComprehensivenessWithNullDirection_ThrowsRatherThanDefaulting()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();
        var relations = scope.ServiceProvider.GetRequiredService<IAbwabRelationsWriter>();
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();
        var section = (await sections.CreateAsync("سلوك: قسم اتجاه الشمول", CancellationToken.None)).Id;

        var anchor = await doors.CreateAsync(
            section, null, "سلوك: باب شامل بلا اتجاه", null, null, [], CancellationToken.None);
        var target = await doors.CreateAsync(
            section, null, "سلوك: باب مشمول بلا اتجاه", null, null, [], CancellationToken.None);

        var act = async () => await relations.AddAsync(
            anchor.Id, AbwabRelationType.Comprehensiveness, null, [target.Id], CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
