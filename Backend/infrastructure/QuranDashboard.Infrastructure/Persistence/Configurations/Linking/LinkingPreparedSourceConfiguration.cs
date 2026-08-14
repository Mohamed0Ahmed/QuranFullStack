using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingPreparedSourceConfiguration : IEntityTypeConfiguration<LinkingPreparedSource>
{
    public void Configure(EntityTypeBuilder<LinkingPreparedSource> builder)
    {
        builder.ToTable("linking_prepared_sources", table =>
        {
            table.HasCheckConstraint("ck_linking_prepared_sources_order", "order_value > 0");
            table.HasCheckConstraint(
                "ck_linking_prepared_sources_resolution_hash",
                LinkingPreparedSchemaConstraints.FixedBinaryHash("resolution_identity_hash"));
            table.HasCheckConstraint(
                "ck_linking_prepared_sources_contribution_hash",
                LinkingPreparedSchemaConstraints.FixedBinaryHash("contribution_identity_hash"));
            table.HasCheckConstraint(
                "ck_linking_prepared_sources_kind",
                LinkingDescriptorCheckConstraints.TokenIn("source_kind", LinkingSourceKindColumn.Tokens));
            table.HasCheckConstraint(
                "ck_linking_prepared_sources_mode",
                LinkingDescriptorCheckConstraints.TokenIn(
                    "contribution_mode", LinkingOperationTokens.ContributionModeTokens));
            table.HasCheckConstraint(
                "ck_linking_prepared_sources_descriptor",
                LinkingPreparedSchemaConstraints.JsonbSchemaVersionMatches(
                    "descriptor_document", "descriptor_schema_version"));
            table.HasCheckConstraint(
                "ck_linking_prepared_sources_configuration",
                LinkingPreparedSchemaConstraints.JsonbSchemaVersionMatches(
                    "configuration_document", "configuration_schema_version"));
            table.HasCheckConstraint(
                "ck_linking_prepared_sources_classification",
                "classification IS NULL OR " + LinkingDescriptorCheckConstraints.TokenIn(
                    "classification", LinkingPreparedSchemaConstraints.ClassificationTokens));
            table.HasCheckConstraint(
                "ck_linking_prepared_sources_counts",
                "(requested_count IS NULL OR requested_count >= 0) "
                + "AND (new_count IS NULL OR new_count >= 0) "
                + "AND (overlapping_count IS NULL OR overlapping_count >= 0) "
                + "AND (unchanged_count IS NULL OR unchanged_count >= 0) "
                + "AND (updated_count IS NULL OR updated_count >= 0) "
                + "AND (removed_count IS NULL OR removed_count >= 0) "
                + "AND (invalid_count IS NULL OR invalid_count >= 0) "
                + "AND (total_ayah_count IS NULL OR total_ayah_count >= 0)");
        });

        builder.HasKey(source => source.Id);
        builder.Property(source => source.Id).ValueGeneratedOnAdd().HasColumnName("id");
        builder.Property(source => source.PreflightId).IsRequired().HasColumnName("preflight_id");
        builder.Property(source => source.OrderValue).IsRequired().HasColumnName("order_value");
        builder.Property(source => source.ResolutionIdentity).IsRequired().HasColumnName("resolution_identity");
        builder.Property(source => source.ResolutionIdentityHash)
            .IsRequired()
            .HasMaxLength(32)
            .IsFixedLength()
            .HasColumnName("resolution_identity_hash");
        builder.Property(source => source.ContributionIdentity)
            .IsRequired()
            .HasColumnName("contribution_identity");
        builder.Property(source => source.ContributionIdentityHash)
            .IsRequired()
            .HasMaxLength(32)
            .IsFixedLength()
            .HasColumnName("contribution_identity_hash");
        builder.Property(source => source.Label).IsRequired().HasColumnName("label");
        builder.Property(source => source.SourceKind)
            .IsRequired()
            .HasColumnName("source_kind")
            .HasConversion(
                kind => LinkingSourceKindColumn.ToToken(kind),
                token => LinkingSourceKindColumn.FromToken(token));
        builder.Property(source => source.ContributionMode)
            .IsRequired()
            .HasColumnName("contribution_mode")
            .HasConversion(
                mode => LinkingOperationTokens.ToToken(mode),
                token => ParseContributionMode(token));
        builder.Property(source => source.DescriptorSchemaVersion)
            .IsRequired()
            .HasColumnName("descriptor_schema_version");
        builder.Property(source => source.DescriptorDocumentJson)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("descriptor_document");
        builder.Property(source => source.ConfigurationSchemaVersion)
            .IsRequired()
            .HasColumnName("configuration_schema_version");
        builder.Property(source => source.ConfigurationDocumentJson)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("configuration_document");
        builder.Property(source => source.WorkspaceSourceId).HasColumnName("workspace_source_id");
        builder.Property(source => source.SourceVersion).HasColumnName("source_version");
        builder.Property(source => source.AutomaticWordMatchesEnabled)
            .HasColumnName("automatic_word_matches_enabled");
        builder.Property(source => source.ExistingContributionId).HasColumnName("existing_contribution_id");
        builder.Property(source => source.ExpectedContributionVersion)
            .HasColumnName("expected_contribution_version");
        builder.Property(source => source.Classification).HasColumnName("classification");
        builder.Property(source => source.RequestedCount).HasColumnName("requested_count");
        builder.Property(source => source.NewCount).HasColumnName("new_count");
        builder.Property(source => source.OverlappingCount).HasColumnName("overlapping_count");
        builder.Property(source => source.UnchangedCount).HasColumnName("unchanged_count");
        builder.Property(source => source.UpdatedCount).HasColumnName("updated_count");
        builder.Property(source => source.RemovedCount).HasColumnName("removed_count");
        builder.Property(source => source.InvalidCount).HasColumnName("invalid_count");
        builder.Property(source => source.TotalAyahCount).HasColumnName("total_ayah_count");

        builder.HasAlternateKey(source => new { source.Id, source.PreflightId });
        builder.HasOne<LinkingPreparedPreflight>()
            .WithMany()
            .HasForeignKey(source => source.PreflightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(source => new { source.PreflightId, source.OrderValue }).IsUnique();
        builder.HasIndex(source => new { source.PreflightId, source.ContributionIdentityHash }).IsUnique();
        builder.HasIndex(source => new { source.PreflightId, source.ResolutionIdentityHash });
    }

    private static LinkingContributionMode ParseContributionMode(string token) =>
        LinkingOperationTokens.TryParseContributionMode(token, out var mode)
            ? mode
            : throw new ArgumentOutOfRangeException(
                nameof(token), token, "Unknown linking contribution mode column value.");
}
