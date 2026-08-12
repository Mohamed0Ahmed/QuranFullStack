using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingWorkspaceSourceWordConfiguration
    : IEntityTypeConfiguration<LinkingWorkspaceSourceWord>
{
    public void Configure(EntityTypeBuilder<LinkingWorkspaceSourceWord> builder)
    {
        builder.ToTable("linking_workspace_source_words");

        builder.HasKey(word => new { word.WorkspaceSourceId, word.QuranWordId });

        builder.Property(word => word.WorkspaceSourceId)
            .IsRequired()
            .HasColumnName("workspace_source_id");

        builder.Property(word => word.QuranWordId)
            .IsRequired()
            .HasColumnName("quran_word_id");

        builder.Property(word => word.AyahId)
            .IsRequired()
            .HasColumnName("ayah_id");

        builder.HasOne<LinkingWorkspaceSource>()
            .WithMany()
            .HasForeignKey(word => word.WorkspaceSourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<QuranWord>()
            .WithMany()
            .HasForeignKey(word => word.QuranWordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(word => word.AyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(word => new { word.WorkspaceSourceId, word.AyahId });
    }
}
