using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Words.Display;
using QuranDashboard.Domain.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingWorkspaceSourceConfiguration : IEntityTypeConfiguration<LinkingWorkspaceSource>
{
    public void Configure(EntityTypeBuilder<LinkingWorkspaceSource> builder)
    {
        builder.ToTable("linking_workspace_sources", table =>
        {
            table.HasCheckConstraint(
                "ck_linking_workspace_sources_source_kind",
                LinkingDescriptorCheckConstraints.TokenIn("source_kind", LinkingSourceKindColumn.Tokens));
            table.HasCheckConstraint(
                "ck_linking_workspace_sources_inclusion_mode",
                LinkingDescriptorCheckConstraints.TokenIn(
                    "inclusion_mode", LinkingWorkspaceTokens.InclusionModeTokens));
            table.HasCheckConstraint(
                "ck_linking_workspace_sources_manual_link_shape",
                "manual_link_shape IS NULL OR "
                + LinkingDescriptorCheckConstraints.TokenIn(
                    "manual_link_shape", LinkingWorkspaceTokens.ManualLinkShapeTokens));
            table.HasCheckConstraint(
                "ck_linking_workspace_sources_scope_schema_version",
                LinkingDescriptorCheckConstraints.JsonbSchemaVersion("scope"));
            table.HasCheckConstraint(
                "ck_linking_workspace_sources_kind_configuration_coherence",
                $"""
                (source_kind = '{LinkingSourceKindColumn.Manual}'
                    AND automatic_word_matches_enabled IS NULL
                    AND manual_link_shape IS NOT NULL)
                OR (source_kind <> '{LinkingSourceKindColumn.Manual}'
                    AND automatic_word_matches_enabled IS NOT NULL
                    AND manual_link_shape IS NULL)
                """);
            table.HasCheckConstraint(
                "ck_linking_workspace_sources_kind_reference_coherence",
                LinkingDescriptorCheckConstraints.KindReferenceCoherence);
        });

        builder.HasKey(source => source.Id);
        builder.Property(source => source.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(source => source.WorkspaceId)
            .IsRequired()
            .HasColumnName("workspace_id");

        builder.Property(source => source.OrderValue)
            .IsRequired()
            .HasColumnName("order_value");

        builder.Property(source => source.SourceKind)
            .IsRequired()
            .HasColumnName("source_kind")
            .HasConversion(
                kind => LinkingSourceKindColumn.ToToken(kind),
                token => LinkingSourceKindColumn.FromToken(token));

        builder.Property(source => source.SourceIdentity)
            .IsRequired()
            .HasColumnName("source_identity");

        builder.Property(source => source.SourceIdentityHash)
            .IsRequired()
            .HasColumnName("source_identity_hash");

        builder.Property(source => source.Label)
            .IsRequired()
            .HasColumnName("label");

        builder.Property(source => source.ScopeJson)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("scope");

        builder.Property(source => source.RootId)
            .HasColumnName("root_id");

        builder.Property(source => source.LemmaId)
            .HasColumnName("lemma_id");

        builder.Property(source => source.StemId)
            .HasColumnName("stem_id");

        builder.Property(source => source.UniqueSimpleWordId)
            .HasColumnName("unique_simple_word_id");

        builder.Property(source => source.UniqueTashkeelWordId)
            .HasColumnName("unique_tashkeel_word_id");

        builder.Property(source => source.WordTypeTashkeelWordId)
            .HasColumnName("word_type_tashkeel_word_id");

        builder.Property(source => source.InclusionMode)
            .IsRequired()
            .HasColumnName("inclusion_mode")
            .HasConversion(
                mode => LinkingWorkspaceTokens.ToToken(mode),
                token => Parse(token));

        builder.Property(source => source.AutomaticWordMatchesEnabled)
            .HasColumnName("automatic_word_matches_enabled");

        builder.Property(source => source.ManualLinkShape)
            .HasColumnName("manual_link_shape")
            .HasConversion(
                shape => shape.HasValue ? LinkingWorkspaceTokens.ToToken(shape.Value) : null,
                token => ParseShape(token));

        builder.Property(source => source.LastResolvedCount)
            .HasColumnName("last_resolved_count");

        builder.Property(source => source.LastResolvedAtUtc)
            .HasColumnName("last_resolved_at_utc");

        builder.Property(source => source.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(source => source.CreatedBy)
            .IsRequired()
            .HasColumnName("created_by");

        builder.Property(source => source.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(source => source.UpdatedBy)
            .IsRequired()
            .HasColumnName("updated_by");

        builder.Property(source => source.Version)
            .IsRowVersion();

        builder.HasOne<LinkingWorkspace>()
            .WithMany()
            .HasForeignKey(source => source.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<QuranRoot>()
            .WithMany()
            .HasForeignKey(source => source.RootId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<QuranLemma>()
            .WithMany()
            .HasForeignKey(source => source.LemmaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<QuranStem>()
            .WithMany()
            .HasForeignKey(source => source.StemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UniqueSimpleWord>()
            .WithMany()
            .HasForeignKey(source => source.UniqueSimpleWordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UniqueTashkeelWord>()
            .WithMany()
            .HasForeignKey(source => source.UniqueTashkeelWordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UniqueTashkeelWord>()
            .WithMany()
            .HasForeignKey(source => source.WordTypeTashkeelWordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(source => source.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(source => source.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(source => new { source.WorkspaceId, source.SourceIdentityHash })
            .IsUnique();

        builder.HasIndex(source => new { source.WorkspaceId, source.OrderValue });
    }

    private static LinkingInclusionMode Parse(string token) =>
        LinkingWorkspaceTokens.TryParseInclusionMode(token, out var mode)
            ? mode
            : throw new ArgumentOutOfRangeException(nameof(token), token, "Unknown linking inclusion mode column value.");

    private static LinkingManualLinkShape? ParseShape(string? token)
    {
        if (token is null)
        {
            return null;
        }

        return LinkingWorkspaceTokens.TryParseManualLinkShape(token, out var shape)
            ? shape
            : throw new ArgumentOutOfRangeException(nameof(token), token, "Unknown linking manual link shape column value.");
    }
}
