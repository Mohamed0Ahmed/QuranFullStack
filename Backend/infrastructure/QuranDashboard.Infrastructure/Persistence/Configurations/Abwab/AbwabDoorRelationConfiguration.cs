using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class AbwabDoorRelationConfiguration : IEntityTypeConfiguration<AbwabDoorRelation>
{
    public void Configure(EntityTypeBuilder<AbwabDoorRelation> builder)
    {
        builder.ToTable("abwab_door_relations", table =>
        {
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

        builder.Property(r => r.Version)
            .IsRowVersion();

        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(r => r.DoorAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(r => r.DoorBId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.DoorAId, r.DoorBId, r.RelationType })
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(r => r.DoorBId);
        builder.HasIndex(r => r.DeletedAtUtc);
    }
}
