using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class ManualProtectionConfiguration : IEntityTypeConfiguration<ManualProtection>
{
    public void Configure(EntityTypeBuilder<ManualProtection> builder)
    {
        builder.ToTable("abwab_manual_protections");

        builder.HasKey(p => p.ManualProtectionId);
        builder.Property(p => p.ManualProtectionId)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(p => p.CategoryId)
            .IsRequired()
            .HasColumnName("category_id");

        builder.Property(p => p.ProtectionType)
            .IsRequired()
            .HasColumnName("protection_type");

        builder.Property(p => p.ProtectionScope)
            .IsRequired()
            .HasColumnName("protection_scope");

        builder.Property(p => p.AppliedByActorSubject)
            .IsRequired()
            .HasColumnName("applied_by_actor_subject");

        builder.Property(p => p.AppliedAtUtc)
            .IsRequired()
            .HasColumnName("applied_at");

        builder.Property(p => p.LiftedByActorSubject)
            .HasColumnName("lifted_by_actor_subject");

        builder.Property(p => p.LiftedAtUtc)
            .HasColumnName("lifted_at");

        builder.Property(p => p.IsDeleted)
            .IsRequired()
            .HasColumnName("is_deleted");

        builder.Property(p => p.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(p => p.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // One active row per (CategoryId, ProtectionType) is the race-safe apply/lift guard (§7.2); ProtectionScope
        // is a plain column on that one row, never a second row for a scope change.
        builder.HasIndex(p => new { p.CategoryId, p.ProtectionType })
            .IsUnique()
            .HasDatabaseName("ix_abwab_manual_protections_active_category_type")
            .HasFilter("is_deleted = false");
    }
}
