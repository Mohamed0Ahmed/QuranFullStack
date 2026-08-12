using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingWorkspaceConfiguration : IEntityTypeConfiguration<LinkingWorkspace>
{
    public void Configure(EntityTypeBuilder<LinkingWorkspace> builder)
    {
        builder.ToTable("linking_workspaces");

        builder.HasKey(workspace => workspace.Id);
        builder.Property(workspace => workspace.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(workspace => workspace.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(workspace => workspace.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(workspace => workspace.CreatedBy)
            .IsRequired()
            .HasColumnName("created_by");

        builder.Property(workspace => workspace.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(workspace => workspace.UpdatedBy)
            .IsRequired()
            .HasColumnName("updated_by");

        builder.Property(workspace => workspace.Version)
            .IsRowVersion();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(workspace => workspace.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(workspace => workspace.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(workspace => workspace.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(workspace => workspace.UserId)
            .IsUnique();
    }
}
