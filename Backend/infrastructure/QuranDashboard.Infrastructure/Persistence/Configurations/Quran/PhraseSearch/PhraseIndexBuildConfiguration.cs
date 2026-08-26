using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.PhraseSearch;

public sealed class PhraseIndexBuildConfiguration : IEntityTypeConfiguration<PhraseIndexBuild>
{
    public void Configure(EntityTypeBuilder<PhraseIndexBuild> builder)
    {
        builder.ToTable("quran_phrase_index_builds", table =>
        {
            table.HasCheckConstraint(
                "ck_quran_phrase_index_builds_status",
                "status IN (1, 2, 3, 4, 5)");
            table.HasCheckConstraint(
                "ck_quran_phrase_index_builds_format_version",
                "format_version > 0");
            table.HasCheckConstraint(
                "ck_quran_phrase_index_builds_source_revision",
                "source_revision > 0");
            table.HasCheckConstraint(
                "ck_quran_phrase_index_builds_source_fingerprint",
                "source_fingerprint ~ '^[0-9a-f]{64}$'");
            table.HasCheckConstraint(
                "ck_quran_phrase_index_builds_totals",
                "search_token_count >= 0 AND variant_count >= 0 AND occurrence_count >= 0 "
                + "AND similarity_edge_count >= 0 AND similarity_anchor_stat_count >= 0");
            table.HasCheckConstraint(
                "ck_quran_phrase_index_builds_active_readiness",
                "status <> 3 OR (exact_ready AND similarity_ready)");
        });

        builder.HasKey(build => build.Id);

        builder.Property(build => build.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(build => build.Status)
            .IsRequired()
            .HasConversion<short>()
            .HasColumnType("smallint")
            .HasColumnName("status");

        builder.Property(build => build.FormatVersion)
            .IsRequired()
            .HasColumnName("format_version");

        builder.Property(build => build.ExactReady)
            .IsRequired()
            .HasColumnName("exact_ready");

        builder.Property(build => build.SimilarityReady)
            .IsRequired()
            .HasColumnName("similarity_ready");

        builder.Property(build => build.BuilderVersion)
            .IsRequired()
            .HasMaxLength(64)
            .HasColumnName("builder_version");

        builder.Property(build => build.SourceRevision)
            .IsRequired()
            .HasColumnName("source_revision");

        builder.Property(build => build.SourceFingerprint)
            .IsRequired()
            .HasMaxLength(64)
            .HasColumnName("source_fingerprint");

        builder.Property(build => build.StartedAtUtc)
            .IsRequired()
            .HasColumnName("started_at_utc");

        builder.Property(build => build.ValidatedAtUtc)
            .HasColumnName("validated_at_utc");

        builder.Property(build => build.ActivatedAtUtc)
            .HasColumnName("activated_at_utc");

        builder.Property(build => build.FailedAtUtc)
            .HasColumnName("failed_at_utc");

        builder.Property(build => build.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.Property(build => build.SearchTokenCount)
            .IsRequired()
            .HasColumnName("search_token_count");

        builder.Property(build => build.VariantCount)
            .IsRequired()
            .HasColumnName("variant_count");

        builder.Property(build => build.OccurrenceCount)
            .IsRequired()
            .HasColumnName("occurrence_count");

        builder.Property(build => build.SimilarityEdgeCount)
            .IsRequired()
            .HasColumnName("similarity_edge_count");

        builder.Property(build => build.SimilarityAnchorStatCount)
            .IsRequired()
            .HasColumnName("similarity_anchor_stat_count");

        builder.Property(build => build.ValidationVerdict)
            .HasMaxLength(32)
            .HasColumnName("validation_verdict");

        builder.Property(build => build.ReportPath)
            .HasMaxLength(1024)
            .HasColumnName("report_path");

        builder.Property(build => build.FailureSummary)
            .HasMaxLength(2000)
            .HasColumnName("failure_summary");

        builder.HasIndex(build => build.Status)
            .IsUnique()
            .HasDatabaseName("ux_quran_phrase_index_builds_active")
            .HasFilter("status = 3");
    }
}
