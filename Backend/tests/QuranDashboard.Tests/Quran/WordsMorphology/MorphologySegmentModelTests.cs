using QuranDashboard.Domain.Quran.Words.Morphology;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

[Collection(nameof(MorphologyImportTestCollection))]
public sealed class MorphologySegmentModelTests(MorphologyImportTestFixture fixture)
{
    [Fact]
    public void Segment_dimension_columns_are_nullable_and_indexed_including_stem_id()
    {
        using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var entity = dbContext.Model.FindEntityType(typeof(WordMorphologySegment));

        entity.Should().NotBeNull();
        var lemmaProperty = entity!.FindProperty(nameof(WordMorphologySegment.LemmaId));
        var rootProperty = entity.FindProperty(nameof(WordMorphologySegment.RootId));
        var stemProperty = entity.FindProperty(nameof(WordMorphologySegment.StemId));

        lemmaProperty.Should().NotBeNull();
        rootProperty.Should().NotBeNull();
        stemProperty.Should().NotBeNull();
        lemmaProperty!.GetColumnName().Should().Be("lemma_id");
        rootProperty!.GetColumnName().Should().Be("root_id");
        stemProperty!.GetColumnName().Should().Be("stem_id");
        lemmaProperty.IsNullable.Should().BeTrue();
        rootProperty.IsNullable.Should().BeTrue();
        stemProperty.IsNullable.Should().BeTrue();

        entity.GetIndexes().Should().Contain(index =>
            index.GetDatabaseName() == "IX_quran_word_morphology_segments_lemma_id"
            && index.Properties.Single().Name == nameof(WordMorphologySegment.LemmaId));
        entity.GetIndexes().Should().Contain(index =>
            index.GetDatabaseName() == "IX_quran_word_morphology_segments_root_id"
            && index.Properties.Single().Name == nameof(WordMorphologySegment.RootId));
        entity.GetIndexes().Should().Contain(index =>
            index.GetDatabaseName() == "IX_quran_word_morphology_segments_stem_id"
            && index.Properties.Single().Name == nameof(WordMorphologySegment.StemId));
    }

    [Fact]
    public void Segment_dimension_foreign_keys_target_lemmas_roots_and_stems_with_restrict_delete()
    {
        using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var entity = dbContext.Model.FindEntityType(typeof(WordMorphologySegment));

        entity.Should().NotBeNull();
        var lemmaForeignKey = entity!.GetForeignKeys().Single(fk =>
            fk.Properties.Single().Name == nameof(WordMorphologySegment.LemmaId));
        var rootForeignKey = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Single().Name == nameof(WordMorphologySegment.RootId));
        var stemForeignKey = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Single().Name == nameof(WordMorphologySegment.StemId));

        lemmaForeignKey.PrincipalEntityType.ClrType.Should().Be(typeof(QuranLemma));
        rootForeignKey.PrincipalEntityType.ClrType.Should().Be(typeof(QuranRoot));
        stemForeignKey.PrincipalEntityType.ClrType.Should().Be(typeof(QuranStem));
        lemmaForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        rootForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        stemForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }
}
