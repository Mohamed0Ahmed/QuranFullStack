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

        builder.HasData(new Role { Id = 1, Name = RoleNames.Owner, DisplayName = "المالك" });
    }
}
