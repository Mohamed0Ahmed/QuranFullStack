using QuranDashboard.Domain.Abwab.Templates;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class DoorTemplateConfiguration : IEntityTypeConfiguration<DoorTemplate>
{
    public void Configure(EntityTypeBuilder<DoorTemplate> builder)
    {
        builder.ToTable("abwab_door_templates");

        builder.HasKey(t => t.DoorTemplateId);
        builder.Property(t => t.DoorTemplateId)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(t => t.Name)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(t => t.NormalizedName)
            .IsRequired()
            .HasColumnName("normalized_name");

        builder.Property(t => t.Description)
            .HasColumnName("description");

        builder.Property(t => t.TemplateRevision)
            .IsRequired()
            .HasColumnName("template_revision");

        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasColumnName("is_deleted");

        builder.Property(t => t.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(t => t.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // A lookup index only: §7.4 defines no template-name uniqueness rule, and §11 owns no
        // conflict code for one, so no unique constraint may be invented here.
        builder.HasIndex(t => t.NormalizedName)
            .HasDatabaseName("ix_abwab_door_templates_normalized_name")
            .HasFilter("is_deleted = false");
    }
}
