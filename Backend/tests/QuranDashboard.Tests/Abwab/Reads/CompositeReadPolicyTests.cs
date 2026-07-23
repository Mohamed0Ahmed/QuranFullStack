using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abwab.Categories;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Application.Abwab.Tree;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Domain.Security.Permissions;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Reads;

[Collection(nameof(AbwabDbCollection))]
public sealed class CompositeReadPolicyTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    public static IEnumerable<object[]> GrantCombinations()
    {
        var codes = new[] { PermissionCatalogue.CategoryView, PermissionCatalogue.SectionView, PermissionCatalogue.ProtectionView };
        for (var mask = 0; mask < 8; mask++)
        {
            var granted = new HashSet<string>(StringComparer.Ordinal);
            for (var bit = 0; bit < codes.Length; bit++)
            {
                if ((mask & (1 << bit)) != 0)
                {
                    granted.Add(codes[bit]);
                }
            }

            yield return [granted];
        }
    }

    [Theory]
    [MemberData(nameof(GrantCombinations))]
    public async Task Redact_MatchesTheContractTable_ForEveryGrantCombination(HashSet<string> granted)
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار التصريحات", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(rootId, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var readPort = AbwabWriterTestHarness.CreateReadPort(db);
        var snapshot = await readPort.GetTreeSnapshotAsync(CancellationToken.None);

        var redacted = AbwabCompositeReadRedactor.Redact(snapshot, granted);

        var hasCategoryAndSectionView = granted.Contains(PermissionCatalogue.CategoryView) && granted.Contains(PermissionCatalogue.SectionView);
        var hasProtectionView = granted.Contains(PermissionCatalogue.ProtectionView);

        if (!hasCategoryAndSectionView)
        {
            redacted.Should().BeNull("tree/search requires BOTH category.view and section.view");
            return;
        }

        redacted.Should().NotBeNull();
        var redactedRoot = redacted!.Categories.Single(c => c.CategoryId == rootId);

        if (hasProtectionView)
        {
            redactedRoot.Protection.ManualProtections.Should().NotBeNull("protection.view unlocks full manual-protection metadata");
            redactedRoot.Protection.ManualProtections!.Should().Contain(m => m.ProtectionType == ManualProtectionType.Deletion && m.IsProtected);
        }
        else
        {
            redactedRoot.Protection.ManualProtections.Should().BeNull(
                "without protection.view, type/scope/actor/source-ancestor must never leak — only the two generic flags survive");
            redactedRoot.Protection.HasEffectiveManualProtection.Should().BeTrue("the generic flag itself is visible with only category.view+section.view");
        }
    }

    [Fact]
    public async Task Redact_AppliesIdenticallyToSearchResults()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار نتائج البحث", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.CategoryData, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var readPort = AbwabWriterTestHarness.CreateReadPort(db);
        var results = await readPort.SearchCategoriesAsync("اختبار نتائج", CancellationToken.None);

        var categoryOnly = new HashSet<string>(StringComparer.Ordinal) { PermissionCatalogue.CategoryView, PermissionCatalogue.SectionView };
        var redacted = AbwabCompositeReadRedactor.Redact(results, categoryOnly);

        redacted.Should().NotBeNull();
        redacted!.Matches.Should().Contain(m => m.CategoryId == categoryId);
        redacted.Matches.Single(m => m.CategoryId == categoryId).Protection.ManualProtections.Should().BeNull();
    }

    [Fact]
    public void Redact_DeniesEntirely_WhenMissingCategoryOrSectionView_EvenWithProtectionView()
    {
        var protectionOnly = new HashSet<string>(StringComparer.Ordinal) { PermissionCatalogue.ProtectionView };

        var access = AbwabCompositeReadRedactor.Classify(protectionOnly);

        access.Should().Be(AbwabCompositeReadAccess.Denied, "protection.view alone never substitutes for category.view+section.view");
    }
}
