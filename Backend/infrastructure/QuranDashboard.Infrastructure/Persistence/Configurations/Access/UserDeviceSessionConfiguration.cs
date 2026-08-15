using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Access;

public sealed class UserDeviceSessionConfiguration : IEntityTypeConfiguration<UserDeviceSession>
{
    public void Configure(EntityTypeBuilder<UserDeviceSession> builder)
    {
        builder.ToTable("user_device_sessions", table =>
        {
            table.HasCheckConstraint(
                "ck_user_device_sessions_expiry",
                "expires_at > created_at");
            table.HasCheckConstraint(
                "ck_user_device_sessions_revocation",
                "revoked_at IS NULL OR revoked_at >= created_at");
        });

        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(session => session.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(session => session.TokenHash)
            .IsRequired()
            .HasMaxLength(64)
            .HasColumnName("token_hash");

        builder.Property(session => session.CsrfTokenHash)
            .IsRequired()
            .HasMaxLength(64)
            .HasColumnName("csrf_token_hash");

        builder.Property(session => session.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(session => session.ExpiresAtUtc)
            .IsRequired()
            .HasColumnName("expires_at");

        builder.Property(session => session.RevokedAtUtc)
            .HasColumnName("revoked_at");

        builder.HasOne(session => session.User)
            .WithMany(user => user.DeviceSessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(session => session.TokenHash).IsUnique();
        builder.HasIndex(session => new { session.UserId, session.ExpiresAtUtc });
        builder.HasIndex(session => new { session.RevokedAtUtc, session.ExpiresAtUtc });
    }
}
