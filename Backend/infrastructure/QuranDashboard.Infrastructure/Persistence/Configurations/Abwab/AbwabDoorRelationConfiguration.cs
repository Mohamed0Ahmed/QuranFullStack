using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class AbwabDoorRelationConfiguration : IEntityTypeConfiguration<AbwabDoorRelation>
{
    public void Configure(EntityTypeBuilder<AbwabDoorRelation> builder)
    {
        builder.ToTable("abwab_door_relations", table =>
        {
            // Canonical ordering and the no-self-relation rule in one constraint: a < b is false for
            // a = b. Applies to the directional type too, which is what makes the opposite direction
            // of an existing pair a duplicate rather than a second legal row.
            table.HasCheckConstraint(
                "CK_abwab_door_relations_canonical_pair",
                "door_a_id < door_b_id");

            // The literal 3 is AbwabRelationType.Comprehensiveness — the enum's members are pinned by
            // this constraint, so reordering them silently changes what it enforces.
            table.HasCheckConstraint(
                "CK_abwab_door_relations_direction",
                "(relation_type = 3) = (broader_door_id IS NOT NULL) "
                + "AND (broader_door_id IS NULL OR broader_door_id IN (door_a_id, door_b_id))");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(r => r.DoorAId)
            .IsRequired()
            .HasColumnName("door_a_id");

        builder.Property(r => r.DoorBId)
            .IsRequired()
            .HasColumnName("door_b_id");

        builder.Property(r => r.RelationType)
            .IsRequired()
            .HasColumnName("relation_type");

        // No FK: the direction CHECK already restricts it to this row's own two endpoints, both of
        // which are FK-backed below.
        builder.Property(r => r.BroaderDoorId)
            .HasColumnName("broader_door_id");

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(r => r.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(r => r.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(r => r.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(r => r.ApprovedAtUtc)
            .HasColumnName("approved_at");

        builder.Property(r => r.ApprovedBy)
            .HasColumnName("approved_by");

        builder.Property(r => r.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(r => r.DeletedBy)
            .HasColumnName("deleted_by");

        // uint + IsRowVersion() maps directly to Postgres's xmin system column — no HasColumnName,
        // since giving it one would make EF treat it as a real column and add it to migrations.
        builder.Property(r => r.Version)
            .IsRowVersion();

        // Restrict on both endpoints, matching AbwabDoor: archive is soft here too, so a
        // hard-delete cascade would be wrong.
        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(r => r.DoorAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(r => r.DoorBId)
            .OnDelete(DeleteBehavior.Restrict);

        // One live relation per (pair, type), direction included — the canonical ordering above is
        // what makes "regardless of direction" structural instead of handler logic. Filtered on the
        // relation's own deleted_at only: a dormant row (an archived endpoint door) still occupies
        // its pair, which is what keeps restore collision-free.
        builder.HasIndex(r => new { r.DoorAId, r.DoorBId, r.RelationType })
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        // A door's relations are read from either side, so both endpoints need their own index; the
        // DoorAId one is covered by the unique index's leading column.
        builder.HasIndex(r => r.DoorBId);
        builder.HasIndex(r => r.DeletedAtUtc);
    }
}
