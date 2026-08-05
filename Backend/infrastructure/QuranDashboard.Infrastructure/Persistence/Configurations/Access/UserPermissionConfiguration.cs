using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Access;

public sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("user_permissions");

        builder.HasKey(grant => new { grant.UserId, grant.PermissionId });

        builder.Property(grant => grant.UserId)
            .HasColumnName("user_id");

        builder.Property(grant => grant.PermissionId)
            .HasColumnName("permission_id");

        builder.Property(grant => grant.GrantedByUserId)
            .IsRequired()
            .HasColumnName("granted_by_user_id");

        builder.Property(grant => grant.GrantedAtUtc)
            .IsRequired()
            .HasColumnName("granted_at");

        builder.HasOne(grant => grant.User)
            .WithMany(user => user.UserPermissions)
            .HasForeignKey(grant => grant.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(grant => grant.Permission)
            .WithMany(permission => permission.UserPermissions)
            .HasForeignKey(grant => grant.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(grant => grant.GrantedByUser)
            .WithMany(user => user.GrantedUserPermissions)
            .HasForeignKey(grant => grant.GrantedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(grant => grant.UserId);
        builder.HasIndex(grant => grant.PermissionId);
    }
}
