namespace QuranDashboard.Application.Abstractions.Linking;

public sealed class LinkingWorkspaceViolationException(LinkingWorkspaceViolation violation) : Exception(
    violation.Value is null
        ? $"Linking workspace violation {violation.Code} on '{violation.Field}'."
        : $"Linking workspace violation {violation.Code} on '{violation.Field}': '{violation.Value}'.")
{
    public LinkingWorkspaceViolation Violation { get; } = violation;
}
