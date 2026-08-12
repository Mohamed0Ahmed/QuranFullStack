using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Ayahs;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingWorkspaceSourceAyahOverrideConfiguration
    : IEntityTypeConfiguration<LinkingWorkspaceSourceAyahOverride>
{
    public void Configure(EntityTypeBuilder<LinkingWorkspaceSourceAyahOverride> builder)
    {
        builder.ToTable("linking_workspace_source_ayah_overrides");

        builder.HasKey(ayahOverride => new { ayahOverride.WorkspaceSourceId, ayahOverride.AyahId });

        builder.Property(ayahOverride => ayahOverride.WorkspaceSourceId)
            .IsRequired()
            .HasColumnName("workspace_source_id");

        builder.Property(ayahOverride => ayahOverride.AyahId)
            .IsRequired()
            .HasColumnName("ayah_id");

        builder.HasOne<LinkingWorkspaceSource>()
            .WithMany()
            .HasForeignKey(ayahOverride => ayahOverride.WorkspaceSourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(ayahOverride => ayahOverride.AyahId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
