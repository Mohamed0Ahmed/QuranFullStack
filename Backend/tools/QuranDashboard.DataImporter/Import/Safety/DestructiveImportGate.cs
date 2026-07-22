namespace QuranDashboard.DataImporter.Import.Safety;

public sealed record DestructiveImportGateResult(bool Allowed, string Reason, string? Warning);

public static class DestructiveImportGate
{
    public static DestructiveImportGateResult Evaluate(bool force, string? sourcePath) =>
        Evaluate(force, sourcePath, new CanonicalQuranSourceVerifier(), Environment.GetEnvironmentVariable);

    public static DestructiveImportGateResult Evaluate(
        bool force,
        string? sourcePath,
        CanonicalQuranSourceVerifier verifier,
        Func<string, string?> readEnv)
    {
        ArgumentNullException.ThrowIfNull(verifier);
        ArgumentNullException.ThrowIfNull(readEnv);

        var environment = DestructiveImportPolicy.Evaluate(force, readEnv);
        if (!environment.Allowed)
        {
            return new DestructiveImportGateResult(false, environment.Reason, null);
        }

        string? warning = null;
        if (!string.IsNullOrWhiteSpace(sourcePath) && Directory.Exists(sourcePath))
        {
            var identity = verifier.Verify(sourcePath);
            if (!identity.Accepted)
            {
                if (identity.Status == SourceIdentityStatus.MissingManifest)
                {
                    warning = $"Source identity unverified (legacy package): {identity.Message}";
                }
                else
                {
                    return new DestructiveImportGateResult(
                        false, $"Source refused ({identity.Status}): {identity.Message}", null);
                }
            }
        }

        return new DestructiveImportGateResult(true, environment.Reason, warning);
    }
}
