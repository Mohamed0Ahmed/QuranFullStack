using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Security;

public sealed class PermissionCodeConfiguration : IEntityTypeConfiguration<PermissionCode>
{
    public void Configure(EntityTypeBuilder<PermissionCode> builder)
    {
        builder.ToTable("permission_codes");

        builder.HasKey(c => c.Code);
        builder.Property(c => c.Code).ValueGeneratedNever().HasColumnName("code");
        builder.Property(c => c.SystemOwnerOnly).IsRequired().HasColumnName("system_owner_only");
        builder.Property(c => c.DashboardAdminBaseline).IsRequired().HasColumnName("dashboard_admin_baseline");
        builder.Ignore(c => c.IsAssignable);

        builder.HasData(PermissionCatalogue.All);
    }
}
