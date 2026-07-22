namespace QuranDashboard.DataImporter.Import.Safety;

public sealed record DestructiveImportGateResult(bool Allowed, string Reason, string? Warning);

// US2 (FR-007/008): the importer-side authorization every force/source verb runs before delegating to
// a pipeline handler. It combines the environment restriction on destructive (`--force`) imports with
// pinned canonical source-identity verification. A positively-identified bad source (forbidden / wrong
// identity / unstable IDs) is refused; a legacy package that predates the identity manifest is allowed
// with a warning so existing staged packages keep importing while the manifest is rolled out.
//
// The race-safe advisory lock and the fail-closed FK-closure preflight run deeper, at the destructive
// SQL step, via the shared QuranImportDestructiveGuard that every pipeline writer routes through.
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
