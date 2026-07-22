using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Security;

// The seeded `permission_codes` table — the "seed" layer of the 5-catalogue parity check. Seeded directly
// from the single canonical PermissionCatalogue, so the seed can never drift from the in-memory catalogue.
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
