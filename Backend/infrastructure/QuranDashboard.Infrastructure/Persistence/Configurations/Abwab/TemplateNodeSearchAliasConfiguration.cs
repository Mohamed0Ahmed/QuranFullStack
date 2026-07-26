using QuranDashboard.Domain.Abwab.Templates;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class TemplateNodeSearchAliasConfiguration : IEntityTypeConfiguration<TemplateNodeSearchAlias>
{
    public void Configure(EntityTypeBuilder<TemplateNodeSearchAlias> builder)
    {
        builder.ToTable("abwab_template_node_search_aliases");

        builder.HasKey(a => a.TemplateNodeSearchAliasId);
        builder.Property(a => a.TemplateNodeSearchAliasId)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(a => a.TemplateNodeId)
            .IsRequired()
            .HasColumnName("template_node_id");

        builder.Property(a => a.Value)
            .IsRequired()
            .HasColumnName("value");

        builder.Property(a => a.NormalizedValue)
            .IsRequired()
            .HasColumnName("normalized_value");

        builder.Property(a => a.IsDeleted)
            .IsRequired()
            .HasColumnName("is_deleted");

        builder.Property(a => a.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(a => a.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne<TemplateNode>()
            .WithMany()
            .HasForeignKey(a => a.TemplateNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TemplateNodeId, a.NormalizedValue })
            .HasDatabaseName("ix_abwab_template_node_search_aliases_normalized_value")
            .HasFilter("is_deleted = false");
    }
}
