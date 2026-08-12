using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Ayahs;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingWorkspaceSourceManualAyahConfiguration
    : IEntityTypeConfiguration<LinkingWorkspaceSourceManualAyah>
{
    public void Configure(EntityTypeBuilder<LinkingWorkspaceSourceManualAyah> builder)
    {
        builder.ToTable("linking_workspace_source_manual_ayahs");

        builder.HasKey(manualAyah => new { manualAyah.WorkspaceSourceId, manualAyah.AyahId });

        builder.Property(manualAyah => manualAyah.WorkspaceSourceId)
            .IsRequired()
            .HasColumnName("workspace_source_id");

        builder.Property(manualAyah => manualAyah.AyahId)
            .IsRequired()
            .HasColumnName("ayah_id");

        builder.Property(manualAyah => manualAyah.OrderValue)
            .IsRequired()
            .HasColumnName("order_value");

        builder.Property(manualAyah => manualAyah.PageHint)
            .HasColumnName("page_hint");

        builder.HasOne<LinkingWorkspaceSource>()
            .WithMany()
            .HasForeignKey(manualAyah => manualAyah.WorkspaceSourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(manualAyah => manualAyah.AyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(manualAyah => new { manualAyah.WorkspaceSourceId, manualAyah.OrderValue });
    }
}
