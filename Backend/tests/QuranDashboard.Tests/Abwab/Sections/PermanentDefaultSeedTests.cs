using QuranDashboard.Domain.Abwab.Tree;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Sections;

[Collection(nameof(AbwabDbCollection))]
public sealed class PermanentDefaultSeedTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Migration_Seeds_ExactlyOnePermanentDefaultSection()
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);

        var permanentDefaults = await context.AbwabSections
            .AsNoTracking()
            .Where(s => s.IsPermanentDefault)
            .ToListAsync();

        permanentDefaults.Should().ContainSingle();
        var section = permanentDefaults[0];
        section.Name.Should().Be("أبواب غير مصنفة");
        section.NormalizedName.Should().Be(ArabicNameNormalizer.Normalize("أبواب غير مصنفة"));
        section.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Migration_Seeds_ExactlyOneSectionTotal()
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);

        var count = await context.AbwabSections.AsNoTracking().CountAsync();

        count.Should().Be(1);
    }

    [Fact]
    public async Task PermanentDefaultIndex_Rejects_ASecondPermanentDefaultRow()
    {
        var duplicate = AbwabTreeSeeding.NewSection("قسم افتراضي آخر", isPermanentDefault: true);

        var act = () => AbwabTreeSeeding.InsertAsync(fixture, duplicate);

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
