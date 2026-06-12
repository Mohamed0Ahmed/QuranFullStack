namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public static class I3rabInvariants
{
    public const int ExpectedSegmentCount = 128_219;
    public const int ExpectedWordCount = 77_432;
    public const int ExpectedRuleCount = 142;
    public const int ExpectedFamilyCount = 67;
    public const int ExpectedNullFormCount = 208;

    public const string CheckSegStatusComplete = "I3RAB-SEG-STATUS-COMPLETE";
    public const string CheckApprovedConsistent = "I3RAB-APPROVED-CONSISTENT";
    public const string CheckNeedsReviewConsistent = "I3RAB-NEEDS-REVIEW-CONSISTENT";
    public const string CheckUnsupportedConsistent = "I3RAB-UNSUPPORTED-CONSISTENT";
    public const string CheckWordDisplayable = "I3RAB-WORD-DISPLAYABLE";
    public const string CheckRuleResolves = "I3RAB-RULE-RESOLVES";
    public const string CheckSourceColumnsUnchanged = "I3RAB-SOURCE-COLUMNS-UNCHANGED";
    public const string CheckSegmentRowCountStable = "I3RAB-SEGMENT-ROWCOUNT-STABLE";
    public const string CheckNullFormNotInvented = "I3RAB-NULL-FORM-NOT-INVENTED";

    public const string WarningCoverage = "I3RAB-COVERAGE";
    public const string WarningRuleUsage = "I3RAB-RULE-USAGE";
    public const string WarningUnknownPatterns = "I3RAB-UNKNOWN-PATTERNS";
    public const string WarningNeedsReviewSummary = "I3RAB-NEEDS-REVIEW-SUMMARY";
    public const string WarningLabelReview = "I3RAB-LABEL-REVIEW";
}
