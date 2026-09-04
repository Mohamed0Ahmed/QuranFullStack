using System.Text.Json;
using FluentAssertions;
using Npgsql;
using QuranDashboard.TestRuntime;

namespace QuranDashboard.Tests.TestRuntime;

[Collection(nameof(TestRuntimeResetCollection))]
public sealed class TestRuntimeProtectedStateTests(TestRuntimeResetFixture fixture)
{
    [Fact]
    public async Task Fingerprint_IgnoresMutableRowsAndMutableSequenceCountersWithoutRetainingADump()
    {
        var outputDirectory = Directory.CreateTempSubdirectory("test-runtime-fingerprint-");
        try
        {
            var before = await RunFingerprintAsync();

            await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
            {
                await connection.OpenAsync();
                await ExecuteAsync(
                    connection,
                    "UPDATE public.linking_data_state SET generation = 9, updated_at_utc = now() WHERE id = 1");
                await ExecuteAsync(
                    connection,
                    "SELECT pg_catalog.setval(pg_catalog.pg_get_serial_sequence('public.users', 'id'), 41, true)");
            }

            var after = await RunFingerprintAsync();

            after.Fingerprint.Should().Be(before.Fingerprint);
            after.CanonicalQuranData.Should().Be(before.CanonicalQuranData);
            after.SystemCatalogue.Should().Be(before.SystemCatalogue);
            after.SchemaState.Should().Be(before.SchemaState);
            after.DumpFilesRetained.Should().Be(0);
            Directory.GetFiles(outputDirectory.FullName).Should().BeEmpty();
        }
        finally
        {
            await using var connection = new NpgsqlConnection(fixture.ConnectionString);
            await connection.OpenAsync();
            await ExecuteAsync(
                connection,
                "UPDATE public.linking_data_state SET generation = 1, updated_at_utc = '1970-01-01 00:00:00+00' WHERE id = 1");
            outputDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Fingerprint_DetectsEveryProtectedStateFamily()
    {
        await AssertProtectedComponentChangesInRolledBackTransactionAsync(
            """
            INSERT INTO public.quran_surahs
                (surah_number, name_arabic, name_simple, name_transliteration, revelation_place, revelation_order, verses_count, bismillah_pre)
            VALUES (1, 'الفاتحة', 'الفاتحة', 'Al-Fatihah', 'makkah', 5, 7, true)
            """,
            component => component.CanonicalQuranData);
        await AssertProtectedComponentChangesInRolledBackTransactionAsync(
            "UPDATE public.roles SET display_name = display_name || '-changed' WHERE id = 1",
            component => component.SystemCatalogue);
        await AssertProtectedComponentChangesInRolledBackTransactionAsync(
            "CREATE INDEX ticket_154_schema_probe ON public.linking_data_state (generation)",
            component => component.SchemaState);
        await AssertProtectedComponentChangesInRolledBackTransactionAsync(
            "ALTER TABLE public.linking_data_state ADD CONSTRAINT ticket_154_constraint_probe CHECK (generation > 0)",
            component => component.SchemaState);
        await AssertProtectedComponentChangesInRolledBackTransactionAsync(
            "CREATE VIEW public.ticket_154_view_probe AS SELECT generation FROM public.linking_data_state",
            component => component.SchemaState);
        await AssertProtectedComponentChangesInRolledBackTransactionAsync(
            "CREATE FUNCTION public.ticket_154_function_probe() RETURNS integer LANGUAGE sql IMMUTABLE AS 'SELECT 154'",
            component => component.SchemaState);
        await AssertProtectedComponentChangesInRolledBackTransactionAsync(
            "CREATE TYPE public.ticket_154_type_probe AS ENUM ('protected')",
            component => component.SchemaState);
        await AssertProtectedComponentChangesInRolledBackTransactionAsync(
            "UPDATE public.\"__EFMigrationsHistory\" SET \"ProductVersion\" = \"ProductVersion\" || '-changed'",
            component => component.SchemaState);
        await AssertProtectedSequenceCounterChangesInRolledBackTransactionAsync();
    }

    [Fact]
    public async Task Fingerprint_WithVerifiedCanonicalBoundary_RechecksCatalogueAndSchemaWithoutRescanningCanonicalData()
    {
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var baseline = await ProtectedStateFingerprint.ComputeAsync(connection, transaction, contract);
            var unchanged = await ProtectedStateFingerprint.ComputeWithVerifiedCanonicalAsync(
                connection,
                transaction,
                contract,
                baseline.Components.CanonicalQuranData);

            unchanged.Should().BeEquivalentTo(baseline);

            await ExecuteAsync(
                connection,
                transaction,
                "UPDATE public.roles SET display_name = display_name || '-changed' WHERE id = 1");
            await ExecuteAsync(
                connection,
                transaction,
                "CREATE INDEX ticket_158_schema_probe ON public.linking_data_state (generation)");

            var changed = await ProtectedStateFingerprint.ComputeWithVerifiedCanonicalAsync(
                connection,
                transaction,
                contract,
                baseline.Components.CanonicalQuranData);

            changed.Fingerprint.Should().NotBe(baseline.Fingerprint);
            changed.Components.CanonicalQuranData.Should().Be(baseline.Components.CanonicalQuranData);
            changed.Components.SystemCatalogue.Should().NotBe(baseline.Components.SystemCatalogue);
            changed.Components.SchemaState.Should().NotBe(baseline.Components.SchemaState);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private async Task AssertProtectedSequenceCounterChangesInRolledBackTransactionAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await ExecuteAsync(connection, transaction, "CREATE SEQUENCE public.ticket_154_protected_sequence_probe");
            var baseline = await ComputeFingerprintAsync(connection, transaction);
            await ExecuteAsync(connection, transaction, "SELECT nextval('public.ticket_154_protected_sequence_probe')");
            var changed = await ComputeFingerprintAsync(connection, transaction);

            changed.Fingerprint.Should().NotBe(baseline.Fingerprint);
            changed.SchemaState.Should().NotBe(baseline.SchemaState);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private async Task AssertProtectedComponentChangesInRolledBackTransactionAsync(
        string mutateSql,
        Func<FingerprintResult, string> component)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var baseline = await ComputeFingerprintAsync(connection, transaction);
            await ExecuteAsync(connection, transaction, mutateSql);
            var changed = await ComputeFingerprintAsync(connection, transaction);
            changed.Fingerprint.Should().NotBe(baseline.Fingerprint);
            component(changed).Should().NotBe(component(baseline));
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private async Task<FingerprintResult> RunFingerprintAsync()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            ["fingerprint", "--contract", TestRuntimeTestPaths.ContractPath],
            output,
            error,
            name => name == TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? fixture.ConnectionString
                : null);

        exitCode.Should().Be(0, $"stderr: {error}{Environment.NewLine}stdout: {output}");
        error.ToString().Should().BeEmpty();
        using var document = JsonDocument.Parse(output.ToString());
        var fingerprint = document.RootElement.GetProperty("protectedStateFingerprint");
        return new FingerprintResult(
            fingerprint.GetProperty("fingerprint").GetString()!,
            fingerprint.GetProperty("components").GetProperty("canonicalQuranData").GetString()!,
            fingerprint.GetProperty("components").GetProperty("systemCatalogue").GetString()!,
            fingerprint.GetProperty("components").GetProperty("schemaState").GetString()!,
            fingerprint.GetProperty("dumpFilesRetained").GetInt32());
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<FingerprintResult> ComputeFingerprintAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var report = await ProtectedStateFingerprint.ComputeAsync(connection, transaction, contract);
        return new FingerprintResult(
            report.Fingerprint,
            report.Components.CanonicalQuranData,
            report.Components.SystemCatalogue,
            report.Components.SchemaState,
            report.DumpFilesRetained);
    }

    private sealed record FingerprintResult(
        string Fingerprint,
        string CanonicalQuranData,
        string SystemCatalogue,
        string SchemaState,
        int DumpFilesRetained);
}
