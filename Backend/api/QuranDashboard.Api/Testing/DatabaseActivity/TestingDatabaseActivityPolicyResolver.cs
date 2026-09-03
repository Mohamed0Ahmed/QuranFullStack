using QuranDashboard.Infrastructure.Testing.DatabaseActivity;

namespace QuranDashboard.Api.Testing.DatabaseActivity;

internal static class TestingDatabaseActivityPolicyResolver
{
    private const string SectionName = "Testing:DatabaseActivity";

    internal static DatabaseActivityPolicy Resolve(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsEnvironment("Testing"))
        {
            return DatabaseActivityPolicy.Production;
        }

        var profileValue = configuration[$"{SectionName}:Profile"];
        var profile = profileValue switch
        {
            "ReadOnly" => DatabaseActivityProfile.ReadOnly,
            "Mutable" => DatabaseActivityProfile.Mutable,
            "DestructiveRehearsal" => DatabaseActivityProfile.DestructiveRehearsal,
            null or "" => throw new InvalidOperationException(
                $"{SectionName}:Profile is required in the Testing environment."),
            _ => throw new InvalidOperationException(
                $"{SectionName}:Profile has unknown value '{profileValue}'."),
        };

        var activities = configuration.GetSection($"{SectionName}:EnabledBackgroundActivities")
            .GetChildren()
            .Select(entry => entry.Value switch
            {
                "LinkingPreparedPreflightProcessor" => DatabaseBackgroundActivity.LinkingPreparedPreflightProcessor,
                "LinkingConfirmationJobProcessor" => DatabaseBackgroundActivity.LinkingConfirmationJobProcessor,
                "LinkingPreparedPreflightCleanup" => DatabaseBackgroundActivity.LinkingPreparedPreflightCleanup,
                "LinkingConfirmationJobCleanup" => DatabaseBackgroundActivity.LinkingConfirmationJobCleanup,
                null or "" => throw new InvalidOperationException(
                    $"{SectionName}:EnabledBackgroundActivities contains a blank value."),
                var value => throw new InvalidOperationException(
                    $"{SectionName}:EnabledBackgroundActivities contains unknown value '{value}'."),
            })
            .ToArray();
        if (activities.Length != activities.Distinct().Count())
        {
            throw new InvalidOperationException(
                $"{SectionName}:EnabledBackgroundActivities contains a duplicate value.");
        }

        var validatedTarget = ResolveValidatedRehearsalTarget(configuration);
        return DatabaseActivityPolicy.Testing(profile, activities, validatedTarget);
    }

    private static ValidatedRehearsalTarget? ResolveValidatedRehearsalTarget(IConfiguration configuration)
    {
        var section = configuration.GetSection($"{SectionName}:ValidatedRehearsalTarget");
        var kindValue = section["Kind"];
        var database = section["Database"];
        var subtype = section["Subtype"];
        if (kindValue is null && database is null && subtype is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException(
                $"{SectionName}:ValidatedRehearsalTarget:Database is required when a validation receipt is supplied.");
        }

        if (string.IsNullOrWhiteSpace(subtype))
        {
            throw new InvalidOperationException(
                $"{SectionName}:ValidatedRehearsalTarget:Subtype is required when a validation receipt is supplied.");
        }

        var kind = kindValue switch
        {
            "scratch-empty" => RehearsalTargetKind.ScratchEmpty,
            "rehearsal-full" => RehearsalTargetKind.RehearsalFull,
            _ => throw new InvalidOperationException(
                $"{SectionName}:ValidatedRehearsalTarget:Kind has unknown value '{kindValue}'."),
        };
        return new ValidatedRehearsalTarget(kind, database, subtype);
    }
}
