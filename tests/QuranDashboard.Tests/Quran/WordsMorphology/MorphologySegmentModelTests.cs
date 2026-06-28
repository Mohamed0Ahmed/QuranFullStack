using Microsoft.EntityFrameworkCore.Metadata;
using QuranDashboard.Domain.Quran.Words.Morphology;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

[Collection(nameof(MorphologyImportTestCollection))]
public sealed class MorphologySegmentModelTests(MorphologyImportTestFixture fixture)
{
    [Fact]
    public void Segment_dimension_columns_are_nullable_and_indexed_without_stem_id()
    {
        using var scope = fixture.CreateServiceProvider().CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var entity = dbContext.Model.FindEntityType(typeof(WordMorphologySegment));

        entity.Should().NotBeNull();
        var lemmaProperty = entity!.FindProperty(nameof(WordMorphologySegment.LemmaId));
        var rootProperty = entity.FindProperty(nameof(WordMorphologySegment.RootId));

        lemmaProperty.Should().NotBeNull();
        rootProperty.Should().NotBeNull();
        lemmaProperty!.GetColumnName().Should().Be("lemma_id");
        rootProperty!.GetColumnName().Should().Be("root_id");
        lemmaProperty.IsNullable.Should().BeTrue();
        rootProperty.IsNullable.Should().BeTrue();

        entity.FindProperty("StemId").Should().BeNull();
        entity.GetIndexes().Should().Contain(index =>
            index.GetDatabaseName() == "IX_quran_word_morphology_segments_lemma_id"
            && index.Properties.Single().Name == nameof(WordMorphologySegment.LemmaId));
        entity.GetIndexes().Should().Contain(index =>
            index.GetDatabaseName() == "IX_quran_word_morphology_segments_root_id"
            && index.Properties.Single().Name == nameof(WordMorphologySegment.RootId));
    }

    [Fact]
    public void Segment_dimension_foreign_keys_target_lemmas_and_roots_with_restrict_delete()
    {
        using var scope = fixture.CreateServiceProvider().CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var entity = dbContext.Model.FindEntityType(typeof(WordMorphologySegment));

        entity.Should().NotBeNull();
        var lemmaForeignKey = entity!.GetForeignKeys().Single(fk =>
            fk.Properties.Single().Name == nameof(WordMorphologySegment.LemmaId));
        var rootForeignKey = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Single().Name == nameof(WordMorphologySegment.RootId));

        lemmaForeignKey.PrincipalEntityType.ClrType.Should().Be(typeof(QuranLemma));
        rootForeignKey.PrincipalEntityType.ClrType.Should().Be(typeof(QuranRoot));
        lemmaForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        rootForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }
}
