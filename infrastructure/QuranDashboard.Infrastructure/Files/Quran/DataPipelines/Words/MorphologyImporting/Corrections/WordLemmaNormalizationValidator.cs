namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

// Schema validator for the word-level lemma normalization artifact. Fails closed on any malformed,
// non-final, duplicated, or evidence-lacking entry before the artifact is applied to the lemma map.
internal static class WordLemmaNormalizationValidator
{
    public static void ValidateSchema(WordLemmaNormalizationLoaded loaded)
    {
        ArgumentNullException.ThrowIfNull(loaded);

        var artifact = loaded.Artifact;
        FailWhen(artifact.SchemaVersion != WordLemmaNormalizationArtifact.SupportedSchemaVersion,
            $"Unsupported word-lemma-normalization schemaVersion '{artifact.SchemaVersion}' "
            + $"(expected {WordLemmaNormalizationArtifact.SupportedSchemaVersion}).");

        FailWhen(string.IsNullOrWhiteSpace(artifact.ArtifactId),
            "word-lemma-normalization artifact is missing artifactId.");
        FailWhen(artifact.Entries.Count == 0,
            "word-lemma-normalization artifact has no entries.");

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var seenLocations = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in artifact.Entries)
        {
            ValidateEntry(entry, seenIds, seenLocations, loaded.Evidence);
        }
    }

    private static void ValidateEntry(
        WordLemmaNormalizationEntry entry,
        HashSet<string> seenIds,
        HashSet<string> seenLocations,
        IReadOnlyList<WordLemmaMappingEvidenceEntry> evidence)
    {
        var where = Describe(entry);

        FailWhen(string.IsNullOrWhiteSpace(entry.Id), "An entry is missing id.");
        FailWhen(!seenIds.Add(entry.Id), $"Duplicate normalization entry id '{entry.Id}'.");
        FailWhen(string.IsNullOrWhiteSpace(entry.Location), $"Entry '{entry.Id}' is missing location.");
        FailWhen(IsInvalidLocation(entry.Location),
            $"{where}: invalid location format '{entry.Location}' (expected S:V:W digits).");
        FailWhen(!seenLocations.Add(entry.Location),
            $"{where}: duplicate operation location '{entry.Location}'.");

        // Draft-only statuses must never reach the active artifact.
        FailWhen(entry.DecisionStatus is WordLemmaNormalizationDecisionStatus.Candidate
                    or WordLemmaNormalizationDecisionStatus.NeedsReview,
            $"{where}: non-final decisionStatus '{entry.DecisionStatus}' "
            + "(candidate/needs-review belong only in the draft artifact).");

        // Operation shape rules (spec §3.2).
        switch (entry.OperationKind)
        {
            case WordLemmaNormalizationOperationKind.Add:
                FailWhen(entry.ExpectedCurrentLemmaArabic is not null,
                    $"{where}: 'add' must have null expectedCurrentLemmaArabic "
                    + $"(got '{entry.ExpectedCurrentLemmaArabic}').");
                FailWhen(string.IsNullOrWhiteSpace(entry.CorrectedLemmaArabic),
                    $"{where}: 'add' must have a non-null correctedLemmaArabic.");
                RequireMappingEvidence(entry, evidence, where);
                break;

            case WordLemmaNormalizationOperationKind.Remove:
                FailWhen(string.IsNullOrWhiteSpace(entry.ExpectedCurrentLemmaArabic),
                    $"{where}: 'remove' must have a non-null expectedCurrentLemmaArabic.");
                FailWhen(entry.CorrectedLemmaArabic is not null,
                    $"{where}: 'remove' must have null correctedLemmaArabic "
                    + $"(got '{entry.CorrectedLemmaArabic}').");
                break;

            case WordLemmaNormalizationOperationKind.Replace:
                FailWhen(string.IsNullOrWhiteSpace(entry.ExpectedCurrentLemmaArabic),
                    $"{where}: 'replace' must have a non-null expectedCurrentLemmaArabic.");
                FailWhen(string.IsNullOrWhiteSpace(entry.CorrectedLemmaArabic),
                    $"{where}: 'replace' must have a non-null correctedLemmaArabic.");
                FailWhen(string.Equals(entry.ExpectedCurrentLemmaArabic, entry.CorrectedLemmaArabic,
                            StringComparison.Ordinal),
                    $"{where}: 'replace' correctedLemmaArabic must differ from expectedCurrentLemmaArabic.");
                RequireMappingEvidence(entry, evidence, where);
                break;

            case WordLemmaNormalizationOperationKind.Keep:
                // Non-mutating: corrected must equal expected, or both may be null (a reviewed
                // "valid null / absence accepted" decision where QUL has no lemma and Corpus has no
                // reliable mapping). Any other mismatch is a forbidden mutation.
                FailWhen(entry.CorrectedLemmaArabic is not null
                        && !string.Equals(entry.ExpectedCurrentLemmaArabic, entry.CorrectedLemmaArabic,
                            StringComparison.Ordinal),
                    $"{where}: 'keep' correctedLemmaArabic must equal expectedCurrentLemmaArabic "
                    + $"(got expected='{entry.ExpectedCurrentLemmaArabic}', "
                    + $"corrected='{entry.CorrectedLemmaArabic}').");
                break;

            case WordLemmaNormalizationOperationKind.Exception:
                // Non-mutating; observed value recorded. A corrected lemma, if present, must equal the
                // observed value (no mutation).
                FailWhen(entry.CorrectedLemmaArabic is not null
                        && entry.ExpectedCurrentLemmaArabic is not null
                        && !string.Equals(entry.ExpectedCurrentLemmaArabic, entry.CorrectedLemmaArabic,
                            StringComparison.Ordinal),
                    $"{where}: 'exception' correctedLemmaArabic must equal expectedCurrentLemmaArabic "
                    + $"(no mutation allowed).");
                FailWhen(string.IsNullOrWhiteSpace(entry.Reason),
                    $"{where}: 'exception' must include a reason.");
                break;

            default:
                Fail(true, $"{where}: invalid operationKind '{entry.OperationKind}'.");
                break;
        }

        // Every entry must carry a human reason + arabicMappingEvidence where applicable.
        FailWhen(string.IsNullOrWhiteSpace(entry.Reason),
            $"{where}: missing reason/evidence.");
        FailWhen(entry.OperationKind is WordLemmaNormalizationOperationKind.Add
                    or WordLemmaNormalizationOperationKind.Replace
                && string.IsNullOrWhiteSpace(entry.ArabicMappingEvidence),
            $"{where}: add/replace must include arabicMappingEvidence.");
    }

    // Accepts an add/replace corrected Arabic lemma when it is backed by an explicit mapping-evidence
    // row matching (buckwalter, arabicLemma). Two evidence tiers are both acceptable:
    //   1. auto-reliable mapping (AllowAutoAddReplace = true); OR
    //   2. explicit curated below-threshold mapping (AllowAutoAddReplace = false) — these are the
    //      legitimate curated corrections such as `Hil~ -> حِلّ` and `vuqifu -> ثُقِفُ`.
    //
    // What must FAIL is a corrected lemma with NO matching evidence row at all (a below-threshold
    // mapping that was never curated). The validator never weakens globally: every add/replace must
    // resolve to a listed evidence row keyed by its buckwalter.
    private static void RequireMappingEvidence(
        WordLemmaNormalizationEntry entry,
        IReadOnlyList<WordLemmaMappingEvidenceEntry> evidence,
        string where)
    {
        var corrected = entry.CorrectedLemmaArabic;
        if (string.IsNullOrWhiteSpace(corrected))
        {
            return; // Shape rules already flagged the missing corrected lemma.
        }

        var buckwalter = entry.CorpusLemmaBuckwalter;
        if (!string.IsNullOrWhiteSpace(buckwalter))
        {
            var matched = evidence.FirstOrDefault(e =>
                string.Equals(e.Buckwalter, buckwalter, StringComparison.Ordinal)
                && string.Equals(e.ArabicLemma, corrected, StringComparison.Ordinal));

            if (matched is not null)
            {
                return; // Explicit curated evidence row — auto-reliable or below-threshold curated.
            }
        }

        throw new InvalidDataException(
            $"{where}: corrected lemma '{corrected}' (buckwalter '{buckwalter}') has no curated "
            + "mapping evidence — needs an auto-reliable OR curated below-threshold evidence row.");
    }

    private static void FailWhen(bool condition, string message) => Fail(condition, message);

    private static void Fail(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidDataException(message);
        }
    }

    // Location format is S:V:W with positive integers (surah:ayah:word).
    private static bool IsInvalidLocation(string location)
    {
        var parts = location.Split(':');
        if (parts.Length != 3)
        {
            return true;
        }

        foreach (var part in parts)
        {
            if (part.Length == 0 || !int.TryParse(part, out var n) || n <= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string Describe(WordLemmaNormalizationEntry entry)
    {
        var id = string.IsNullOrWhiteSpace(entry.Id) ? "?" : entry.Id;
        var loc = string.IsNullOrWhiteSpace(entry.Location) ? "?" : entry.Location;
        return $"entry '{id}' at '{loc}'";
    }
}
