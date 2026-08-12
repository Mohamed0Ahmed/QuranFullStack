namespace QuranDashboard.Application.Abstractions.Linking;

public enum LinkingWorkspaceViolationCode
{
    PreparedSourceLimitExceeded = 1,
    WordsNotAllowedOnAutomaticSource = 2,
    SelectedWordInvalid = 3,
    SelectedWordAyahOutsideManualSet = 4,
    AyahReferenceUnknown = 5,
    ConfigurationIncoherent = 6,
    ReorderSetMismatch = 7,
    LabelInvalid = 8,
    VersionRequired = 9,
    ReferenceUnknown = 10,
    SelectedWordAyahConflict = 11,
    DescriptionLimitExceeded = 12,
    DescriptionBodyInvalid = 13,
    DescriptionOrderConflict = 14,
    DescriptionAyahOutsideSource = 15,
}
