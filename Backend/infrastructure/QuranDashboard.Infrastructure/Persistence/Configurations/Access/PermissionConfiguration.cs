using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Access;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions", table =>
            table.HasCheckConstraint(
                "ck_permissions_code_format",
                "code ~ '^[a-z0-9]+(\\.[a-z0-9_]+)+$'"));

        builder.HasKey(permission => permission.Id);
        builder.Property(permission => permission.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(permission => permission.Code)
            .IsRequired()
            .HasMaxLength(128)
            .HasColumnName("code");

        builder.Property(permission => permission.ArabicLabel)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("arabic_label");

        builder.Property(permission => permission.EnglishDescription)
            .IsRequired()
            .HasMaxLength(512)
            .HasColumnName("english_description");

        builder.Property(permission => permission.DisplayOrder)
            .IsRequired()
            .HasColumnName("display_order");

        builder.Property(permission => permission.RetiredAtUtc)
            .HasColumnName("retired_at");

        builder.HasIndex(permission => permission.Code).IsUnique();
        builder.HasIndex(permission => permission.DisplayOrder);
    }
}
