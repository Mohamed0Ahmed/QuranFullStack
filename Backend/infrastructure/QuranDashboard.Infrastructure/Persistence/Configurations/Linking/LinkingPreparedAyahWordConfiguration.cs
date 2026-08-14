using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingPreparedAyahWordConfiguration : IEntityTypeConfiguration<LinkingPreparedAyahWord>
{
    public void Configure(EntityTypeBuilder<LinkingPreparedAyahWord> builder)
    {
        builder.ToTable("linking_prepared_ayah_words", table =>
        {
            table.HasCheckConstraint(
                "ck_linking_prepared_ayah_words_membership",
                "is_source_match OR is_requested");
            table.HasCheckConstraint(
                "ck_linking_prepared_ayah_words_order",
                "order_value > 0");
        });

        builder.HasKey(word => new { word.PreparedAyahId, word.QuranWordId });
        builder.Property(word => word.PreparedAyahId).IsRequired().HasColumnName("prepared_ayah_id");
        builder.Property(word => word.QuranWordId).IsRequired().HasColumnName("quran_word_id");
        builder.Property(word => word.IsSourceMatch).IsRequired().HasColumnName("is_source_match");
        builder.Property(word => word.IsRequested).IsRequired().HasColumnName("is_requested");
        builder.Property(word => word.OrderValue).IsRequired().HasColumnName("order_value");

        builder.HasOne<LinkingPreparedAyah>()
            .WithMany()
            .HasForeignKey(word => word.PreparedAyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(word => new { word.PreparedAyahId, word.OrderValue }).IsUnique();
    }
}
