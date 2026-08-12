namespace QuranDashboard.Application.Abstractions.Linking;

public enum LinkingDescriptorViolationCode
{
    MalformedDescriptor = 1,
    ResolvedAyahLimitExceeded = 2,
    ManualAyahCompletenessFailed = 3,
}
