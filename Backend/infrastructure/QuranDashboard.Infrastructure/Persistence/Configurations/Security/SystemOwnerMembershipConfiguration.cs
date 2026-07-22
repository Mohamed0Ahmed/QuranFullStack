using QuranDashboard.Domain.Security.Owners;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Security;

public sealed class SystemOwnerMembershipConfiguration : IEntityTypeConfiguration<SystemOwnerMembership>
{
    public void Configure(EntityTypeBuilder<SystemOwnerMembership> builder)
    {
        builder.ToTable("system_owner_memberships");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(o => o.Issuer).IsRequired().HasColumnName("issuer");
        builder.Property(o => o.Subject).IsRequired().HasColumnName("subject");
        builder.Property(o => o.IsActive).IsRequired().HasColumnName("is_active");
        builder.Property(o => o.IsAccountEnabled).IsRequired().HasColumnName("is_account_enabled");
        builder.Property(o => o.CreatedAtUtc).IsRequired().HasColumnName("created_at");
        builder.Property(o => o.DeactivatedAtUtc).HasColumnName("deactivated_at");

        // Immutable composite identity: at most one membership row per (issuer, subject).
        builder.HasIndex(o => new { o.Issuer, o.Subject }).IsUnique();

        builder.Ignore(o => o.Identity);
        builder.Ignore(o => o.IsActiveOwner);
    }
}
