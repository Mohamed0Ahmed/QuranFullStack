using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Access;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(64)
            .HasColumnName("name");

        builder.Property(r => r.DisplayName)
            .IsRequired()
            .HasMaxLength(128)
            .HasColumnName("display_name");

        builder.HasIndex(r => r.Name).IsUnique();

        // Fixed, seeded role set. Capabilities are enforced in code keyed by Name; the Arabic
        // DisplayName is the user-facing label. Ids are pinned to keep the FK/seed stable.
        builder.HasData(
            new Role { Id = 1, Name = RoleNames.Owner, DisplayName = "المالك" },
            new Role { Id = 2, Name = RoleNames.Admin, DisplayName = "المشرف" },
            new Role { Id = 3, Name = RoleNames.Editor, DisplayName = "المحرر" });
    }
}
