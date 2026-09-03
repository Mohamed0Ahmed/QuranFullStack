namespace QuranDashboard.Infrastructure.Testing.DatabaseActivity;

public sealed class DatabaseActivityPolicy
{
    private const string DevelopmentDatabase = "quran_dashboard";
    private const string TestDatabase = "quran_dashboard_test";
    private const string ScratchDatabasePrefix = "quran_test_scratch_";
    private const string ReaderRole = "quran_dashboard_test_reader";
    private const string ApplicationRole = "quran_dashboard_test_application";
    private const string RehearsalEnabledMarker = "quran_dashboard.test_runtime.rehearsal_enabled";
    private const string RehearsalSubtypeMarker = "quran_dashboard.test_runtime.rehearsal_subtype";
    private const string CanonicalPipelineMarker = "quran_dashboard.test_runtime.canonical_pipeline";
    private const string CanonicalInputProvenanceMarker = "quran_dashboard.test_runtime.canonical_input_provenance";
    private const string CanonicalQuranFingerprintMarker = "quran_dashboard.test_runtime.canonical_quran_fingerprint";
    private const string SystemCatalogueFingerprintMarker = "quran_dashboard.test_runtime.system_catalogue_fingerprint";
    private const string MigrationHeadMarker = "quran_dashboard.test_runtime.migration_head";
    private const string RefreshedAtUtcMarker = "quran_dashboard.test_runtime.refreshed_at_utc";
    private static readonly IReadOnlySet<string> RehearsalSubtypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "canonical-import",
        "canonical-rebuild",
        "canonical-generation",
        "migration",
        "system-catalogue-reconciliation",
        "schema-drift",
        "phrase-search-index-build",
        "recovery",
    };

    private readonly IReadOnlySet<DatabaseBackgroundActivity> enabledBackgroundActivities;

    private DatabaseActivityPolicy(
        DatabaseActivityProfile? profile,
        IEnumerable<DatabaseBackgroundActivity> enabledBackgroundActivities,
        ValidatedRehearsalTarget? validatedRehearsalTarget = null)
    {
        Profile = profile;
        this.enabledBackgroundActivities = enabledBackgroundActivities.ToHashSet();
        ValidatedRehearsalTarget = validatedRehearsalTarget;
    }

    public static DatabaseActivityPolicy Production { get; } = new(
        null,
        Enum.GetValues<DatabaseBackgroundActivity>());

    public DatabaseActivityProfile? Profile { get; }

    public ValidatedRehearsalTarget? ValidatedRehearsalTarget { get; }

    public bool IsTesting => Profile is not null;

    public bool AllowPermissionCatalogueSynchronization => !IsTesting;

    public static DatabaseActivityPolicy Testing(
        DatabaseActivityProfile profile,
        IEnumerable<DatabaseBackgroundActivity> enabledBackgroundActivities,
        ValidatedRehearsalTarget? validatedRehearsalTarget = null)
    {
        ArgumentNullException.ThrowIfNull(enabledBackgroundActivities);
        var activities = enabledBackgroundActivities.ToArray();
        if (profile == DatabaseActivityProfile.ReadOnly && activities.Length != 0)
        {
            throw new InvalidOperationException(
                "The ReadOnly database activity profile cannot enable background activity.");
        }

        if (profile == DatabaseActivityProfile.DestructiveRehearsal && activities.Length != 0)
        {
            throw new InvalidOperationException(
                "The DestructiveRehearsal database activity profile cannot enable ordinary background activity.");
        }

        if (profile != DatabaseActivityProfile.DestructiveRehearsal && validatedRehearsalTarget is not null)
        {
            throw new InvalidOperationException(
                "A validated rehearsal target can be supplied only for the DestructiveRehearsal profile.");
        }

        return new DatabaseActivityPolicy(profile, activities, validatedRehearsalTarget);
    }

    public bool Enables(DatabaseBackgroundActivity activity) =>
        enabledBackgroundActivities.Contains(activity);

    internal string ApplyToConnectionString(string connectionString)
    {
        if (!IsTesting)
        {
            return connectionString;
        }

        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        if (connection.Database == DevelopmentDatabase)
        {
            throw new InvalidOperationException(
                "Testing database activity profiles refuse the Development Database 'quran_dashboard'.");
        }

        if (Profile == DatabaseActivityProfile.DestructiveRehearsal)
        {
            ValidateRehearsalTarget(connection);
        }

        if (connection.NoResetOnClose)
        {
            throw new InvalidOperationException(
                "Testing database activity profiles require pooled connections to reset on close.");
        }

        var options = new List<string>();
        if (!string.IsNullOrWhiteSpace(connection.Options))
        {
            options.Add(connection.Options);
        }

        // Capability roles apply to the stable Test Database. Disposable targets remain usable only
        // during the accepted architecture's staged migration and are removed by the later cutover.
        if (connection.Database == TestDatabase)
        {
            var role = Profile == DatabaseActivityProfile.ReadOnly ? ReaderRole : ApplicationRole;
            options.Add($"-c role={role}");
        }

        options.Add(Profile == DatabaseActivityProfile.ReadOnly
            ? "-c default_transaction_read_only=on"
            : "-c default_transaction_read_only=off");
        connection.Options = string.Join(' ', options);
        connection.ApplicationName = $"quran-dashboard-api-testing-{Profile!.Value.ToString().ToLowerInvariant()}";
        return connection.ConnectionString;
    }

    private void ValidateRehearsalTarget(NpgsqlConnectionStringBuilder target)
    {
        var database = target.Database;
        if (database == TestDatabase)
        {
            throw new InvalidOperationException(
                "DestructiveRehearsal refuses the persistent Test Database 'quran_dashboard_test'.");
        }

        if (ValidatedRehearsalTarget is null)
        {
            throw new InvalidOperationException(
                "DestructiveRehearsal refuses an unvalidated database target; a TestRuntime-validated scratch or full Rehearsal Database is required.");
        }

        if (!string.Equals(database, ValidatedRehearsalTarget.Database, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "DestructiveRehearsal refuses a database that does not match the TestRuntime validation receipt.");
        }

        var validTargetShape = ValidatedRehearsalTarget.Kind switch
        {
            RehearsalTargetKind.ScratchEmpty =>
                !string.IsNullOrWhiteSpace(database)
                && database.StartsWith(ScratchDatabasePrefix, StringComparison.Ordinal)
                && database.Length > ScratchDatabasePrefix.Length,
            RehearsalTargetKind.RehearsalFull =>
                !string.IsNullOrWhiteSpace(database)
                && !database.StartsWith(ScratchDatabasePrefix, StringComparison.Ordinal),
            _ => false,
        };
        if (!validTargetShape)
        {
            throw new InvalidOperationException(
                "DestructiveRehearsal refuses a target whose database identity does not match its TestRuntime validation receipt.");
        }

        if (!IsLocalEndpoint(target.Host))
        {
            throw new InvalidOperationException(
                "DestructiveRehearsal refuses a non-local database target.");
        }

        ValidateRehearsalMarkers(target);
    }

    private void ValidateRehearsalMarkers(NpgsqlConnectionStringBuilder target)
    {
        var validationConnection = new NpgsqlConnectionStringBuilder(target.ConnectionString)
        {
            Pooling = false,
        };
        try
        {
            using var connection = new NpgsqlConnection(validationConnection.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT pg_get_userbyid(database.datdba), session_user "
                + "FROM pg_catalog.pg_database AS database WHERE database.datname = current_database()";
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                throw new InvalidOperationException(
                    "DestructiveRehearsal requires TestRuntime rehearsal markers on the target database.");
            }

            var databaseOwner = reader.GetString(0);
            var sessionUser = reader.GetString(1);
            reader.Close();

            var rehearsalEnabled = ReadDatabaseSetting(connection, RehearsalEnabledMarker);
            var rehearsalSubtype = ReadDatabaseSetting(connection, RehearsalSubtypeMarker);
            if (!string.Equals(rehearsalEnabled, "true", StringComparison.Ordinal)
                || !RehearsalSubtypes.Contains(rehearsalSubtype ?? string.Empty)
                || !string.Equals(
                    rehearsalSubtype,
                    ValidatedRehearsalTarget!.Subtype,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "DestructiveRehearsal requires matching TestRuntime rehearsal markers on the target database.");
            }

            if (ValidatedRehearsalTarget.Kind == RehearsalTargetKind.ScratchEmpty)
            {
                if (!string.Equals(databaseOwner, sessionUser, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "DestructiveRehearsal requires the connected login to own the validated scratch database.");
                }

                return;
            }

            var fullMarkerValues = new[]
            {
                ReadDatabaseSetting(connection, CanonicalPipelineMarker),
                ReadDatabaseSetting(connection, CanonicalInputProvenanceMarker),
                ReadDatabaseSetting(connection, CanonicalQuranFingerprintMarker),
                ReadDatabaseSetting(connection, SystemCatalogueFingerprintMarker),
                ReadDatabaseSetting(connection, MigrationHeadMarker),
            };
            if (fullMarkerValues.Any(string.IsNullOrWhiteSpace)
                || !DateTimeOffset.TryParse(ReadDatabaseSetting(connection, RefreshedAtUtcMarker), out _))
            {
                throw new InvalidOperationException(
                    "DestructiveRehearsal requires complete TestRuntime provenance, fingerprint, migration, and provisioning markers on a full Rehearsal Database.");
            }
        }
        catch (NpgsqlException exception)
        {
            throw new InvalidOperationException(
                "DestructiveRehearsal could not validate the target database through TestRuntime markers.",
                exception);
        }
    }

    private static bool IsLocalEndpoint(string? host) =>
        host is "localhost" or "127.0.0.1" or "::1"
        || (!string.IsNullOrWhiteSpace(host)
            && Path.IsPathRooted(host)
            && !host.Contains(',', StringComparison.Ordinal));

    private static string? ReadDatabaseSetting(NpgsqlConnection connection, string name)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT substring(configured.setting FROM position('=' IN configured.setting) + 1)
            FROM pg_catalog.pg_db_role_setting AS settings
            INNER JOIN pg_catalog.pg_database AS database ON database.oid = settings.setdatabase
            CROSS JOIN LATERAL unnest(settings.setconfig) AS configured(setting)
            WHERE database.datname = current_database()
              AND settings.setrole = 0
              AND split_part(configured.setting, '=', 1) = @name
            """;
        command.Parameters.AddWithValue("name", name);
        var value = command.ExecuteScalar();
        return value is null or DBNull ? null : (string)value;
    }
}
