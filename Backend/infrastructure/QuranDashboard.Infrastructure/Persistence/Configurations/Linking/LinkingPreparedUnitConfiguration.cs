using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingPreparedUnitConfiguration : IEntityTypeConfiguration<LinkingPreparedUnit>
{
    public void Configure(EntityTypeBuilder<LinkingPreparedUnit> builder)
    {
        builder.ToTable("linking_prepared_units", table =>
        {
            table.HasCheckConstraint("ck_linking_prepared_units_order", "order_value > 0");
            table.HasCheckConstraint(
                "ck_linking_prepared_units_identity_hash",
                LinkingPreparedSchemaConstraints.FixedBinaryHash("unit_identity_hash"));
        });

        builder.HasKey(unit => unit.Id);
        builder.Property(unit => unit.Id).ValueGeneratedOnAdd().HasColumnName("id");
        builder.Property(unit => unit.PreflightId).IsRequired().HasColumnName("preflight_id");
        builder.Property(unit => unit.SourceId).IsRequired().HasColumnName("source_id");
        builder.Property(unit => unit.OrderValue).IsRequired().HasColumnName("order_value");
        builder.Property(unit => unit.UnitIdentity).IsRequired().HasColumnName("unit_identity");
        builder.Property(unit => unit.UnitIdentityHash)
            .IsRequired()
            .HasMaxLength(32)
            .IsFixedLength()
            .HasColumnName("unit_identity_hash");
        builder.Property(unit => unit.IsGrouped).IsRequired().HasColumnName("is_grouped");

        builder.HasAlternateKey(unit => new { unit.Id, unit.SourceId, unit.PreflightId });
        builder.HasOne<LinkingPreparedSource>()
            .WithMany()
            .HasForeignKey(unit => new { unit.SourceId, unit.PreflightId })
            .HasPrincipalKey(source => new { source.Id, source.PreflightId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(unit => new { unit.SourceId, unit.OrderValue }).IsUnique();
        builder.HasIndex(unit => new { unit.SourceId, unit.UnitIdentityHash });
    }
}
