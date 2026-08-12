using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Ayahs;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingWorkspaceSourceDescriptionConfiguration
    : IEntityTypeConfiguration<LinkingWorkspaceSourceDescription>
{
    public void Configure(EntityTypeBuilder<LinkingWorkspaceSourceDescription> builder)
    {
        builder.ToTable("linking_workspace_source_descriptions", table =>
        {
            table.HasCheckConstraint(
                "ck_linking_workspace_source_descriptions_body_not_blank",
                "btrim(body) <> ''");
            table.HasCheckConstraint(
                "ck_linking_workspace_source_descriptions_order_value",
                $"order_value BETWEEN 1 AND {LinkingLimits.MaxDescriptionsPerSourceAyah}");
        });

        builder.HasKey(description => description.Id);
        builder.Property(description => description.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(description => description.WorkspaceSourceId)
            .IsRequired()
            .HasColumnName("workspace_source_id");

        builder.Property(description => description.AyahId)
            .IsRequired()
            .HasColumnName("ayah_id");

        builder.Property(description => description.OrderValue)
            .IsRequired()
            .HasColumnName("order_value");

        builder.Property(description => description.Body)
            .IsRequired()
            .HasMaxLength(LinkingLimits.MaxDescriptionLength)
            .HasColumnName("body");

        builder.Property(description => description.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(description => description.CreatedBy)
            .IsRequired()
            .HasColumnName("created_by");

        builder.Property(description => description.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(description => description.UpdatedBy)
            .IsRequired()
            .HasColumnName("updated_by");

        builder.Property(description => description.Version)
            .IsRowVersion();

        builder.HasOne<LinkingWorkspaceSource>()
            .WithMany()
            .HasForeignKey(description => description.WorkspaceSourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(description => description.AyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(description => description.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(description => description.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(description => new
        {
            description.WorkspaceSourceId,
            description.AyahId,
            description.OrderValue,
        })
            .IsUnique();
    }
}
