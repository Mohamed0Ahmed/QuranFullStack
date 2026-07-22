using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Security;

public sealed class PermissionAssignmentConfiguration : IEntityTypeConfiguration<PermissionAssignment>
{
    public void Configure(EntityTypeBuilder<PermissionAssignment> builder)
    {
        builder.ToTable("permission_assignments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(a => a.TargetKind).IsRequired().HasColumnName("target_kind");
        builder.Property(a => a.TargetKey).IsRequired().HasColumnName("target_key");
        builder.Property(a => a.PermissionCode).IsRequired().HasColumnName("permission_code");
        builder.Property(a => a.IsGranted).IsRequired().HasColumnName("is_granted");
        builder.Property(a => a.Version).IsRequired().HasColumnName("version");
        builder.Property(a => a.CreatedAtUtc).IsRequired().HasColumnName("created_at");
        builder.Property(a => a.UpdatedAtUtc).IsRequired().HasColumnName("updated_at");

        // Uniquely keyed (target-kind, target-key, permission-code): first-grant serialization relies on it.
        builder.HasIndex(a => new { a.TargetKind, a.TargetKey, a.PermissionCode }).IsUnique();
    }
}
