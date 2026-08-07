using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Tests.Abwab;

// The regression behind the UniqueKeyIsolation policy the AbwabSchemaTestCollection row of
// TestSupport/Execution/test-resources.tsv declares: one database serves the whole collection for the
// whole run, so every case must create uniquely keyed rows and read back only its own. Each case here
// writes the collection's representative sequence three times with different keys and NO restore in
// between, then reads all three back through the production tree reader. Fixed keys fail the second
// write — section name is unique and door name is unique within (section, parent), both live-filtered —
// and reading totals instead of own keys fails the scoped assertions.
[Collection(nameof(AbwabSchemaTestCollection))]
public sealed class AbwabCollectionKeyIsolationTests(AbwabSchemaFixture fixture)
{
    private const string KeyPrefix = "عزل: ";

    [Theory]
    [InlineData("الأولى")]
    [InlineData("الثانية")]
    public async Task RepeatedWritesWithoutARestore_ReadBackOnlyTheirOwnKeys(string caseName)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var reader = scope.ServiceProvider.GetRequiredService<IAbwabTreeReader>();

        var earliest = await WriteRepresentativeRowsAsync(scope, $"{caseName} السابقة");
        var middle = await WriteRepresentativeRowsAsync(scope, $"{caseName} الوسطى");
        var latest = await WriteRepresentativeRowsAsync(scope, $"{caseName} اللاحقة");

        latest.SectionId.Should().NotBe(middle.SectionId).And.NotBe(earliest.SectionId);

        var tree = await reader.GetTreeAsync(CancellationToken.None);

        // All three, after all of them were written: a run that trampled an earlier one fails here
        // rather than leaving the damage for whichever case runs next.
        AssertReadsBackItsOwnRows(tree, earliest);
        AssertReadsBackItsOwnRows(tree, middle);
        AssertReadsBackItsOwnRows(tree, latest);
    }

    private static void AssertReadsBackItsOwnRows(AbwabTreeDto tree, WrittenRows written)
    {
        tree.Sections.Should().ContainSingle(section => section.Id == written.SectionId)
            .Which.Name.Should().Be(SectionName(written.Key));

        tree.Doors.Where(door => door.SectionId == written.SectionId).Select(door => door.Id)
            .Should().BeEquivalentTo(new[] { written.RootDoorId, written.ChildDoorId });

        var root = tree.Doors.Single(door => door.Id == written.RootDoorId);
        root.ParentId.Should().BeNull();
        root.Name.Should().Be(RootDoorName(written.Key));
        root.DirectChildCount.Should().Be(1);

        var child = tree.Doors.Single(door => door.Id == written.ChildDoorId);
        child.ParentId.Should().Be(written.RootDoorId);
        child.Name.Should().Be(ChildDoorName(written.Key));
    }

    private static async Task<WrittenRows> WriteRepresentativeRowsAsync(AsyncServiceScope scope, string key)
    {
        var sections = scope.ServiceProvider.GetRequiredService<IAbwabSectionsWriter>();
        var doors = scope.ServiceProvider.GetRequiredService<IAbwabDoorsWriter>();

        var section = await sections.CreateAsync(SectionName(key), CancellationToken.None);
        var root = await doors.CreateAsync(
            section.Id, null, RootDoorName(key), null, null, [], CancellationToken.None);
        var child = await doors.CreateAsync(
            null, root.Id, ChildDoorName(key), null, null, [], CancellationToken.None);

        return new WrittenRows(key, section.Id, root.Id, child.Id);
    }

    private static string SectionName(string key) => $"{KeyPrefix}قسم {key}";

    private static string RootDoorName(string key) => $"{KeyPrefix}باب {key}";

    private static string ChildDoorName(string key) => $"{KeyPrefix}باب فرعي {key}";

    private sealed record WrittenRows(string Key, int SectionId, int RootDoorId, int ChildDoorId);
}
