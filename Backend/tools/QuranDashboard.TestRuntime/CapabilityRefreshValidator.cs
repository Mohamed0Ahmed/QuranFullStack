using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;
using NpgsqlTypes;
using QuranDashboard.Application.Abstractions.Security.Permissions;

namespace QuranDashboard.TestRuntime;

internal sealed class CapabilityRefreshValidator : ICapabilityRefreshValidator
{
    private readonly TestDatabaseRefreshOracles oracles;

    internal CapabilityRefreshValidator(string oraclePath)
    {
        using var stream = File.OpenRead(oraclePath);
        oracles = JsonSerializer.Deserialize<TestDatabaseRefreshOracles>(
            stream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            }) ?? throw new JsonException("The Test Database refresh oracle file was empty.");
        if (oracles.Format != "quran-dashboard-test-database-refresh-oracles"
            || oracles.Version != 1
            || !IsSha256(oracles.Quran.Sha256)
            || oracles.Quran.SurahNumber <= 0
            || oracles.Quran.RowCount <= 0
            || oracles.PhraseSearch.Mode != "simple"
            || oracles.PhraseSearch.ExactVerseKeys.Length == 0
            || oracles.PhraseSearch.DifferencePositions.Length == 0
            || oracles.PhraseSearch.EvidenceSha256.Length == 0
            || oracles.PhraseSearch.EvidenceSha256.Any(hash => !IsSha256(hash)))
        {
            throw new JsonException("The Test Database refresh oracle contract is invalid.");
        }
    }

    private static readonly IReadOnlyDictionary<string, long> ExactCanonicalCounts =
        new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["quran_surahs"] = 114,
            ["quran_ayahs"] = 6236,
            ["quran_mushaf_pages"] = 604,
            ["quran_mushaf_lines"] = 9046,
            ["quran_words"] = 83668,
            ["quran_words_ordered_tashkeel"] = 77432,
            ["quran_words_ordered_simple"] = 77432,
            ["quran_word_morphology"] = 77432,
            ["quran_word_morphology_segments"] = 128219,
            ["quran_roots"] = 1642,
            ["quran_lemmas"] = 4817,
            ["quran_lemma_analyses"] = 4832,
            ["quran_stems"] = 11843,
            ["quran_pos_tags"] = 49,
            ["quran_i3rab_rules"] = 142,
            ["quran_mutashabihat_groups"] = 814,
            ["quran_mutashabihat_occurrences"] = 3557,
            ["quran_similar_ayah_links"] = 3552,
            ["quran_juzs"] = 30,
            ["quran_hizbs"] = 60,
            ["quran_rubs"] = 240,
            ["quran_sajdas"] = 15,
            ["quran_full_i3rab_sources"] = 4,
            ["quran_full_i3rab_entries"] = 14513,
            ["quran_full_i3rab_ayah_entries"] = 24944,
            ["quran_tafsir_sources"] = 10,
            ["quran_translation_sources"] = 10,
        };

    public async Task<CapabilityRefreshValidation> ValidateAsync(
        DatabaseContract contract,
        ContractValidationResult contractValidation,
        string connectionString,
        string selectedLogin,
        IReadOnlyDictionary<string, string>? requiredMarkers,
        CancellationToken cancellationToken)
    {
        var violations = new List<ContractViolation>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await ExecuteAsync(connection, transaction, "SET TRANSACTION READ ONLY", cancellationToken);

        await ValidateServerAsync(connection, transaction, contract, violations, cancellationToken);
        var liveTables = await ReadNamesAsync(
            connection,
            transaction,
            "SELECT tablename FROM pg_catalog.pg_tables WHERE schemaname = 'public' ORDER BY tablename",
            cancellationToken);
        ValidateTableClassification(contract, liveTables, violations);
        await ValidateMigrationsAsync(
            connection,
            transaction,
            contractValidation.ExpectedMigrations,
            liveTables,
            violations,
            cancellationToken);
        await ValidateExtensionsAsync(connection, transaction, violations, cancellationToken);
        await ValidateCanonicalCountsAsync(connection, transaction, liveTables, violations, cancellationToken);
        await ValidateCanonicalInvariantsAsync(
            connection, transaction, liveTables, oracles, violations, cancellationToken);
        await ValidateSystemCatalogueAsync(connection, transaction, contract, liveTables, violations, cancellationToken);
        await ValidateMutableBaselineAsync(connection, transaction, contract, liveTables, violations, cancellationToken);
        if (requiredMarkers is not null)
        {
            await ValidateMarkersAsync(connection, transaction, contract, requiredMarkers, violations, cancellationToken);
            await ValidateRestrictedRolesAsync(
                connection, transaction, contract, selectedLogin, liveTables, violations, cancellationToken);
        }

        string? canonicalFingerprint = null;
        string? catalogueFingerprint = null;
        string? schemaFingerprint = null;
        if (!contract.DataClasses.CanonicalQuranData.Except(liveTables, StringComparer.Ordinal).Any())
        {
            canonicalFingerprint = await FingerprintTablesAsync(
                connection,
                transaction,
                contract.DataClasses.CanonicalQuranData,
                cancellationToken);
        }

        if (!contract.DataClasses.SystemCatalogue.Except(liveTables, StringComparer.Ordinal).Any())
        {
            catalogueFingerprint = await FingerprintTablesAsync(
                connection,
                transaction,
                contract.DataClasses.SystemCatalogue,
                cancellationToken);
        }

        if (liveTables.Contains("__EFMigrationsHistory"))
        {
            schemaFingerprint = await FingerprintSchemaStateAsync(
                connection,
                transaction,
                contract,
                cancellationToken);
        }

        var protectedFingerprint = canonicalFingerprint is null
                                   || catalogueFingerprint is null
                                   || schemaFingerprint is null
            ? null
            : Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
                $"canonical:{canonicalFingerprint}\ncatalogue:{catalogueFingerprint}\nschema:{schemaFingerprint}\n")));

        if (requiredMarkers is not null)
        {
            await ValidateRefreshMetadataAsync(
                connection,
                transaction,
                contract,
                canonicalFingerprint,
                catalogueFingerprint,
                protectedFingerprint,
                violations,
                cancellationToken);
        }

        await transaction.RollbackAsync(cancellationToken);
        var ordered = Order(violations);
        return new CapabilityRefreshValidation(
            ordered.Count == 0,
            canonicalFingerprint,
            catalogueFingerprint,
            schemaFingerprint,
            protectedFingerprint,
            ordered);
    }

    private static async Task ValidateServerAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        ICollection<ContractViolation> violations,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT current_setting('server_version_num')::integer / 10000, pg_is_in_recovery()",
            connection,
            transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        if (reader.GetInt32(0) != contract.PostgresMajorVersion)
        {
            violations.Add(new ContractViolation("refresh.validation.postgres-version"));
        }

        if (reader.GetBoolean(1))
        {
            violations.Add(new ContractViolation("refresh.validation.in-recovery"));
        }
    }

    private static void ValidateTableClassification(
        DatabaseContract contract,
        IReadOnlySet<string> liveTables,
        ICollection<ContractViolation> violations)
    {
        var expected = contract.AllTables().Select(entry => entry.Table).ToHashSet(StringComparer.Ordinal);
        foreach (var missing in expected.Except(liveTables, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            violations.Add(new ContractViolation("refresh.validation.table-missing", missing));
        }

        foreach (var unknown in liveTables.Except(expected, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            violations.Add(new ContractViolation("refresh.validation.table-unclassified", unknown));
        }
    }

    private static async Task ValidateMigrationsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<string> expected,
        IReadOnlySet<string> liveTables,
        ICollection<ContractViolation> violations,
        CancellationToken cancellationToken)
    {
        if (!liveTables.Contains("__EFMigrationsHistory"))
        {
            violations.Add(new ContractViolation("refresh.validation.migration-history-missing"));
            return;
        }

        var applied = await ReadNamesAsync(
            connection,
            transaction,
            "SELECT \"MigrationId\" FROM public.\"__EFMigrationsHistory\" ORDER BY \"MigrationId\"",
            cancellationToken);
        if (!applied.SequenceEqual(expected, StringComparer.Ordinal))
        {
            violations.Add(new ContractViolation(
                "refresh.validation.migration-not-current",
                DatabaseInspector.ClassifyMigrationState(expected, applied.ToArray())));
        }
    }

    private static async Task ValidateExtensionsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ICollection<ContractViolation> violations,
        CancellationToken cancellationToken)
    {
        var extensions = await ReadNamesAsync(
            connection,
            transaction,
            "SELECT extname FROM pg_catalog.pg_extension ORDER BY extname",
            cancellationToken);
        if (!extensions.Contains("pg_trgm"))
        {
            violations.Add(new ContractViolation("refresh.validation.extension-missing", "pg_trgm"));
        }
    }

    private static async Task ValidateCanonicalCountsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlySet<string> liveTables,
        ICollection<ContractViolation> violations,
        CancellationToken cancellationToken)
    {
        foreach (var expected in ExactCanonicalCounts.Where(entry => liveTables.Contains(entry.Key)))
        {
            var actual = await CountAsync(connection, transaction, expected.Key, cancellationToken);
            if (actual != expected.Value)
            {
                violations.Add(new ContractViolation(
                    "refresh.validation.canonical-count",
                    $"{expected.Key}:{actual.ToString(CultureInfo.InvariantCulture)}"));
            }
        }

        foreach (var table in new[]
                 {
                     "quran_words_unique_tashkeel", "quran_words_unique_simple",
                     "quran_tafsir_entries", "quran_tafsir_ayah_entries",
                     "quran_translation_ayah_entries", "quran_phrase_search_tokens",
                     "quran_phrase_variants", "quran_phrase_occurrences",
                     "quran_phrase_similarity_edges", "quran_phrase_similarity_anchor_stats",
                 }.Where(liveTables.Contains))
        {
            if (await CountAsync(connection, transaction, table, cancellationToken) == 0)
            {
                violations.Add(new ContractViolation("refresh.validation.canonical-empty", table));
            }
        }
    }

    private static async Task ValidateCanonicalInvariantsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlySet<string> liveTables,
        TestDatabaseRefreshOracles oracles,
        ICollection<ContractViolation> violations,
        CancellationToken cancellationToken)
    {
        var required = new[]
        {
            "quran_ayahs", "quran_phrase_index_builds", "quran_phrase_index_state",
            "quran_phrase_search_tokens", "quran_phrase_variants", "quran_phrase_occurrences",
            "quran_phrase_similarity_edges", "quran_phrase_similarity_anchor_stats",
        };
        if (required.Except(liveTables, StringComparer.Ordinal).Any())
        {
            return;
        }

        await using var quranOracleCommand = new NpgsqlCommand(
            "SELECT string_agg(id::text || '|' || verse_key || '|' || text_uthmani, E'\\n' ORDER BY id) "
            + "FROM public.quran_ayahs WHERE surah_number = @surahNumber",
            connection,
            transaction);
        quranOracleCommand.Parameters.AddWithValue("surahNumber", oracles.Quran.SurahNumber);
        var surahOne = Convert.ToString(await quranOracleCommand.ExecuteScalarAsync(cancellationToken));
        var oracleHash = string.IsNullOrEmpty(surahOne)
            ? null
            : Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{surahOne}\n")));
        if (oracleHash != oracles.Quran.Sha256
            || await CountWhereAsync(
                connection,
                transaction,
                "quran_ayahs",
                "surah_number = @value",
                oracles.Quran.SurahNumber,
                cancellationToken) != oracles.Quran.RowCount)
        {
            violations.Add(new ContractViolation("refresh.validation.quran-oracle-mismatch"));
        }

        const string phraseSql = """
            SELECT count(*) = 1
            FROM public.quran_phrase_index_state AS state
            INNER JOIN public.quran_phrase_index_builds AS build ON build.id = state.active_build_id
            WHERE state.id = 1 AND state.previous_build_id IS NULL AND state.is_stale = false
              AND build.status = 3 AND build.exact_ready AND build.similarity_ready
              AND build.validation_verdict = 'pass'
              AND build.search_token_count = (SELECT count(*) FROM public.quran_phrase_search_tokens WHERE build_id = build.id)
              AND build.variant_count = (SELECT count(*) FROM public.quran_phrase_variants WHERE build_id = build.id)
              AND build.occurrence_count = (SELECT count(*) FROM public.quran_phrase_occurrences WHERE build_id = build.id)
              AND build.similarity_edge_count = (SELECT count(*) FROM public.quran_phrase_similarity_edges WHERE build_id = build.id)
              AND build.similarity_anchor_stat_count = (SELECT count(*) FROM public.quran_phrase_similarity_anchor_stats WHERE build_id = build.id)
            """;
        if (!Convert.ToBoolean(await ScalarAsync(connection, transaction, phraseSql, cancellationToken), CultureInfo.InvariantCulture))
        {
            violations.Add(new ContractViolation("refresh.validation.phrase-search-invariant"));
        }

        const string phraseOracleSql = """
            SELECT array_agg(DISTINCT ayah.verse_key ORDER BY ayah.verse_key)
            FROM public.quran_phrase_index_state AS state
            INNER JOIN public.quran_phrase_variants AS variant
              ON variant.build_id = state.active_build_id
             AND variant.mode = 1 AND variant.word_count = @wordCount
             AND variant.first_quran_word_id = @firstQuranWordId
            INNER JOIN public.quran_phrase_occurrences AS occurrence
              ON occurrence.build_id = variant.build_id AND occurrence.variant_id = variant.id
            INNER JOIN public.quran_ayahs AS ayah ON ayah.id = occurrence.ayah_id
            """;
        await using (var phraseOracleCommand = new NpgsqlCommand(phraseOracleSql, connection, transaction))
        {
            phraseOracleCommand.Parameters.AddWithValue("wordCount", oracles.PhraseSearch.WordCount);
            phraseOracleCommand.Parameters.AddWithValue("firstQuranWordId", oracles.PhraseSearch.FirstQuranWordId);
            var actualVerseKeys = await phraseOracleCommand.ExecuteScalarAsync(cancellationToken) as string[] ?? [];
            if (!actualVerseKeys.SequenceEqual(
                    oracles.PhraseSearch.ExactVerseKeys.Order(StringComparer.Ordinal),
                    StringComparer.Ordinal))
            {
                violations.Add(new ContractViolation("refresh.validation.phrase-search-oracle-mismatch"));
            }
        }

        const string similarityOracleSql = """
            SELECT EXISTS (
                SELECT 1
                FROM public.quran_phrase_index_state AS state
                INNER JOIN public.quran_phrase_variants AS exact_variant
                  ON exact_variant.build_id = state.active_build_id
                 AND exact_variant.mode = 1 AND exact_variant.word_count = @wordCount
                 AND exact_variant.first_quran_word_id = @firstQuranWordId
                INNER JOIN public.quran_phrase_similarity_edges AS edge
                  ON edge.build_id = exact_variant.build_id
                 AND (edge.left_variant_id = exact_variant.id OR edge.right_variant_id = exact_variant.id)
                INNER JOIN public.quran_phrase_occurrences AS similar_occurrence
                  ON similar_occurrence.build_id = edge.build_id
                 AND similar_occurrence.variant_id = CASE
                     WHEN edge.left_variant_id = exact_variant.id THEN edge.right_variant_id
                     ELSE edge.left_variant_id END
                INNER JOIN public.quran_ayahs AS similar_ayah ON similar_ayah.id = similar_occurrence.ayah_id
                WHERE similar_ayah.verse_key = @similarVerseKey
                  AND edge.matched_count = @matchedWords
                  AND edge.difference_positions = @differencePositions)
            """;
        await using (var similarityCommand = new NpgsqlCommand(similarityOracleSql, connection, transaction))
        {
            similarityCommand.Parameters.AddWithValue("wordCount", oracles.PhraseSearch.WordCount);
            similarityCommand.Parameters.AddWithValue("firstQuranWordId", oracles.PhraseSearch.FirstQuranWordId);
            similarityCommand.Parameters.AddWithValue("similarVerseKey", oracles.PhraseSearch.SimilarVerseKey);
            similarityCommand.Parameters.AddWithValue("matchedWords", oracles.PhraseSearch.MatchedWords);
            similarityCommand.Parameters.AddWithValue(
                "differencePositions",
                NpgsqlDbType.Array | NpgsqlDbType.Smallint,
                oracles.PhraseSearch.DifferencePositions);
            if (await similarityCommand.ExecuteScalarAsync(cancellationToken) is not true)
            {
                violations.Add(new ContractViolation("refresh.validation.phrase-search-similarity-oracle-mismatch"));
            }
        }
    }

    private static async Task ValidateSystemCatalogueAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        IReadOnlySet<string> liveTables,
        ICollection<ContractViolation> violations,
        CancellationToken cancellationToken)
    {
        if (!liveTables.Contains("roles") || !liveTables.Contains("permissions"))
        {
            return;
        }

        var owner = contract.SystemCatalogue.OwnerRole;
        await using (var ownerCommand = new NpgsqlCommand(
                         "SELECT count(*) FROM public.roles WHERE id = @id AND name = @name AND display_name = @displayName",
                         connection,
                         transaction))
        {
            ownerCommand.Parameters.AddWithValue("id", owner.Id);
            ownerCommand.Parameters.AddWithValue("name", owner.Name);
            ownerCommand.Parameters.AddWithValue("displayName", owner.DisplayName);
            if (Convert.ToInt64(await ownerCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) != 1)
            {
                violations.Add(new ContractViolation("refresh.validation.system-catalogue.owner"));
            }
        }

        const string permissionSql = """
            SELECT code, arabic_label, english_description, display_order, retired_at
            FROM public.permissions ORDER BY code
            """;
        await using var command = new NpgsqlCommand(permissionSql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var actual = new Dictionary<string, PermissionRow>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
        {
            var code = reader.GetString(0);
            if (!actual.TryAdd(code, new PermissionRow(
                    reader.GetString(1), reader.GetString(2), reader.GetInt32(3), !reader.IsDBNull(4))))
            {
                violations.Add(new ContractViolation("refresh.validation.system-catalogue.permission-duplicate", code));
            }
        }

        foreach (var definition in AbwabPermissionCatalogue.All)
        {
            if (!actual.TryGetValue(definition.Code, out var row)
                || row.ArabicLabel != definition.ArabicLabel
                || row.EnglishDescription != definition.EnglishDescription
                || row.DisplayOrder != definition.DisplayOrder
                || row.Retired)
            {
                violations.Add(new ContractViolation("refresh.validation.system-catalogue.permission", definition.Code));
            }
        }

        var canonical = AbwabPermissionCatalogue.All.Select(item => item.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var unknownActive in actual.Where(item => !canonical.Contains(item.Key) && !item.Value.Retired))
        {
            violations.Add(new ContractViolation("refresh.validation.system-catalogue.active-unknown", unknownActive.Key));
        }
    }

    private static async Task ValidateMutableBaselineAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        IReadOnlySet<string> liveTables,
        ICollection<ContractViolation> violations,
        CancellationToken cancellationToken)
    {
        foreach (var table in contract.DataClasses.MutableApplicationState
                     .Where(table => table != contract.LinkingDataBaseline.Table && liveTables.Contains(table)))
        {
            if (await CountAsync(connection, transaction, table, cancellationToken) != 0)
            {
                violations.Add(new ContractViolation("refresh.validation.mutable-state-not-empty", table));
            }
        }

        if (!liveTables.Contains(contract.LinkingDataBaseline.Table))
        {
            return;
        }

        var baseline = contract.LinkingDataBaseline;
        await using var command = new NpgsqlCommand(
            $"SELECT count(*) FROM {CapabilityRefresher.QuoteIdentifier(baseline.Table)} "
            + "WHERE id = @id AND generation = @generation AND updated_at_utc = @updatedAtUtc",
            connection,
            transaction);
        command.Parameters.AddWithValue("id", baseline.Id);
        command.Parameters.AddWithValue("generation", baseline.Generation);
        command.Parameters.AddWithValue("updatedAtUtc", baseline.UpdatedAtUtc);
        if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) != 1
            || await CountAsync(connection, transaction, baseline.Table, cancellationToken) != 1)
        {
            violations.Add(new ContractViolation("refresh.validation.linking-baseline"));
        }
    }

    private static async Task ValidateMarkersAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        IReadOnlyDictionary<string, string> requiredMarkers,
        ICollection<ContractViolation> violations,
        CancellationToken cancellationToken)
    {
        foreach (var marker in requiredMarkers)
        {
            var value = await ReadDatabaseSettingAsync(
                connection,
                transaction,
                contract.Markers.AsDictionary()[marker.Key],
                cancellationToken);
            if (value != marker.Value)
            {
                violations.Add(new ContractViolation("refresh.validation.marker", marker.Key));
            }
        }
    }

    private static async Task ValidateRefreshMetadataAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        string? canonicalFingerprint,
        string? catalogueFingerprint,
        string? protectedFingerprint,
        ICollection<ContractViolation> violations,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var marker in new[]
                 {
                     "canonicalPipeline", "canonicalInputProvenance", "canonicalQuranFingerprint",
                     "systemCatalogueFingerprint", "protectedStateFingerprint", "refreshedAtUtc",
                 })
        {
            values[marker] = await ReadDatabaseSettingAsync(
                connection,
                transaction,
                contract.Markers.AsDictionary()[marker],
                cancellationToken);
        }

        if (values["canonicalPipeline"] != CapabilityRefresher.PipelineIdentity)
        {
            violations.Add(new ContractViolation("refresh.validation.pipeline-marker"));
        }

        if (!IsSha256(values["canonicalInputProvenance"]))
        {
            violations.Add(new ContractViolation("refresh.validation.provenance-marker"));
        }

        if (canonicalFingerprint is null || values["canonicalQuranFingerprint"] != canonicalFingerprint)
        {
            violations.Add(new ContractViolation("refresh.validation.canonical-fingerprint-marker"));
        }

        if (catalogueFingerprint is null || values["systemCatalogueFingerprint"] != catalogueFingerprint)
        {
            violations.Add(new ContractViolation("refresh.validation.catalogue-fingerprint-marker"));
        }

        if (protectedFingerprint is null || values["protectedStateFingerprint"] != protectedFingerprint)
        {
            violations.Add(new ContractViolation("refresh.validation.protected-fingerprint-marker"));
        }

        if (!DateTimeOffset.TryParse(
                values["refreshedAtUtc"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out _))
        {
            violations.Add(new ContractViolation("refresh.validation.refreshed-at-marker"));
        }
    }

    private static async Task<string?> ReadDatabaseSettingAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string name,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT substring(configured.setting FROM position('=' IN configured.setting) + 1)
            FROM pg_catalog.pg_db_role_setting AS settings
            INNER JOIN pg_catalog.pg_database AS database ON database.oid = settings.setdatabase
            CROSS JOIN LATERAL unnest(settings.setconfig) AS configured(setting)
            WHERE database.datname = current_database() AND settings.setrole = 0
              AND split_part(configured.setting, '=', 1) = @name
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("name", name);
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private static async Task ValidateRestrictedRolesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        string selectedLogin,
        IReadOnlySet<string> liveTables,
        ICollection<ContractViolation> violations,
        CancellationToken cancellationToken)
    {
        var existingRoles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in contract.Roles.AsDictionary())
        {
            await using var roleCommand = new NpgsqlCommand(
                "SELECT role.rolcanlogin = false AND role.rolsuper = false AND role.rolcreaterole = false "
                + "AND role.rolcreatedb = @createdb AND role.rolreplication = false AND role.rolbypassrls = false "
                + "AND (SELECT count(*) FROM pg_catalog.pg_auth_members AS membership "
                + "INNER JOIN pg_catalog.pg_roles AS member ON member.oid = membership.member "
                + "WHERE membership.roleid = role.oid AND member.rolname = @login) = 1 "
                + "AND (SELECT count(*) FROM pg_catalog.pg_auth_members AS membership WHERE membership.roleid = role.oid) = 1 "
                + "AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_auth_members AS membership WHERE membership.member = role.oid) "
                + "AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_database AS database "
                + "WHERE database.datdba = role.oid "
                + "AND database.datname IN (@developmentDatabase, @testDatabase, current_database())) "
                + "FROM pg_catalog.pg_roles AS role WHERE role.rolname = @role",
                connection,
                transaction);
            roleCommand.Parameters.AddWithValue("role", role.Value);
            roleCommand.Parameters.AddWithValue("login", selectedLogin);
            roleCommand.Parameters.AddWithValue("createdb", role.Key == "scratchAdministrator");
            roleCommand.Parameters.AddWithValue("developmentDatabase", contract.Targets.DevelopmentDatabase);
            roleCommand.Parameters.AddWithValue("testDatabase", contract.Targets.TestDatabase);
            if (await roleCommand.ExecuteScalarAsync(cancellationToken) is not true)
            {
                violations.Add(new ContractViolation("refresh.validation.role-attributes", role.Key));
            }
            else
            {
                existingRoles.Add(role.Value);
            }
        }

        var protectedTables = contract.DataClasses.CanonicalQuranData
            .Concat(contract.DataClasses.SystemCatalogue)
            .Concat(contract.DataClasses.SchemaState)
            .Where(liveTables.Contains)
            .ToArray();
        var allTables = contract.AllTables().Select(entry => entry.Table).Where(liveTables.Contains).ToArray();
        var mutableTables = contract.DataClasses.MutableApplicationState.Where(liveTables.Contains).ToArray();
        var resetTables = mutableTables.Where(table => table != contract.LinkingDataBaseline.Table).ToArray();
        var allSequences = await ReadSequencesAsync(connection, transaction, tables: null, cancellationToken);
        var mutableSequences = await ReadSequencesAsync(
            connection,
            transaction,
            contract.DataClasses.MutableApplicationState,
            cancellationToken);
        var protectedSequences = allSequences.Except(mutableSequences, StringComparer.Ordinal).ToArray();
        foreach (var role in contract.Roles.AsDictionary())
        {
            if (!existingRoles.Contains(role.Value))
            {
                continue;
            }

            var mutation = await HasAnyTablePrivilegeAsync(
                connection,
                transaction,
                role.Value,
                protectedTables,
                ["INSERT", "UPDATE", "DELETE", "TRUNCATE", "REFERENCES", "TRIGGER", "MAINTAIN"],
                cancellationToken);
            if (mutation)
            {
                violations.Add(new ContractViolation("refresh.validation.role-protected-mutation", role.Key));
            }

            await using var createCommand = new NpgsqlCommand(
                "SELECT pg_catalog.has_database_privilege(@role, current_database(), 'CREATE') "
                + "OR pg_catalog.has_schema_privilege(@role, 'public', 'CREATE')",
                connection,
                transaction);
            createCommand.Parameters.AddWithValue("role", role.Value);
            if (await createCommand.ExecuteScalarAsync(cancellationToken) is true)
            {
                violations.Add(new ContractViolation("refresh.validation.role-create-privilege", role.Key));
            }
        }

        if (!existingRoles.Contains(contract.Roles.Reader)
            || !await HasAllTablePrivilegeAsync(
                connection, transaction, contract.Roles.Reader,
                allTables,
                "SELECT", cancellationToken))
        {
            violations.Add(new ContractViolation("refresh.validation.reader-grants"));
        }

        if (!existingRoles.Contains(contract.Roles.Application)
            || !await HasAllTablePrivilegeAsync(
                connection, transaction, contract.Roles.Application,
                allTables, "SELECT", cancellationToken)
            || !await HasAllTablePrivilegeAsync(
                connection, transaction, contract.Roles.Application,
                mutableTables,
                "INSERT,UPDATE,DELETE", cancellationToken))
        {
            violations.Add(new ContractViolation("refresh.validation.application-grants"));
        }

        if (existingRoles.Contains(contract.Roles.Reader)
            && await HasAnyTablePrivilegeAsync(
                connection, transaction, contract.Roles.Reader, mutableTables,
                ["INSERT", "UPDATE", "DELETE", "TRUNCATE", "REFERENCES", "TRIGGER", "MAINTAIN"], cancellationToken))
        {
            violations.Add(new ContractViolation("refresh.validation.reader-mutation-grant"));
        }

        if (!existingRoles.Contains(contract.Roles.Resetter)
            || !await HasAllTablePrivilegeAsync(
                connection, transaction, contract.Roles.Resetter, mutableTables,
                "SELECT", cancellationToken)
            || !await HasAllTablePrivilegeAsync(
                connection, transaction, contract.Roles.Resetter, resetTables,
                "TRUNCATE", cancellationToken)
            || !await HasAllTablePrivilegeAsync(
                connection, transaction, contract.Roles.Resetter,
                [contract.LinkingDataBaseline.Table], "UPDATE", cancellationToken))
        {
            violations.Add(new ContractViolation("refresh.validation.resetter-grants"));
        }

        if (existingRoles.Contains(contract.Roles.Resetter)
            && (await HasAnyTablePrivilegeAsync(
                    connection, transaction, contract.Roles.Resetter, protectedTables,
                    ["SELECT"], cancellationToken)
                || await HasAnyTablePrivilegeAsync(
                    connection, transaction, contract.Roles.Resetter, mutableTables,
                    ["INSERT", "DELETE", "REFERENCES", "TRIGGER", "MAINTAIN"], cancellationToken)
                || await HasAnyTablePrivilegeAsync(
                    connection, transaction, contract.Roles.Resetter, resetTables,
                    ["UPDATE"], cancellationToken)
                || await HasAnyTablePrivilegeAsync(
                    connection, transaction, contract.Roles.Resetter,
                    [contract.LinkingDataBaseline.Table], ["TRUNCATE"], cancellationToken)))
        {
            violations.Add(new ContractViolation("refresh.validation.resetter-excess-grant"));
        }

        if (existingRoles.Contains(contract.Roles.Application)
            && (await HasAnyTablePrivilegeAsync(
                    connection, transaction, contract.Roles.Application, mutableTables,
                    ["TRUNCATE", "REFERENCES", "TRIGGER", "MAINTAIN"], cancellationToken)
                || !await HasAllSequencePrivilegeAsync(
                    connection, transaction, contract.Roles.Application,
                    mutableSequences, ["SELECT", "USAGE"], cancellationToken)
                || await HasAnySequencePrivilegeAsync(
                    connection, transaction, contract.Roles.Application,
                    mutableSequences, ["UPDATE"], cancellationToken)
                || await HasAnySequencePrivilegeAsync(
                    connection, transaction, contract.Roles.Application,
                    protectedSequences, ["SELECT", "USAGE", "UPDATE"], cancellationToken)))
        {
            violations.Add(new ContractViolation("refresh.validation.application-excess-grant"));
        }

        foreach (var role in new[]
                 {
                     contract.Roles.Reader,
                     contract.Roles.Resetter,
                     contract.Roles.ScratchAdministrator,
                 }.Where(existingRoles.Contains))
        {
            if (await HasAnySequencePrivilegeAsync(
                    connection, transaction, role, allSequences,
                    ["SELECT", "USAGE", "UPDATE"], cancellationToken))
            {
                violations.Add(new ContractViolation("refresh.validation.role-sequence-grant", role));
            }
        }

        if (existingRoles.Contains(contract.Roles.ScratchAdministrator)
            && await HasAnyTablePrivilegeAsync(
                connection, transaction, contract.Roles.ScratchAdministrator,
                allTables,
                ["SELECT", "INSERT", "UPDATE", "DELETE", "TRUNCATE", "REFERENCES", "TRIGGER", "MAINTAIN"],
                cancellationToken))
        {
            violations.Add(new ContractViolation("refresh.validation.scratch-administrator-table-grant"));
        }
    }

    private static async Task<string> FingerprintSchemaStateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var queries = new[]
        {
            "SELECT concat_ws('|', extname, extversion) FROM pg_catalog.pg_extension ORDER BY extname",
            """
            SELECT concat_ws('|', relation.relname, attribute.attnum::text, attribute.attname,
                              pg_catalog.format_type(attribute.atttypid, attribute.atttypmod),
                              attribute.attnotnull::text, attribute.attidentity,
                              COALESCE(pg_catalog.pg_get_expr(default_value.adbin, default_value.adrelid), ''))
            FROM pg_catalog.pg_class AS relation
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = relation.relnamespace
            INNER JOIN pg_catalog.pg_attribute AS attribute ON attribute.attrelid = relation.oid
            LEFT JOIN pg_catalog.pg_attrdef AS default_value
              ON default_value.adrelid = relation.oid AND default_value.adnum = attribute.attnum
            WHERE namespace.nspname = 'public' AND relation.relkind IN ('r', 'p')
              AND attribute.attnum > 0 AND NOT attribute.attisdropped
            ORDER BY relation.relname, attribute.attnum
            """,
            """
            SELECT concat_ws('|', relation.relname, constraint_record.contype,
                              constraint_record.conname, pg_catalog.pg_get_constraintdef(constraint_record.oid, true))
            FROM pg_catalog.pg_constraint AS constraint_record
            INNER JOIN pg_catalog.pg_class AS relation ON relation.oid = constraint_record.conrelid
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = relation.relnamespace
            WHERE namespace.nspname = 'public'
            ORDER BY relation.relname, constraint_record.contype, constraint_record.conname
            """,
            """
            SELECT concat_ws('|', table_relation.relname, index_relation.relname,
                              pg_catalog.pg_get_indexdef(index_relation.oid))
            FROM pg_catalog.pg_index AS index_record
            INNER JOIN pg_catalog.pg_class AS table_relation ON table_relation.oid = index_record.indrelid
            INNER JOIN pg_catalog.pg_class AS index_relation ON index_relation.oid = index_record.indexrelid
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = table_relation.relnamespace
            WHERE namespace.nspname = 'public'
            ORDER BY table_relation.relname, index_relation.relname
            """,
            """
            SELECT concat_ws('|', sequence.relname, sequence_definition.seqstart::text,
                              sequence_definition.seqincrement::text, sequence_definition.seqmin::text,
                              sequence_definition.seqmax::text, sequence_definition.seqcache::text,
                              sequence_definition.seqcycle::text)
            FROM pg_catalog.pg_class AS sequence
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = sequence.relnamespace
            INNER JOIN pg_catalog.pg_sequence AS sequence_definition ON sequence_definition.seqrelid = sequence.oid
            WHERE namespace.nspname = 'public' AND sequence.relkind = 'S'
            ORDER BY sequence.relname
            """,
            """
            SELECT concat_ws('|', relation.relname, trigger_record.tgname, pg_catalog.pg_get_triggerdef(trigger_record.oid, true))
            FROM pg_catalog.pg_trigger AS trigger_record
            INNER JOIN pg_catalog.pg_class AS relation ON relation.oid = trigger_record.tgrelid
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = relation.relnamespace
            WHERE namespace.nspname = 'public' AND NOT trigger_record.tgisinternal
            ORDER BY relation.relname, trigger_record.tgname
            """,
            "SELECT concat_ws('|', \"MigrationId\", \"ProductVersion\") FROM public.\"__EFMigrationsHistory\" ORDER BY \"MigrationId\"",
        };
        foreach (var query in queries)
        {
            await AppendQueryFingerprintAsync(hash, connection, transaction, query, cancellationToken);
            hash.AppendData([0]);
        }

        const string protectedSequencesSql = """
            SELECT DISTINCT sequence.relname
            FROM pg_catalog.pg_class AS sequence
            INNER JOIN pg_catalog.pg_depend AS dependency
              ON dependency.classid = 'pg_class'::regclass
             AND dependency.objid = sequence.oid AND dependency.deptype IN ('a', 'i')
            INNER JOIN pg_catalog.pg_class AS table_relation ON table_relation.oid = dependency.refobjid
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = table_relation.relnamespace
            WHERE sequence.relkind = 'S' AND namespace.nspname = 'public'
              AND table_relation.relname = ANY(@protectedTables)
            ORDER BY sequence.relname
            """;
        await using var protectedSequencesCommand = new NpgsqlCommand(
            protectedSequencesSql,
            connection,
            transaction);
        protectedSequencesCommand.Parameters.AddWithValue(
            "protectedTables",
            NpgsqlDbType.Array | NpgsqlDbType.Text,
            contract.DataClasses.CanonicalQuranData
                .Concat(contract.DataClasses.SystemCatalogue)
                .ToArray());
        await using var protectedSequencesReader = await protectedSequencesCommand.ExecuteReaderAsync(cancellationToken);
        var protectedSequences = new List<string>();
        while (await protectedSequencesReader.ReadAsync(cancellationToken))
        {
            protectedSequences.Add(protectedSequencesReader.GetString(0));
        }

        await protectedSequencesReader.DisposeAsync();
        foreach (var sequence in protectedSequences.Order(StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes($"sequence:{sequence}|"));
            await AppendQueryFingerprintAsync(
                hash,
                connection,
                transaction,
                $"SELECT concat_ws('|', last_value::text, is_called::text) FROM {CapabilityRefresher.QuoteIdentifier(sequence)}",
                cancellationToken);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static async Task AppendQueryFingerprintAsync(
        IncrementalHash hash,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(reader.IsDBNull(0) ? "<null>" : reader.GetString(0)));
            hash.AppendData([10]);
        }
    }

    private static async Task<string> FingerprintTablesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IEnumerable<string> tables,
        CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var table in tables.Order(StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes($"table:{table}\n"));
            var orderColumns = await PrimaryKeyColumnsAsync(connection, transaction, table, cancellationToken);
            var order = orderColumns.Count == 0
                ? ""
                : " ORDER BY " + string.Join(", ", orderColumns.Select(CapabilityRefresher.QuoteIdentifier));
            var sql = $"SELECT row_to_json(row_value)::text FROM (SELECT * FROM {CapabilityRefresher.QuoteIdentifier(table)}{order}) AS row_value";
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                hash.AppendData(Encoding.UTF8.GetBytes(reader.GetString(0)));
                hash.AppendData([10]);
            }
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static async Task<IReadOnlyList<string>> PrimaryKeyColumnsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string table,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT attribute.attname
            FROM pg_catalog.pg_index AS index
            INNER JOIN pg_catalog.pg_class AS relation ON relation.oid = index.indrelid
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = relation.relnamespace
            CROSS JOIN LATERAL unnest(index.indkey) WITH ORDINALITY AS key(attnum, ordinal)
            INNER JOIN pg_catalog.pg_attribute AS attribute
              ON attribute.attrelid = relation.oid AND attribute.attnum = key.attnum
            WHERE namespace.nspname = 'public' AND relation.relname = @table AND index.indisprimary
            ORDER BY key.ordinal
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("table", table);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    private static async Task<bool> HasAllTablePrivilegeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string role,
        IReadOnlyList<string> tables,
        string privileges,
        CancellationToken cancellationToken)
    {
        foreach (var privilege in privileges.Split(','))
        {
            const string sql = """
                SELECT COALESCE(bool_and(pg_catalog.has_table_privilege(@role, format('%I.%I', 'public', table_name), @privilege)), false)
                FROM unnest(@tables) AS names(table_name)
                """;
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("role", role);
            command.Parameters.AddWithValue("privilege", privilege);
            command.Parameters.AddWithValue("tables", NpgsqlDbType.Array | NpgsqlDbType.Text, tables.ToArray());
            if (await command.ExecuteScalarAsync(cancellationToken) is not true)
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<bool> HasAnyTablePrivilegeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string role,
        IReadOnlyList<string> tables,
        IReadOnlyList<string> privileges,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM unnest(@tables) AS names(table_name)
                CROSS JOIN unnest(@privileges) AS requested(privilege)
                WHERE pg_catalog.has_table_privilege(@role, format('%I.%I', 'public', table_name), privilege))
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("tables", NpgsqlDbType.Array | NpgsqlDbType.Text, tables.ToArray());
        command.Parameters.AddWithValue("privileges", NpgsqlDbType.Array | NpgsqlDbType.Text, privileges.ToArray());
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<IReadOnlyList<string>> ReadSequencesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<string>? tables,
        CancellationToken cancellationToken)
    {
        const string allSql = """
            SELECT sequence.relname
            FROM pg_catalog.pg_class AS sequence
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = sequence.relnamespace
            WHERE sequence.relkind = 'S' AND namespace.nspname = 'public'
            ORDER BY sequence.relname
            """;
        const string ownedSql = """
            SELECT DISTINCT sequence.relname
            FROM pg_catalog.pg_class AS sequence
            INNER JOIN pg_catalog.pg_depend AS dependency
              ON dependency.classid = 'pg_class'::regclass
             AND dependency.objid = sequence.oid AND dependency.deptype IN ('a', 'i')
            INNER JOIN pg_catalog.pg_class AS table_relation ON table_relation.oid = dependency.refobjid
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = table_relation.relnamespace
            WHERE sequence.relkind = 'S' AND namespace.nspname = 'public'
              AND table_relation.relname = ANY(@tables)
            ORDER BY sequence.relname
            """;
        await using var command = new NpgsqlCommand(tables is null ? allSql : ownedSql, connection, transaction);
        if (tables is not null)
        {
            command.Parameters.AddWithValue("tables", NpgsqlDbType.Array | NpgsqlDbType.Text, tables.ToArray());
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    private static async Task<bool> HasAllSequencePrivilegeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string role,
        IReadOnlyList<string> sequences,
        IReadOnlyList<string> privileges,
        CancellationToken cancellationToken)
    {
        foreach (var privilege in privileges)
        {
            const string sql = """
                SELECT COALESCE(bool_and(pg_catalog.has_sequence_privilege(
                    @role, format('%I.%I', 'public', sequence_name), @privilege)), true)
                FROM unnest(@sequences) AS names(sequence_name)
                """;
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("role", role);
            command.Parameters.AddWithValue("privilege", privilege);
            command.Parameters.AddWithValue(
                "sequences", NpgsqlDbType.Array | NpgsqlDbType.Text, sequences.ToArray());
            if (await command.ExecuteScalarAsync(cancellationToken) is not true)
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<bool> HasAnySequencePrivilegeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string role,
        IReadOnlyList<string> sequences,
        IReadOnlyList<string> privileges,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM unnest(@sequences) AS names(sequence_name)
                CROSS JOIN unnest(@privileges) AS requested(privilege)
                WHERE pg_catalog.has_sequence_privilege(
                    @role, format('%I.%I', 'public', sequence_name), privilege))
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue(
            "sequences", NpgsqlDbType.Array | NpgsqlDbType.Text, sequences.ToArray());
        command.Parameters.AddWithValue(
            "privileges", NpgsqlDbType.Array | NpgsqlDbType.Text, privileges.ToArray());
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<long> CountAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string table,
        CancellationToken cancellationToken) =>
        Convert.ToInt64(await ScalarAsync(
            connection,
            transaction,
            $"SELECT count(*) FROM {CapabilityRefresher.QuoteIdentifier(table)}",
            cancellationToken), CultureInfo.InvariantCulture);

    private static async Task<long> CountWhereAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string table,
        string predicate,
        int value,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT count(*) FROM {CapabilityRefresher.QuoteIdentifier(table)} WHERE {predicate}",
            connection,
            transaction);
        command.Parameters.AddWithValue("value", value);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private static async Task<object?> ScalarAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task<HashSet<string>> ReadNamesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IReadOnlyList<ContractViolation> Order(IEnumerable<ContractViolation> violations) =>
        violations.Distinct()
            .OrderBy(violation => violation.Code, StringComparer.Ordinal)
            .ThenBy(violation => violation.Subject, StringComparer.Ordinal)
            .ToArray();

    private static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(character => char.IsAsciiHexDigit(character) && !char.IsUpper(character));

    private sealed record PermissionRow(
        string ArabicLabel,
        string EnglishDescription,
        int DisplayOrder,
        bool Retired);

    private sealed class TestDatabaseRefreshOracles
    {
        public required string Format { get; init; }
        public required int Version { get; init; }
        public required QuranRefreshOracle Quran { get; init; }
        public required PhraseSearchRefreshOracle PhraseSearch { get; init; }
    }

    private sealed class QuranRefreshOracle
    {
        public required string Id { get; init; }
        public required int SurahNumber { get; init; }
        public required int RowCount { get; init; }
        public required string Sha256 { get; init; }
        public required string Serialization { get; init; }
        public required string Provenance { get; init; }
    }

    private sealed class PhraseSearchRefreshOracle
    {
        public required string Id { get; init; }
        public required string Mode { get; init; }
        public required short WordCount { get; init; }
        public required int FirstQuranWordId { get; init; }
        public required string[] ExactVerseKeys { get; init; }
        public required string SimilarVerseKey { get; init; }
        public required short MatchedWords { get; init; }
        public required short[] DifferencePositions { get; init; }
        public required string[] EvidenceSha256 { get; init; }
        public required string Provenance { get; init; }
    }
}
