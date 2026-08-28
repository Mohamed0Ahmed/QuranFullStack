using QuranDashboard.Application.Abstractions.Quran.DataPipelines;

namespace QuranDashboard.DataImporter.Import.ArgumentParsing;

internal static class DataImporterProfileArguments
{
    internal static bool TryExtract(
        string[] args,
        out DataImporterProfile profile,
        out string[] remainingArgs,
        out string errorMessage)
    {
        profile = DataImporterProfile.CuratedTen;
        errorMessage = string.Empty;
        var filteredArgs = new List<string>(args.Length);
        var profileWasProvided = false;

        for (var index = 0; index < args.Length; index++)
        {
            if (!string.Equals(args[index], "--profile", StringComparison.Ordinal))
            {
                filteredArgs.Add(args[index]);
                continue;
            }

            if (profileWasProvided)
            {
                remainingArgs = [];
                errorMessage = "--profile may be specified only once.";
                return false;
            }

            if (index + 1 >= args.Length || args[index + 1].StartsWith('-'))
            {
                remainingArgs = [];
                errorMessage = "Missing value for --profile.";
                return false;
            }

            profileWasProvided = true;
            var profileValue = args[++index];
            switch (profileValue)
            {
                case QuranImportProfiles.CuratedTen:
                    profile = DataImporterProfile.CuratedTen;
                    break;
                case QuranImportProfiles.Full:
                    profile = DataImporterProfile.Full;
                    break;
                default:
                    remainingArgs = [];
                    errorMessage = $"Unknown import profile '{profileValue}'. Expected curated-10 or full.";
                    return false;
            }
        }

        remainingArgs = [.. filteredArgs];
        return true;
    }

    internal static string GetValue(DataImporterProfile profile) =>
        profile == DataImporterProfile.Full ? QuranImportProfiles.Full : QuranImportProfiles.CuratedTen;
}

internal enum DataImporterProfile
{
    CuratedTen,
    Full,
}
