using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Access;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(u => u.LogtoSub)
            .IsRequired()
            .HasColumnName("logto_sub");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasColumnName("email");

        builder.Property(u => u.NormalizedEmail)
            .IsRequired()
            .HasColumnName("normalized_email");

        builder.Property(u => u.UserName)
            .HasColumnName("user_name");

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name");

        builder.Property(u => u.Title)
            .HasColumnName("title");

        builder.Property(u => u.Version)
            .IsRowVersion()
            .HasColumnName("xmin");

        builder.Property(u => u.RoleId)
            .HasColumnName("role_id");

        builder.HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(u => u.Status)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(u => u.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.HasIndex(u => u.LogtoSub).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.NormalizedEmail).IsUnique();
        builder.HasIndex(u => new { u.Status, u.Id });
    }
}
