using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.SimpleI3rabGeneration;

public static class SegmentSignatureBuilder
{
    // Attached pronouns (ضمير متصل) glue the person onto the POS token, e.g. "SUFFIX|PRON:3MP",
    // unlike stems which carry the person as its own token, e.g. "STEM|POS:PRON|3MS".
    private const string GluedPronounPrefix = "PRON:";
    private static readonly string[] VerbTenseMarkers = ["PERF", "IMPF", "IMPV"];
    private static readonly string[] PersonTokens =
    [
        "3MD", "3FD", "2MD", "2FD", "3MP", "3FP", "2MP", "2FP", "3MS", "3FS", "2MS", "2FS",
        "3D", "2D", "1P", "1S"
    ];

    public static string Build(I3rabSegmentInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parts = new List<string> { input.Kind, input.Pos };
        var features = ParseFeatureTokens(input.FeaturesRaw);

        if (input.IsAllahLemma && input.Kind == "STEM" && input.Pos == "PN")
        {
            parts.Add("ALLAH");
            if (TryGetCaseCode(features, input.CaseFeature) is { } allahCase)
            {
                parts.Add(allahCase);
            }

            return string.Join(':', parts);
        }

        if (input.Kind == "STEM" && input.Pos == "V")
        {
            AppendVerbSignature(parts, input, features);
            return string.Join(':', parts);
        }

        if (input.Pos is "N" or "PN" or "ADJ")
        {
            var caseCode = TryGetCaseCode(features, input.CaseFeature);
            if (caseCode is not null)
            {
                parts.Add(caseCode);
            }

            if (input.Kind == "STEM" && input.Pos == "N" && caseCode == "GEN" && features.Contains("1S"))
            {
                parts.Add("1S");
            }

            return string.Join(':', parts);
        }

        if (input.Pos == "PRON")
        {
            if (TryGetPersonToken(features) is { } person)
            {
                parts.Add(person);
            }

            return string.Join(':', parts);
        }

        if (input.Kind == "STEM" && input.Pos == "DEM" && TryGetPersonToken(features) is { } demPerson)
        {
            parts.Add(demPerson);
        }

        return string.Join(':', parts);
    }

    private static void AppendVerbSignature(
        List<string> parts,
        I3rabSegmentInput input,
        HashSet<string> features)
    {
        var tense = MapVerbTenseMarker(input.VerbTense) ?? VerbTenseMarkers.FirstOrDefault(features.Contains);
        var voice = features.Contains("PASS") || string.Equals(input.VerbVoice, "passive", StringComparison.Ordinal)
            ? "PASS"
            : "ACT";
        var person = TryGetPersonToken(features);

        if (tense is not null)
        {
            parts.Add(tense);
        }

        parts.Add(voice);

        if (person is not null)
        {
            parts.Add(person);
        }
    }

    private static string? MapVerbTenseMarker(string? verbTense) =>
        verbTense switch
        {
            "past" => "PERF",
            "present" => "IMPF",
            "imperative" => "IMPV",
            "PERF" or "IMPF" or "IMPV" => verbTense,
            _ => null
        };

    private static string? TryGetCaseCode(HashSet<string> features, string? caseFeature)
    {
        if (features.Contains("NOM") || string.Equals(caseFeature, "nominative", StringComparison.Ordinal))
        {
            return "NOM";
        }

        if (features.Contains("ACC") || string.Equals(caseFeature, "accusative", StringComparison.Ordinal))
        {
            return "ACC";
        }

        if (features.Contains("GEN") || string.Equals(caseFeature, "genitive", StringComparison.Ordinal))
        {
            return "GEN";
        }

        return null;
    }

    private static string? TryGetPersonToken(HashSet<string> features)
    {
        var standalonePerson = PersonTokens.FirstOrDefault(features.Contains);
        if (standalonePerson is not null)
        {
            return standalonePerson;
        }

        // Recover the person from an attached-pronoun token such as "PRON:3MP" that the feature
        // tokenizer keeps intact (it splits on '|' and ' ', not ':').
        foreach (var token in features)
        {
            if (token.StartsWith(GluedPronounPrefix, StringComparison.Ordinal))
            {
                var person = token[GluedPronounPrefix.Length..];
                if (PersonTokens.Contains(person))
                {
                    return person;
                }
            }
        }

        return null;
    }

    private static HashSet<string> ParseFeatureTokens(string? featuresRaw)
    {
        if (string.IsNullOrWhiteSpace(featuresRaw))
        {
            return [];
        }

        return featuresRaw
            .Split(['|', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }
}
