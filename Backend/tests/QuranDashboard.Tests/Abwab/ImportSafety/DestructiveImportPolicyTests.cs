using QuranDashboard.DataImporter.Import.Safety;

namespace QuranDashboard.Tests.Abwab.ImportSafety;

// US2 / FR-007: environment restriction on destructive (`--force`) imports, plus the importer-side gate
// that layers canonical source-identity verification on top. Pure (no DB), so exercised directly.
public sealed class DestructiveImportPolicyTests
{
    [Theory]
    // non-force imports are never gated
    [InlineData(false, null, null, true)]
    [InlineData(false, "Production", null, true)]
    // force in Production is refused even with the opt-in set
    [InlineData(true, "Production", "1", false)]
    // force without the opt-in is refused
    [InlineData(true, "Development", null, false)]
    [InlineData(true, null, null, false)]
    // force with the opt-in is allowed outside Production
    [InlineData(true, "Development", "1", true)]
    [InlineData(true, null, "true", true)]
    public void Evaluate_AppliesEnvironmentRestriction(
        bool force, string? environment, string? allowOptIn, bool expectedAllowed)
    {
        var env = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (environment is not null)
        {
            env["DOTNET_ENVIRONMENT"] = environment;
        }

        if (allowOptIn is not null)
        {
            env[DestructiveImportPolicy.AllowEnvVar] = allowOptIn;
        }

        var decision = DestructiveImportPolicy.Evaluate(force, key => env.GetValueOrDefault(key));

        decision.Allowed.Should().Be(expectedAllowed, decision.Reason);
    }

    private static string FixtureDirectory(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Abwab", "ImportSafety", "_fixtures", name);

    private static Func<string, string?> OptedInEnv() =>
        key => key == DestructiveImportPolicy.AllowEnvVar ? "1" : null;

    [Fact]
    public void Gate_AllowsMissingManifestSource_WithWarning()
    {
        // Documented FR-008 accommodation: a legacy package predating the identity manifest is allowed
        // to keep importing, but the gate surfaces a warning rather than silently trusting it.
        var result = DestructiveImportGate.Evaluate(
            force: true,
            FixtureDirectory("no-manifest-source"),
            new CanonicalQuranSourceVerifier(),
            OptedInEnv());

        result.Allowed.Should().BeTrue(result.Reason);
        result.Warning.Should().NotBeNull();
    }

    [Fact]
    public void Gate_RefusesForbiddenSource_EvenWhenOptedIn()
    {
        var result = DestructiveImportGate.Evaluate(
            force: true,
            FixtureDirectory("forbidden-source"),
            new CanonicalQuranSourceVerifier(),
            OptedInEnv());

        result.Allowed.Should().BeFalse();
    }

    [Fact]
    public void Gate_RefusesForceWithoutOptIn_BeforeCheckingSource()
    {
        var result = DestructiveImportGate.Evaluate(
            force: true,
            FixtureDirectory("canonical-source"),
            new CanonicalQuranSourceVerifier(),
            readEnv: _ => null);

        result.Allowed.Should().BeFalse("the environment restriction must refuse force before a source is considered");
    }
}
