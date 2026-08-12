namespace QuranDashboard.Application.Abstractions.Linking;

public sealed class LinkingInvalidDescriptorException(LinkingDescriptorViolation violation) : Exception(
    violation.Value is null
        ? $"Linking descriptor violation {violation.Code} on '{violation.Field}'."
        : $"Linking descriptor violation {violation.Code} on '{violation.Field}': '{violation.Value}'.")
{
    public LinkingDescriptorViolation Violation { get; } = violation;
}
