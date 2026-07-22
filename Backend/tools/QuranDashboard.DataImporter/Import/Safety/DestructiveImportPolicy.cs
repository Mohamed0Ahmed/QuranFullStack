namespace QuranDashboard.DataImporter.Import.Safety;

public sealed record DestructiveImportDecision(bool Allowed, string Reason);

// A --force import truncates committed Quran tables; refuse it in Production and require explicit opt-in.
public static class DestructiveImportPolicy
{
    public const string AllowEnvVar = "QURANDASHBOARD_ALLOW_DESTRUCTIVE_IMPORT";

    private static readonly string[] EnvironmentEnvVars = ["DOTNET_ENVIRONMENT", "ASPNETCORE_ENVIRONMENT"];

    public static DestructiveImportDecision Evaluate(bool force) =>
        Evaluate(force, Environment.GetEnvironmentVariable);

    public static DestructiveImportDecision Evaluate(bool force, Func<string, string?> readEnv)
    {
        ArgumentNullException.ThrowIfNull(readEnv);

        if (!force)
        {
            return new DestructiveImportDecision(true, "Non-destructive import; no environment restriction applies.");
        }

        var environment = EnvironmentEnvVars
            .Select(readEnv)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
        {
            return new DestructiveImportDecision(
                false,
                "Destructive --force imports are refused in the Production environment.");
        }

        if (!IsTruthy(readEnv(AllowEnvVar)))
        {
            return new DestructiveImportDecision(
                false,
                $"Destructive --force imports require opt-in: set {AllowEnvVar}=1.");
        }

        return new DestructiveImportDecision(true, "Destructive --force import explicitly permitted for this environment.");
    }

    private static bool IsTruthy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        return normalized is "1"
            || normalized.Equals("true", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}
