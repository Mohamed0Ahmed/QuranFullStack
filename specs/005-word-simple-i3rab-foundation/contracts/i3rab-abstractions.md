# Contract — Application.Abstractions (i‘rab generation)

Interfaces and DTOs the Application layer depends on; implemented in Infrastructure. Namespaces under
`QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab`. These mirror the Feature 004
`MorphologySourceData` / `IMorphologyImportSource` / `MorphologyInvariants` shapes.

## DTOs

```text
I3rabSegmentInput            // one per morphology segment, read for generation
  int      SegmentId
  int      QuranWordId
  short    SegmentNumber
  string   Kind              // PREFIX | STEM | SUFFIX
  string   Pos               // segment POS code
  string   FeaturesRaw       // '|'-delimited tokens (case/tense/voice/person)
  string?  CaseFeature       // NOM | ACC | GEN | null   (noun-class)
  string?  VerbTense         // PERF | IMPF | IMPV | null
  string?  VerbVoice         // ACT | PASS | null
  bool     IsAllahLemma      // PN stem whose lemma_buckwalter = the divine name
  bool     FormIsNull        // form_arabic_normalized IS NULL (the 208)

I3rabRuleSeedRow             // one per catalogue row (142)
  string   SignatureKey
  string   RuleFamily
  string   I3rabArabic
  string   DefaultStatus     // 'approved' in v1
  string?  Description
  short    SortOrder

I3rabSegmentLabel            // assembler output, one per segment
  int      SegmentId
  string?  I3rabArabic
  string   SignatureKey      // resolved → rule id at write time
  string   Status            // 'approved' | 'needs_review' | 'unsupported'
  string?  ReviewReason

I3rabGenerationResult
  bool     Persisted
  bool     Forced
  int      SegmentCount          // 128,219
  int      ApprovedCount
  int      NeedsReviewCount
  int      UnsupportedCount
  int      RuleCount              // 142
  int      FamilyCount            // 67
  int      WordsDisplayable
  IReadOnlyList<I3rabCheckResult> Checks
  string?  ReportPath
```

## Interfaces

```text
II3rabGenerationSource
  // read-only reads from the populated morphology
  bool                          MorphologyIsReady(out int segmentCount)   // preflight (FR-025)
  bool                          I3rabAlreadyPopulated()                   // preflight (FR-027)
  IReadOnlyList<I3rabSegmentInput> LoadSegments()

II3rabRuleCatalog
  IReadOnlyList<I3rabRuleSeedRow> Rows()                  // the curated 142 (single label owner)
  bool TryGet(string signatureKey, out I3rabRuleSeedRow row)

II3rabAssembler                                           // (may live in Infrastructure/Files)
  IReadOnlyList<I3rabSegmentLabel> Assemble(IReadOnlyList<I3rabSegmentInput> segments)

II3rabGenerationWriter
  // one transaction: seed rules (upsert by signature_key) + COPY-staged UPDATE of the 4 columns;
  // returns committed counts, or rolls back on a failed gate
  I3rabGenerationResult Write(IReadOnlyList<I3rabRuleSeedRow> rules,
                              IReadOnlyList<I3rabSegmentLabel> labels,
                              bool force)

II3rabGenerationReportWriter
  string Write(I3rabGenerationResult result, string outputDirectory)      // Markdown + JSON, returns path
```

## Invariants surface

```text
I3rabInvariants     // static ids + expected values (mirrors MorphologyInvariants)
  const int    ExpectedSegmentCount = 128_219;
  const int    ExpectedWordCount    = 77_432;
  const int    ExpectedRuleCount    = 142;
  const int    ExpectedFamilyCount  = 67;
  const int    ExpectedNullFormCount = 208;
  // hard-check ids: I3RAB-SEG-STATUS-COMPLETE, I3RAB-APPROVED-CONSISTENT, ...
```

## Dependency direction (Clean Architecture)

- **Application** (`GenerateI3rab`) depends only on these abstractions + Domain.
- **Infrastructure** implements them (`I3rabRuleCatalogSeed`, `SegmentSignatureBuilder`, `I3rabAssembler`,
  `EfI3rabGenerationWriter`, `MarkdownJsonI3rabReportWriter`) and references Application.Abstractions +
  Domain only.
- The **console host** wires DI and parses the verb; it contains no business logic.
