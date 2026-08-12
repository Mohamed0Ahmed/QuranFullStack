using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Words.Display;
using QuranDashboard.Domain.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingSourceContributionConfiguration : IEntityTypeConfiguration<LinkingSourceContribution>
{
    public void Configure(EntityTypeBuilder<LinkingSourceContribution> builder)
    {
        builder.ToTable("linking_source_contributions", table =>
        {
            table.HasCheckConstraint(
                "ck_linking_source_contributions_source_kind",
                LinkingDescriptorCheckConstraints.TokenIn("source_kind", LinkingSourceKindColumn.Tokens));
            table.HasCheckConstraint(
                "ck_linking_source_contributions_contribution_mode",
                LinkingDescriptorCheckConstraints.TokenIn(
                    "contribution_mode", LinkingOperationTokens.ContributionModeTokens));
            table.HasCheckConstraint(
                "ck_linking_source_contributions_manual_mode_coherence",
                $"""
                (source_kind = '{LinkingSourceKindColumn.Manual}'
                    AND {LinkingDescriptorCheckConstraints.TokenIn(
                        "contribution_mode", LinkingOperationTokens.ManualContributionModeTokens)})
                OR (source_kind <> '{LinkingSourceKindColumn.Manual}'
                    AND contribution_mode = '{LinkingOperationTokens.ToToken(LinkingContributionMode.Automatic)}')
                """);
            table.HasCheckConstraint(
                "ck_linking_source_contributions_scope_schema_version",
                LinkingDescriptorCheckConstraints.JsonbSchemaVersion("scope"));
            table.HasCheckConstraint(
                "ck_linking_source_contributions_kind_reference_coherence",
                LinkingDescriptorCheckConstraints.KindReferenceCoherence);
        });

        builder.HasKey(contribution => contribution.Id);
        builder.Property(contribution => contribution.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(contribution => contribution.OperationId)
            .IsRequired()
            .HasColumnName("operation_id");

        builder.Property(contribution => contribution.DoorId)
            .IsRequired()
            .HasColumnName("door_id");

        builder.Property(contribution => contribution.OrderValue)
            .IsRequired()
            .HasColumnName("order_value");

        builder.Property(contribution => contribution.ContributionMode)
            .IsRequired()
            .HasColumnName("contribution_mode")
            .HasConversion(
                mode => LinkingOperationTokens.ToToken(mode),
                token => ParseContributionMode(token));

        builder.Property(contribution => contribution.SourceKind)
            .IsRequired()
            .HasColumnName("source_kind")
            .HasConversion(
                kind => LinkingSourceKindColumn.ToToken(kind),
                token => LinkingSourceKindColumn.FromToken(token));

        builder.Property(contribution => contribution.SourceIdentity)
            .IsRequired()
            .HasColumnName("source_identity");

        builder.Property(contribution => contribution.SourceIdentityHash)
            .IsRequired()
            .HasColumnName("source_identity_hash");

        builder.Property(contribution => contribution.Label)
            .IsRequired()
            .HasColumnName("label");

        builder.Property(contribution => contribution.ScopeJson)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("scope");

        builder.Property(contribution => contribution.RootId)
            .HasColumnName("root_id");

        builder.Property(contribution => contribution.LemmaId)
            .HasColumnName("lemma_id");

        builder.Property(contribution => contribution.StemId)
            .HasColumnName("stem_id");

        builder.Property(contribution => contribution.UniqueSimpleWordId)
            .HasColumnName("unique_simple_word_id");

        builder.Property(contribution => contribution.UniqueTashkeelWordId)
            .HasColumnName("unique_tashkeel_word_id");

        builder.Property(contribution => contribution.WordTypeTashkeelWordId)
            .HasColumnName("word_type_tashkeel_word_id");

        builder.Property(contribution => contribution.ResolvedAyahCount)
            .IsRequired()
            .HasColumnName("resolved_ayah_count");

        builder.Property(contribution => contribution.ResolvedAtUtc)
            .IsRequired()
            .HasColumnName("resolved_at_utc");

        builder.Property(contribution => contribution.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(contribution => contribution.CreatedBy)
            .IsRequired()
            .HasColumnName("created_by");

        builder.Property(contribution => contribution.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(contribution => contribution.UpdatedBy)
            .IsRequired()
            .HasColumnName("updated_by");

        builder.Property(contribution => contribution.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(contribution => contribution.DeletedBy)
            .HasColumnName("deleted_by");

        builder.Property(contribution => contribution.Version)
            .IsRowVersion();

        builder.HasAlternateKey(contribution => new { contribution.Id, contribution.DoorId });

        builder.HasOne<LinkingOperation>()
            .WithMany()
            .HasForeignKey(contribution => contribution.OperationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(contribution => contribution.DoorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<QuranRoot>()
            .WithMany()
            .HasForeignKey(contribution => contribution.RootId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<QuranLemma>()
            .WithMany()
            .HasForeignKey(contribution => contribution.LemmaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<QuranStem>()
            .WithMany()
            .HasForeignKey(contribution => contribution.StemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UniqueSimpleWord>()
            .WithMany()
            .HasForeignKey(contribution => contribution.UniqueSimpleWordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UniqueTashkeelWord>()
            .WithMany()
            .HasForeignKey(contribution => contribution.UniqueTashkeelWordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UniqueTashkeelWord>()
            .WithMany()
            .HasForeignKey(contribution => contribution.WordTypeTashkeelWordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(contribution => contribution.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(contribution => contribution.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(contribution => contribution.DeletedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(contribution => new { contribution.DoorId, contribution.SourceIdentityHash })
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(contribution => new { contribution.OperationId, contribution.OrderValue });

        builder.HasIndex(contribution => contribution.DoorId)
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(contribution => contribution.RootId)
            .HasFilter("root_id IS NOT NULL");

        builder.HasIndex(contribution => contribution.LemmaId)
            .HasFilter("lemma_id IS NOT NULL");

        builder.HasIndex(contribution => contribution.StemId)
            .HasFilter("stem_id IS NOT NULL");

        builder.HasIndex(contribution => contribution.UniqueSimpleWordId)
            .HasFilter("unique_simple_word_id IS NOT NULL");

        builder.HasIndex(contribution => contribution.UniqueTashkeelWordId)
            .HasFilter("unique_tashkeel_word_id IS NOT NULL");

        builder.HasIndex(contribution => contribution.WordTypeTashkeelWordId)
            .HasFilter("word_type_tashkeel_word_id IS NOT NULL");
    }

    private static LinkingContributionMode ParseContributionMode(string token) =>
        LinkingOperationTokens.TryParseContributionMode(token, out var mode)
            ? mode
            : throw new ArgumentOutOfRangeException(
                nameof(token), token, "Unknown linking contribution mode column value.");
}
