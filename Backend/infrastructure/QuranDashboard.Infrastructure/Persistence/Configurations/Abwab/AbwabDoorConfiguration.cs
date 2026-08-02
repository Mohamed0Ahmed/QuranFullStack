using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class AbwabDoorConfiguration : IEntityTypeConfiguration<AbwabDoor>
{
    public void Configure(EntityTypeBuilder<AbwabDoor> builder)
    {
        builder.ToTable("abwab_doors");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(d => d.SectionId)
            .IsRequired()
            .HasColumnName("section_id");

        builder.Property(d => d.ParentId)
            .HasColumnName("parent_id");

        builder.Property(d => d.Name)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(d => d.Description)
            .HasColumnName("description");

        builder.Property(d => d.RepresentativeAyahText)
            .HasColumnName("representative_ayah_text");

        builder.Property(d => d.OrderValue)
            .IsRequired()
            .HasColumnName("order_value");

        builder.Property(d => d.GlobalOrderValue)
            .HasColumnName("global_order_value");

        builder.Property(d => d.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(d => d.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(d => d.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(d => d.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(d => d.ApprovedAtUtc)
            .HasColumnName("approved_at");

        builder.Property(d => d.ApprovedBy)
            .HasColumnName("approved_by");

        builder.Property(d => d.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(d => d.DeletedBy)
            .HasColumnName("deleted_by");

        // uint + IsRowVersion() maps directly to Postgres's xmin system column — no HasColumnName,
        // since giving it one would make EF treat it as a real column and add it to migrations.
        builder.Property(d => d.Version)
            .IsRowVersion();

        // Restrict, not Cascade: archive is soft, so a hard-delete cascade would be wrong here.
        builder.HasOne<AbwabSection>()
            .WithMany()
            .HasForeignKey(d => d.SectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(d => d.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.SectionId, d.ParentId, d.OrderValue });
        builder.HasIndex(d => d.ParentId);
        builder.HasIndex(d => d.DeletedAtUtc);

        // Backs the superset's ORDER BY and every ResequenceGlobal read. No UNIQUE: renumbering
        // issues one UPDATE per row and a unique index is checked per statement, so 1..N
        // resequencing would transiently violate it (plan §6 — same reasoning as order_value).
        builder.HasIndex(d => d.GlobalOrderValue)
            .HasFilter("parent_id IS NULL AND deleted_at IS NULL");

        // NULLS NOT DISTINCT: the naive UNIQUE (parent_id, name) does not constrain root doors,
        // since Postgres NULLs never collide in a unique index by default — two root doors named
        // «العلم بالله» would both insert cleanly. Requires PostgreSQL 15+ (target is postgres:16).
        builder.HasIndex(d => new { d.SectionId, d.ParentId, d.Name })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter("deleted_at IS NULL");
    }
}
