using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Application.Linking.Queries.ResolveLinkingSource;

public sealed class ResolveLinkingSourceHandler(
    ILogger<ResolveLinkingSourceHandler> logger,
    ILinkingSourceResolutionReader reader,
    ILinkingDataRevisionReadScope revisionScope,
    ILinkingScalabilityPolicy policy)
{
    private const string FeatureName = "Linking";
    private const string OperationName = "ResolveLinkingSource";
    private const string DescriptorField = "descriptor";

    public async Task<ResolveLinkingSourceOutcome> HandleAsync(
        ResolveLinkingSourceQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!LinkingSourceDescriptorValidation.TryValidate(query.Descriptor, out var error))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {kind} {diagnostic}",
                FeatureName,
                OperationName,
                "invalidDescriptor",
                query.Descriptor.Kind,
                error);

            return new ResolveLinkingSourceOutcome.InvalidDescriptor(new LinkingDescriptorViolation(
                LinkingDescriptorViolationCode.MalformedDescriptor,
                DescriptorField,
                null));
        }

        try
        {
            var source = await revisionScope.ExecuteAsync(
                policy.MaximumAutomaticAttempts,
                (revision, token) => reader.ResolveAsync(query.Descriptor, revision, token),
                cancellationToken);

            return new ResolveLinkingSourceOutcome.Success(source);
        }
        catch (LinkingInvalidDescriptorException exception)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {kind} {violationCode} {violationField}",
                FeatureName,
                OperationName,
                "invalidDescriptor",
                query.Descriptor.Kind,
                exception.Violation.Code,
                exception.Violation.Field);

            return new ResolveLinkingSourceOutcome.InvalidDescriptor(exception.Violation);
        }
        catch (LinkingSourceNotFoundException exception)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {kind} {reference}",
                FeatureName,
                OperationName,
                "sourceNotFound",
                query.Descriptor.Kind,
                exception.Reference);

            return new ResolveLinkingSourceOutcome.NotFound(exception.Reference);
        }
    }
}
