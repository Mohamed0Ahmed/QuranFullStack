using QuranDashboard.Domain.Quran.Words.Morphology.Irab;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Words.Morphology.Irab;

public sealed class QuranI3rabRuleConfiguration : IEntityTypeConfiguration<QuranI3rabRule>
{
    public void Configure(EntityTypeBuilder<QuranI3rabRule> builder)
    {
        builder.ToTable("quran_i3rab_rules", table =>
        {
            table.HasCheckConstraint(
                "CK_quran_i3rab_rules_default_status",
                "default_status IN ('approved', 'needs_review', 'unsupported')");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(r => r.SignatureKey)
            .IsRequired()
            .HasColumnName("signature_key");

        builder.Property(r => r.RuleFamily)
            .IsRequired()
            .HasColumnName("rule_family");

        builder.Property(r => r.I3rabArabic)
            .IsRequired()
            .HasColumnName("i3rab_arabic");

        builder.Property(r => r.DefaultStatus)
            .IsRequired()
            .HasColumnName("default_status");

        builder.Property(r => r.Description)
            .HasColumnName("description");

        builder.Property(r => r.SortOrder)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("sort_order");

        builder.HasIndex(r => r.SignatureKey).IsUnique();
        builder.HasIndex(r => r.RuleFamily);
    }
}
