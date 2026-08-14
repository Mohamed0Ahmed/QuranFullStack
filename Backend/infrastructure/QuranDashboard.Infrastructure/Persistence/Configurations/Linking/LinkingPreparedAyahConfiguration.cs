using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingPreparedAyahConfiguration : IEntityTypeConfiguration<LinkingPreparedAyah>
{
    public void Configure(EntityTypeBuilder<LinkingPreparedAyah> builder)
    {
        builder.ToTable("linking_prepared_ayahs", table =>
        {
            table.HasCheckConstraint(
                "ck_linking_prepared_ayahs_requested_unit",
                "(is_requested AND unit_id IS NOT NULL) OR (NOT is_requested AND unit_id IS NULL)");
            table.HasCheckConstraint(
                "ck_linking_prepared_ayahs_order",
                "source_order > 0 AND unit_order > 0 AND ayah_order > 0 AND quran_order > 0");
            table.HasCheckConstraint(
                "ck_linking_prepared_ayahs_classification",
                LinkingDescriptorCheckConstraints.TokenIn(
                    "classification", LinkingPreparedSchemaConstraints.ClassificationTokens));
            table.HasCheckConstraint(
                "ck_linking_prepared_ayahs_invalid_reason",
                "invalid_reason IS NULL OR " + LinkingDescriptorCheckConstraints.TokenIn(
                    "invalid_reason", LinkingPreparedSchemaConstraints.InvalidReasonTokens));
            table.HasCheckConstraint(
                "ck_linking_prepared_ayahs_impact",
                LinkingDescriptorCheckConstraints.JsonbSchemaVersion("classification_impact"));
        });

        builder.HasKey(ayah => ayah.Id);
        builder.Property(ayah => ayah.Id).ValueGeneratedOnAdd().HasColumnName("id");
        builder.Property(ayah => ayah.PreflightId).IsRequired().HasColumnName("preflight_id");
        builder.Property(ayah => ayah.SourceId).IsRequired().HasColumnName("source_id");
        builder.Property(ayah => ayah.UnitId).HasColumnName("unit_id");
        builder.Property(ayah => ayah.IsRequested).IsRequired().HasColumnName("is_requested");
        builder.Property(ayah => ayah.SourceOrder).IsRequired().HasColumnName("source_order");
        builder.Property(ayah => ayah.UnitOrder).IsRequired().HasColumnName("unit_order");
        builder.Property(ayah => ayah.AyahOrder).IsRequired().HasColumnName("ayah_order");
        builder.Property(ayah => ayah.QuranOrder).IsRequired().HasColumnName("quran_order");
        builder.Property(ayah => ayah.IsGrouped).IsRequired().HasColumnName("is_grouped");
        builder.Property(ayah => ayah.AyahId).IsRequired().HasColumnName("ayah_id");
        builder.Property(ayah => ayah.Classification).IsRequired().HasColumnName("classification");
        builder.Property(ayah => ayah.InvalidReason).HasColumnName("invalid_reason");
        builder.Property(ayah => ayah.ClassificationImpactJson)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("classification_impact");

        builder.HasOne<LinkingPreparedSource>()
            .WithMany()
            .HasForeignKey(ayah => new { ayah.SourceId, ayah.PreflightId })
            .HasPrincipalKey(source => new { source.Id, source.PreflightId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LinkingPreparedUnit>()
            .WithMany()
            .HasForeignKey(ayah => new { ayah.UnitId, ayah.SourceId, ayah.PreflightId })
            .HasPrincipalKey(unit => new { unit.Id, unit.SourceId, unit.PreflightId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ayah => new { ayah.SourceId, ayah.AyahId }).IsUnique();
        builder.HasIndex(ayah => new { ayah.SourceId, ayah.QuranOrder, ayah.AyahId });
        builder.HasIndex(ayah => new { ayah.SourceId, ayah.Classification, ayah.QuranOrder, ayah.AyahId });
        builder.HasIndex(ayah => new { ayah.PreflightId, ayah.QuranOrder, ayah.AyahId });
        builder.HasIndex(ayah => new { ayah.PreflightId, ayah.Classification, ayah.QuranOrder, ayah.AyahId });
        builder.HasIndex(ayah => new { ayah.PreflightId, ayah.AyahId, ayah.SourceOrder });
    }
}
