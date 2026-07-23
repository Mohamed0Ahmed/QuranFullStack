using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Infrastructure.Abwab.Restore;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

[Collection(nameof(AbwabDbCollection))]
public sealed class RepresentativeExcerptTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void RepresentativeQuranExcerpt_IsAnOptionalPlainStringColumn_WithNoQuranForeignKey()
    {
        typeof(Category).GetProperty(nameof(Category.RepresentativeQuranExcerpt))!.PropertyType.Should().Be(typeof(string));

        var foreignKeys = typeof(Category).Assembly.GetType("QuranDashboard.Domain.Abwab.Categories.Category")!
            .GetProperties()
            .Where(p => p.Name.Contains("Ayah", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Surah", StringComparison.OrdinalIgnoreCase));

        foreignKeys.Should().BeEmpty("no Quran identity field may exist alongside the plain-string excerpt");
    }

    [Fact]
    public async Task Add_WithAnExcerpt_PersistsItAsAnOpaqueString()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        const string excerpt = "نص تمثيلي غير مُتحقق منه هيكليًا — قد لا يطابق أي آية فعلية 12345";

        var categoryId = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب اختبار النص التمثيلي", null, excerpt, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var category = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        category.RepresentativeQuranExcerpt.Should().Be(excerpt, "the string is stored verbatim with no ayah validation or reformatting");
    }

    [Fact]
    public async Task Edit_TheExcerpt_IsAuditedAndBumpsCategoryContentRevisionOnce()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب اختبار تعديل النص", null, "نص أول", null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var before = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        before.CategoryContentRevision.Should().Be(0);

        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();

        await writePort.EditCategoryAsync(
            new EditCategoryCommand(categoryId, "باب اختبار تعديل النص", null, "نص محدّث تمامًا", before.Version, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var after = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        after.RepresentativeQuranExcerpt.Should().Be("نص محدّث تمامًا");
        after.CategoryContentRevision.Should().Be(1);

        var changeSetsAfter = await db.AbwabChangeSets.AsNoTracking().CountAsync();
        changeSetsAfter.Should().Be(changeSetsBefore + 1);
    }

    [Fact]
    public async Task Edit_TheExcerpt_ActivatesOrdinaryProtectionAsDirectContent()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب اختبار تفعيل الحماية العادية", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "actor-1"), CancellationToken.None);
        var before = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        before.OrdinaryProtectionLastEditedAtUtc.Should().BeNull("a freshly created category has no edit history yet");

        await writePort.EditCategoryAsync(
            new EditCategoryCommand(categoryId, "باب اختبار تفعيل الحماية العادية", null, "نص تمثيلي جديد", before.Version, ExpectedTimelineGeneration.Of(0), "actor-1"), CancellationToken.None);

        var after = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        after.OrdinaryProtectionActorSubject.Should().Be("actor-1");
        after.OrdinaryProtectionLastEditedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Excerpt_IsRestorable_ViaTheCategoryAdapter()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب اختبار الاستعادة", null, "نص للاستعادة", null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var category = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        var adapter = new CategoryRestoreAdapter();
        var snapshot = adapter.Capture(new CategoryAggregate(category, []));

        snapshot.RepresentativeQuranExcerpt.Should().Be("نص للاستعادة");

        var reconstructed = adapter.Reconstruct(snapshot);
        reconstructed.Category.RepresentativeQuranExcerpt.Should().Be("نص للاستعادة");
    }
}
