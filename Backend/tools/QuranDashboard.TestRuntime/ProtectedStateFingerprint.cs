using System.Buffers.Binary;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace QuranDashboard.TestRuntime;

internal sealed record ProtectedStateFingerprintComponents(
    string CanonicalQuranData,
    string SystemCatalogue,
    string SchemaState);

internal sealed record ProtectedStateFingerprintReport(
    string Algorithm,
    string Fingerprint,
    ProtectedStateFingerprintComponents Components,
    int CanonicalTableCount,
    int SystemCatalogueTableCount,
    int SchemaTableCount,
    int ProtectedSequenceCount,
    int DumpFilesRetained);

internal static class ProtectedStateFingerprint
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    internal static async Task<ProtectedStateFingerprintReport> ComputeAsync(
        NpgsqlConnection connection,
        DatabaseContract contract,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken);
        await ExecuteAsync(
            connection,
            transaction,
            "SET TRANSACTION READ ONLY; SET LOCAL timezone = 'UTC'; SET LOCAL datestyle = 'ISO, YMD'; SET LOCAL intervalstyle = 'iso_8601'; SET LOCAL bytea_output = 'hex'; SET LOCAL extra_float_digits = 3",
            cancellationToken);

        var report = await ComputeAsync(
            connection,
            transaction,
            contract,
            useReaderRole: true,
            verifiedCanonicalQuranDataFingerprint: null,
            cancellationToken);
        await transaction.RollbackAsync(cancellationToken);
        return report;
    }

    internal static async Task<ProtectedStateFingerprintReport> ComputeWithVerifiedCanonicalAsync(
        NpgsqlConnection connection,
        DatabaseContract contract,
        string verifiedCanonicalQuranDataFingerprint,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken);
        await ExecuteAsync(
            connection,
            transaction,
            "SET TRANSACTION READ ONLY; SET LOCAL timezone = 'UTC'; SET LOCAL datestyle = 'ISO, YMD'; SET LOCAL intervalstyle = 'iso_8601'; SET LOCAL bytea_output = 'hex'; SET LOCAL extra_float_digits = 3",
            cancellationToken);

        var report = await ComputeAsync(
            connection,
            transaction,
            contract,
            useReaderRole: true,
            verifiedCanonicalQuranDataFingerprint,
            cancellationToken);
        await transaction.RollbackAsync(cancellationToken);
        return report;
    }

    internal static async Task<ProtectedStateFingerprintReport> ComputeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        CancellationToken cancellationToken = default)
    {
        return await ComputeAsync(
            connection,
            transaction,
            contract,
            useReaderRole: false,
            verifiedCanonicalQuranDataFingerprint: null,
            cancellationToken);
    }

    internal static Task<ProtectedStateFingerprintReport> ComputeWithVerifiedCanonicalAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        string verifiedCanonicalQuranDataFingerprint,
        CancellationToken cancellationToken = default)
    {
        return ComputeAsync(
            connection,
            transaction,
            contract,
            useReaderRole: false,
            verifiedCanonicalQuranDataFingerprint,
            cancellationToken);
    }

    private static async Task<ProtectedStateFingerprintReport> ComputeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        bool useReaderRole,
        string? verifiedCanonicalQuranDataFingerprint,
        CancellationToken cancellationToken)
    {
        if (useReaderRole)
        {
            await ExecuteAsync(
                connection,
                transaction,
                $"SET LOCAL ROLE {PostgreSqlIdentifier.Quote(contract.Roles.Reader)}",
                cancellationToken);
        }

        var canonical = verifiedCanonicalQuranDataFingerprint is null
            ? await HashTablesAsync(
                connection,
                transaction,
                "canonical-quran-data",
                contract.DataClasses.CanonicalQuranData,
                cancellationToken)
            : new ComponentHash(
                NormalizeVerifiedFingerprint(verifiedCanonicalQuranDataFingerprint),
                ProtectedSequenceCount: 0);
        var catalogue = await HashTablesAsync(
            connection,
            transaction,
            "system-catalogue",
            contract.DataClasses.SystemCatalogue,
            cancellationToken);
        if (useReaderRole)
        {
            await ExecuteAsync(connection, transaction, "RESET ROLE", cancellationToken);
        }

        var schema = await HashSchemaAsync(connection, transaction, contract, cancellationToken);
        var aggregate = HashValues(
            ("canonical-quran-data", canonical.Hash),
            ("system-catalogue", catalogue.Hash),
            ("schema-state", schema.Hash));

        return new ProtectedStateFingerprintReport(
            "sha256",
            aggregate,
            new ProtectedStateFingerprintComponents(canonical.Hash, catalogue.Hash, schema.Hash),
            contract.DataClasses.CanonicalQuranData.Length,
            contract.DataClasses.SystemCatalogue.Length,
            contract.DataClasses.SchemaState.Length,
            schema.ProtectedSequenceCount,
            0);
    }

    private static string NormalizeVerifiedFingerprint(string fingerprint)
    {
        if (fingerprint.Length != 64 || !fingerprint.All(Uri.IsHexDigit))
        {
            throw new ArgumentException(
                "A verified Canonical Quran Data fingerprint must be a 64-character SHA-256 value.",
                nameof(fingerprint));
        }

        return fingerprint.ToLowerInvariant();
    }

    private static async Task<ComponentHash> HashTablesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string component,
        IReadOnlyCollection<string> tables,
        CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, component);
        foreach (var table in tables.Order(StringComparer.Ordinal))
        {
            Append(hash, $"table:{table}");
            await AppendQueryAsync(
                hash,
                connection,
                transaction,
                $"SELECT pg_catalog.row_to_json(row_value)::text FROM public.{PostgreSqlIdentifier.Quote(table)} AS row_value ORDER BY pg_catalog.row_to_json(row_value)::text COLLATE \"C\"",
                cancellationToken);
        }

        return new ComponentHash(Convert.ToHexStringLower(hash.GetHashAndReset()), 0);
    }

    private static async Task<ComponentHash> HashSchemaAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, "schema-state");

        await AppendNamedQueryAsync(
            hash,
            "relations-and-columns",
            connection,
            transaction,
            """
            SELECT pg_catalog.jsonb_build_array(
                       namespace.nspname,
                       relation.relname,
                       relation.relkind,
                       relation.relpersistence,
                       relation.relreplident,
                       relation.relrowsecurity,
                       relation.relforcerowsecurity,
                       relation.reloptions,
                       pg_catalog.pg_get_partkeydef(relation.oid),
                       access_method.amname,
                       tablespace.spcname,
                       attribute.attnum,
                       attribute.attname,
                       pg_catalog.format_type(attribute.atttypid, attribute.atttypmod),
                       attribute.attnotnull,
                       attribute.attidentity,
                       attribute.attgenerated,
                       pg_catalog.pg_get_expr(default_value.adbin, default_value.adrelid, true),
                       collation_value.collname)::text
            FROM pg_catalog.pg_class AS relation
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = relation.relnamespace
            LEFT JOIN pg_catalog.pg_attribute AS attribute
              ON attribute.attrelid = relation.oid
             AND attribute.attnum > 0
             AND NOT attribute.attisdropped
            LEFT JOIN pg_catalog.pg_attrdef AS default_value
              ON default_value.adrelid = relation.oid
             AND default_value.adnum = attribute.attnum
            LEFT JOIN pg_catalog.pg_collation AS collation_value ON collation_value.oid = attribute.attcollation
            LEFT JOIN pg_catalog.pg_am AS access_method ON access_method.oid = relation.relam
            LEFT JOIN pg_catalog.pg_tablespace AS tablespace ON tablespace.oid = relation.reltablespace
            WHERE namespace.nspname = 'public'
              AND relation.relkind IN ('r', 'p', 'v', 'm', 'f')
            ORDER BY relation.relname COLLATE "C", attribute.attnum
            """,
            cancellationToken);
        await AppendNamedQueryAsync(
            hash,
            "views-and-rules",
            connection,
            transaction,
            """
            SELECT pg_catalog.jsonb_build_array(
                       relation.relname,
                       relation.relkind,
                       pg_catalog.pg_get_viewdef(relation.oid, true),
                       rules.rulename,
                       rules.definition)::text
            FROM pg_catalog.pg_class AS relation
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = relation.relnamespace
            LEFT JOIN pg_catalog.pg_rules AS rules
              ON rules.schemaname = namespace.nspname
             AND rules.tablename = relation.relname
            WHERE namespace.nspname = 'public'
              AND relation.relkind IN ('v', 'm')
            ORDER BY relation.relname COLLATE "C", rules.rulename COLLATE "C"
            """,
            cancellationToken);
        await AppendNamedQueryAsync(
            hash,
            "functions",
            connection,
            transaction,
            """
            SELECT pg_catalog.jsonb_build_array(
                       procedure_value.proname,
                       pg_catalog.pg_get_function_identity_arguments(procedure_value.oid),
                       pg_catalog.pg_get_functiondef(procedure_value.oid))::text
            FROM pg_catalog.pg_proc AS procedure_value
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = procedure_value.pronamespace
            WHERE namespace.nspname = 'public'
              AND procedure_value.prokind IN ('f', 'p')
            ORDER BY procedure_value.proname COLLATE "C",
                     pg_catalog.pg_get_function_identity_arguments(procedure_value.oid) COLLATE "C"
            """,
            cancellationToken);
        await AppendNamedQueryAsync(
            hash,
            "types",
            connection,
            transaction,
            """
            SELECT pg_catalog.jsonb_build_array(
                       type_value.typname,
                       type_value.typtype,
                       type_value.typcategory,
                       type_value.typnotnull,
                       type_value.typdefault,
                       pg_catalog.format_type(type_value.typbasetype, type_value.typtypmod),
                       collation_value.collname,
                       enum_value.enumsortorder,
                       enum_value.enumlabel)::text
            FROM pg_catalog.pg_type AS type_value
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = type_value.typnamespace
            LEFT JOIN pg_catalog.pg_collation AS collation_value ON collation_value.oid = type_value.typcollation
            LEFT JOIN pg_catalog.pg_enum AS enum_value ON enum_value.enumtypid = type_value.oid
            WHERE namespace.nspname = 'public'
              AND type_value.typtype IN ('c', 'd', 'e', 'r', 'm')
            ORDER BY type_value.typname COLLATE "C", enum_value.enumsortorder
            """,
            cancellationToken);
        await AppendNamedQueryAsync(
            hash,
            "row-security-policies",
            connection,
            transaction,
            """
            SELECT pg_catalog.jsonb_build_array(
                       tablename,
                       policyname,
                       permissive,
                       roles,
                       cmd,
                       qual,
                       with_check)::text
            FROM pg_catalog.pg_policies
            WHERE schemaname = 'public'
            ORDER BY tablename COLLATE "C", policyname COLLATE "C"
            """,
            cancellationToken);
        await AppendNamedQueryAsync(
            hash,
            "constraints",
            connection,
            transaction,
            """
            SELECT pg_catalog.jsonb_build_array(
                       relation.relname,
                       constraint_value.conname,
                       constraint_value.contype,
                       constraint_value.condeferrable,
                       constraint_value.condeferred,
                       constraint_value.convalidated,
                       pg_catalog.pg_get_constraintdef(constraint_value.oid, true))::text
            FROM pg_catalog.pg_constraint AS constraint_value
            INNER JOIN pg_catalog.pg_class AS relation ON relation.oid = constraint_value.conrelid
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = relation.relnamespace
            WHERE namespace.nspname = 'public'
            ORDER BY relation.relname COLLATE "C", constraint_value.conname COLLATE "C"
            """,
            cancellationToken);
        await AppendNamedQueryAsync(
            hash,
            "indexes",
            connection,
            transaction,
            """
            SELECT pg_catalog.jsonb_build_array(tablename, indexname, indexdef)::text
            FROM pg_catalog.pg_indexes
            WHERE schemaname = 'public'
            ORDER BY tablename COLLATE "C", indexname COLLATE "C"
            """,
            cancellationToken);
        await AppendNamedQueryAsync(
            hash,
            "extensions",
            connection,
            transaction,
            """
            SELECT pg_catalog.jsonb_build_array(extension.extname, extension.extversion, namespace.nspname, extension.extrelocatable)::text
            FROM pg_catalog.pg_extension AS extension
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = extension.extnamespace
            ORDER BY extension.extname COLLATE "C"
            """,
            cancellationToken);
        await AppendNamedQueryAsync(
            hash,
            "triggers",
            connection,
            transaction,
            """
            SELECT pg_catalog.jsonb_build_array(
                       relation.relname,
                       trigger_value.tgname,
                       pg_catalog.pg_get_triggerdef(trigger_value.oid, true))::text
            FROM pg_catalog.pg_trigger AS trigger_value
            INNER JOIN pg_catalog.pg_class AS relation ON relation.oid = trigger_value.tgrelid
            INNER JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = relation.relnamespace
            WHERE namespace.nspname = 'public'
              AND NOT trigger_value.tgisinternal
            ORDER BY relation.relname COLLATE "C", trigger_value.tgname COLLATE "C"
            """,
            cancellationToken);

        var migrationHash = await HashTablesAsync(
            connection,
            transaction,
            "migration-history",
            contract.DataClasses.SchemaState,
            cancellationToken);
        Append(hash, "migration-history");
        Append(hash, migrationHash.Hash);

        Append(hash, "sequences");
        var protectedSequenceCount = 0;
        var sequences = new List<SequenceFingerprintRow>();
        await using (var command = new NpgsqlCommand(
                         """
                         SELECT pg_catalog.jsonb_build_array(
                                    sequences.schemaname,
                                    sequences.sequencename,
                                    sequences.data_type,
                                    sequences.start_value,
                                    sequences.min_value,
                                    sequences.max_value,
                                    sequences.increment_by,
                                    sequences.cycle,
                                    sequences.cache_size,
                                    owned_relation.relname,
                                    owned_attribute.attname)::text,
                                sequences.sequencename,
                                owned_relation.relname IS NULL OR NOT (owned_relation.relname = ANY(@mutable_tables))
                         FROM pg_catalog.pg_sequences AS sequences
                         INNER JOIN pg_catalog.pg_class AS sequence_relation
                           ON sequence_relation.relname = sequences.sequencename
                         INNER JOIN pg_catalog.pg_namespace AS sequence_namespace
                           ON sequence_namespace.oid = sequence_relation.relnamespace
                          AND sequence_namespace.nspname = sequences.schemaname
                         LEFT JOIN pg_catalog.pg_depend AS dependency
                           ON dependency.classid = 'pg_catalog.pg_class'::pg_catalog.regclass
                          AND dependency.objid = sequence_relation.oid
                          AND dependency.refclassid = 'pg_catalog.pg_class'::pg_catalog.regclass
                          AND dependency.deptype IN ('a', 'i')
                         LEFT JOIN pg_catalog.pg_class AS owned_relation ON owned_relation.oid = dependency.refobjid
                         LEFT JOIN pg_catalog.pg_attribute AS owned_attribute
                           ON owned_attribute.attrelid = dependency.refobjid
                          AND owned_attribute.attnum = dependency.refobjsubid
                         WHERE sequences.schemaname = 'public'
                         ORDER BY sequences.sequencename COLLATE "C"
                         """,
                         connection,
                         transaction))
        {
            command.Parameters.AddWithValue(
                "mutable_tables",
                NpgsqlDbType.Array | NpgsqlDbType.Text,
                contract.DataClasses.MutableApplicationState);
            await using var reader = await command.ExecuteReaderAsync(
                System.Data.CommandBehavior.SequentialAccess,
                cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                sequences.Add(new SequenceFingerprintRow(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetBoolean(2)));
            }
        }

        foreach (var sequence in sequences)
        {
            Append(hash, sequence.Definition);
            if (sequence.IsProtected)
            {
                await using var stateCommand = new NpgsqlCommand(
                    $"SELECT last_value, is_called FROM public.{PostgreSqlIdentifier.Quote(sequence.Name)}",
                    connection,
                    transaction);
                await using var stateReader = await stateCommand.ExecuteReaderAsync(cancellationToken);
                await stateReader.ReadAsync(cancellationToken);
                Append(hash, stateReader.GetInt64(0).ToString(System.Globalization.CultureInfo.InvariantCulture));
                Append(hash, stateReader.GetBoolean(1) ? "true" : "false");
                protectedSequenceCount++;
            }
        }

        return new ComponentHash(Convert.ToHexStringLower(hash.GetHashAndReset()), protectedSequenceCount);
    }

    private static async Task AppendNamedQueryAsync(
        IncrementalHash hash,
        string name,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        Append(hash, name);
        await AppendQueryAsync(hash, connection, transaction, sql, cancellationToken);
    }

    private static async Task AppendQueryAsync(
        IncrementalHash hash,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(
            System.Data.CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            Append(hash, reader.GetString(0));
        }
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

    private static string HashValues(params (string Name, string Value)[] values)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var value in values)
        {
            Append(hash, value.Name);
            Append(hash, value.Value);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void Append(IncrementalHash hash, string value)
    {
        var bytes = Utf8.GetBytes(value);
        Span<byte> length = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
        hash.AppendData(length);
        hash.AppendData(bytes);
    }

    private sealed record ComponentHash(string Hash, int ProtectedSequenceCount);

    private sealed record SequenceFingerprintRow(string Definition, string Name, bool IsProtected);
}
