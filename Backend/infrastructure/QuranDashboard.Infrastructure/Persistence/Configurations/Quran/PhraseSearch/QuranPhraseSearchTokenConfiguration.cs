using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.PhraseSearch;

public sealed class QuranPhraseSearchTokenConfiguration : IEntityTypeConfiguration<QuranPhraseSearchToken>
{
    public void Configure(EntityTypeBuilder<QuranPhraseSearchToken> builder)
    {
        builder.ToTable("quran_phrase_search_tokens", table =>
        {
            table.HasCheckConstraint(
                "ck_quran_phrase_search_tokens_mode",
                "mode IN (1, 2)");
            table.HasCheckConstraint(
                "ck_quran_phrase_search_tokens_search_text",
                "btrim(search_text) <> ''");
            table.HasCheckConstraint(
                "ck_quran_phrase_search_tokens_exact_token_ids",
                "cardinality(exact_token_ids) > 0");
        });

        builder.HasKey(token => new { token.BuildId, token.Mode, token.Id });

        builder.Property(token => token.BuildId)
            .ValueGeneratedNever()
            .HasColumnName("build_id");

        builder.Property(token => token.Mode)
            .HasConversion<short>()
            .HasColumnType("smallint")
            .HasColumnName("mode");

        builder.Property(token => token.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(token => token.SearchText)
            .IsRequired()
            .HasColumnName("search_text");

        builder.Property(token => token.ExactTokenIds)
            .IsRequired()
            .HasColumnType("integer[]")
            .HasColumnName("exact_token_ids");

        builder.HasOne<PhraseIndexBuild>()
            .WithMany()
            .HasForeignKey(token => token.BuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(token => new { token.BuildId, token.Mode, token.SearchText })
            .IsUnique();
    }
}
