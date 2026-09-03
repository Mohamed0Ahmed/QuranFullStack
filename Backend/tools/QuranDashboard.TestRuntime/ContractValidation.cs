using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.TestRuntime;

internal sealed record ContractViolation(string Code, string? Subject = null);

internal sealed record ContractValidationResult(
    bool IsValid,
    int ContractVersion,
    int MappedTableCount,
    int SchemaTableCount,
    IReadOnlyList<string> ExpectedMigrations,
    IReadOnlyList<ContractViolation> Violations);

internal static partial class DatabaseContractValidator
{
    private static readonly string[] ExpectedTargetKinds = ["rehearsal-full", "scratch-empty", "test-capability"];
    private static readonly string[] ExpectedRehearsalSubtypes =
    [
        "canonical-generation",
        "canonical-import",
        "canonical-rebuild",
        "migration",
        "phrase-search-index-build",
        "recovery",
        "schema-drift",
        "system-catalogue-reconciliation",
    ];

    internal static ContractValidationResult Validate(DatabaseContract contract)
    {
        var violations = new List<ContractViolation>();
        ValidateStructure(contract, violations);

        using var db = CreateModelContext();
        var mappedTables = db.Model.GetEntityTypes()
            .Select(entityType => (Schema: entityType.GetSchema(), Table: entityType.GetTableName()))
            .Where(mapping => mapping.Table is not null)
            .Select(mapping => (mapping.Schema, Table: mapping.Table!))
            .Distinct()
            .OrderBy(mapping => mapping.Table, StringComparer.Ordinal)
            .ToArray();

        foreach (var mapping in mappedTables.Where(mapping => mapping.Schema is not null and not "public"))
        {
            violations.Add(new ContractViolation(
                "contract.model.unsupported-schema",
                $"{mapping.Schema}.{mapping.Table}"));
        }

        var modelTableNames = mappedTables.Select(mapping => mapping.Table).ToHashSet(StringComparer.Ordinal);
        var classifiedTables = contract.AllTables().Select(entry => entry.Table).ToHashSet(StringComparer.Ordinal);
        foreach (var table in modelTableNames.Except(classifiedTables, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            violations.Add(new ContractViolation("contract.model.unclassified-table", table));
        }

        foreach (var table in contract.ApplicationTables().Select(entry => entry.Table)
                     .Except(modelTableNames, StringComparer.Ordinal)
                     .Order(StringComparer.Ordinal))
        {
            violations.Add(new ContractViolation("contract.model.unknown-table", table));
        }

        foreach (var table in modelTableNames.Order(StringComparer.Ordinal))
        {
            var expectedClass = table.StartsWith("quran_", StringComparison.Ordinal)
                ? "canonicalQuranData"
                : table is "roles" or "permissions"
                    ? "systemCatalogue"
                    : "mutableApplicationState";
            var actualClass = contract.AllTables()
                .Where(entry => entry.Table == table)
                .Select(entry => entry.DataClass)
                .FirstOrDefault();
            if (actualClass is not null && actualClass != expectedClass)
            {
                violations.Add(new ContractViolation(
                    "contract.model.classification-mismatch",
                    table));
            }
        }

        var orderedViolations = violations
            .Distinct()
            .OrderBy(violation => violation.Code, StringComparer.Ordinal)
            .ThenBy(violation => violation.Subject, StringComparer.Ordinal)
            .ToArray();
        return new ContractValidationResult(
            orderedViolations.Length == 0,
            contract.ContractVersion,
            mappedTables.Length,
            contract.DataClasses.SchemaState.Length,
            db.Database.GetMigrations().ToArray(),
            orderedViolations);
    }

    private static void ValidateStructure(DatabaseContract contract, ICollection<ContractViolation> violations)
    {
        AddValueViolation(contract.ContractVersion == 1, "contract.version.unsupported", violations);
        AddValueViolation(contract.CapabilityMetadataVersion > 0, "contract.capability-version.invalid", violations);
        AddValueViolation(contract.PostgresMajorVersion == 18, "contract.postgres-version.invalid", violations);

        var allTables = contract.AllTables().ToArray();
        foreach (var duplicate in allTables.GroupBy(entry => entry.Table, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key)
                     .Order(StringComparer.Ordinal))
        {
            violations.Add(new ContractViolation("contract.table.duplicate", duplicate));
        }

        foreach (var table in allTables.Select(entry => entry.Table)
                     .Where(table => string.IsNullOrWhiteSpace(table) || !TableNamePattern().IsMatch(table))
                     .Order(StringComparer.Ordinal))
        {
            violations.Add(new ContractViolation("contract.table.invalid-name", ReportSubject(table)));
        }

        AddValueViolation(
            contract.DataClasses.SchemaState.SequenceEqual(["__EFMigrationsHistory"], StringComparer.Ordinal),
            "contract.schema-state.invalid",
            violations);
        AddValueViolation(
            contract.LinkingDataBaseline.Table == "linking_data_state"
            && contract.LinkingDataBaseline.Id == 1
            && contract.LinkingDataBaseline.Generation == 1
            && contract.LinkingDataBaseline.UpdatedAtUtc == DateTimeOffset.UnixEpoch,
            "contract.linking-baseline.invalid",
            violations);
        AddValueViolation(
            contract.SystemCatalogue.OwnerRole is { Id: 1, Name: "Owner" }
            && !string.IsNullOrWhiteSpace(contract.SystemCatalogue.OwnerRole.DisplayName),
            "contract.owner-role.invalid",
            violations);

        ValidateNamedValues(contract.Roles.AsDictionary(), "contract.role", RoleNamePattern(), violations);
        ValidateNamedValues(contract.Markers.AsDictionary(), "contract.marker", MarkerNamePattern(), violations);
        AddValueViolation(
            contract.Targets.DevelopmentDatabase == "quran_dashboard"
            && contract.Targets.TestDatabase == "quran_dashboard_test"
            && contract.Targets.ScratchPrefix == "quran_test_scratch_"
            && contract.Targets.RefreshPrefix == "quran_dashboard_test_refresh_",
            "contract.targets.invalid",
            violations);
        AddValueViolation(
            contract.Targets.AllowedDatabaseTargets.Order(StringComparer.Ordinal)
                .SequenceEqual(ExpectedTargetKinds, StringComparer.Ordinal),
            "contract.target-kinds.invalid",
            violations);
        AddValueViolation(contract.AdvisoryLock.Key != 0, "contract.advisory-lock.invalid", violations);
        AddValueViolation(
            contract.RehearsalSubtypes.Order(StringComparer.Ordinal)
                .SequenceEqual(ExpectedRehearsalSubtypes, StringComparer.Ordinal),
            "contract.rehearsal-subtypes.invalid",
            violations);
    }

    private static void ValidateNamedValues(
        IReadOnlyDictionary<string, string> values,
        string codePrefix,
        Regex pattern,
        ICollection<ContractViolation> violations)
    {
        foreach (var entry in values.Where(entry => string.IsNullOrWhiteSpace(entry.Value) || !pattern.IsMatch(entry.Value)))
        {
            violations.Add(new ContractViolation($"{codePrefix}.invalid", entry.Key));
        }

        foreach (var duplicate in values.GroupBy(entry => entry.Value, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            violations.Add(new ContractViolation($"{codePrefix}.duplicate", ReportSubject(duplicate.Key)));
        }
    }

    private static QuranDashboardDbContext CreateModelContext()
    {
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql("Host=localhost;Database=quran_dashboard_test;Username=test_runtime_contract")
            .Options;
        return new QuranDashboardDbContext(options);
    }

    private static void AddValueViolation(
        bool condition,
        string code,
        ICollection<ContractViolation> violations)
    {
        if (!condition)
        {
            violations.Add(new ContractViolation(code));
        }
    }

    private static string ReportSubject(string? value) => string.IsNullOrWhiteSpace(value) ? "empty" : value;

    [GeneratedRegex("^(?:[a-z][a-z0-9_]*|__EFMigrationsHistory)$", RegexOptions.CultureInvariant)]
    private static partial Regex TableNamePattern();

    [GeneratedRegex("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex RoleNamePattern();

    [GeneratedRegex("^[a-z][a-z0-9_]*(?:\\.[a-z][a-z0-9_]*)+$", RegexOptions.CultureInvariant)]
    private static partial Regex MarkerNamePattern();
}
