namespace QuranDashboard.Application.Abstractions.Linking;

public enum LinkingOperationViolationCode
{
    SourcesRequired = 1,
    AyahRequired = 2,
    DuplicateAyah = 3,
    GroupingInvalid = 4,
    ContributionModeInvalid = 5,
    WordsNotAllowedOnAutomaticSource = 6,
    AutomaticWordMatchesRequired = 7,
    AutomaticWordMatchesNotAllowed = 8,
    DescriptionLimitExceeded = 9,
    DescriptionBodyInvalid = 10,
    MalformedBody = 11,
    PreflightTokenRequired = 12,
    IdempotencyKeyRequired = 13,
    ExistingContributionPairInvalid = 14,
    PreparedIntentInvalid = 15,
    DuplicateSource = 16,
    ExistingContributionIdInvalid = 17,
    ExistingContributionVersionInvalid = 18,
}
