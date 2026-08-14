using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingPreparedAyahDescriptionConfiguration
    : IEntityTypeConfiguration<LinkingPreparedAyahDescription>
{
    public void Configure(EntityTypeBuilder<LinkingPreparedAyahDescription> builder)
    {
        builder.ToTable("linking_prepared_ayah_descriptions", table =>
        {
            table.HasCheckConstraint(
                "ck_linking_prepared_ayah_descriptions_body",
                "btrim(body) <> ''");
            table.HasCheckConstraint(
                "ck_linking_prepared_ayah_descriptions_order",
                $"order_value BETWEEN 1 AND {LinkingLimits.MaxDescriptionsPerSourceAyah}");
        });

        builder.HasKey(description => description.Id);
        builder.Property(description => description.Id).ValueGeneratedOnAdd().HasColumnName("id");
        builder.Property(description => description.PreparedAyahId)
            .IsRequired()
            .HasColumnName("prepared_ayah_id");
        builder.Property(description => description.OrderValue).IsRequired().HasColumnName("order_value");
        builder.Property(description => description.Body)
            .IsRequired()
            .HasMaxLength(LinkingLimits.MaxDescriptionLength)
            .HasColumnName("body");

        builder.HasOne<LinkingPreparedAyah>()
            .WithMany()
            .HasForeignKey(description => description.PreparedAyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(description => new { description.PreparedAyahId, description.OrderValue }).IsUnique();
    }
}
