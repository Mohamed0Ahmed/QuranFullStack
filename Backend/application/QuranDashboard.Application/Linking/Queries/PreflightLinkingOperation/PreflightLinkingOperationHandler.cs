using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;

namespace QuranDashboard.Application.Linking.Queries.PreflightLinkingOperation;

public sealed class PreflightLinkingOperationHandler(
    ILogger<PreflightLinkingOperationHandler> logger,
    ILinkingConfirmedStateReader confirmedState,
    LinkingOperationPreparation preparation,
    ILinkingDataRevisionReadScope revisionScope,
    ILinkingScalabilityPolicy policy)
{
    private const string FeatureName = "Linking";
    private const string OperationName = "PreflightLinkingOperation";

    public async Task<PreflightLinkingOperationOutcome> HandleAsync(
        PreflightLinkingOperationQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var request = query.Request;
        var violation = LinkingOperationValidation.Validate(request);

        if (violation is not null)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {doorId} {violationCode} {violationField}",
                FeatureName,
                OperationName,
                "invalidRequest",
                request.DoorId,
                violation.Code,
                violation.Field);

            return new PreflightLinkingOperationOutcome.InvalidRequest(violation);
        }

        try
        {
            return await revisionScope.ExecuteAsync<PreflightLinkingOperationOutcome>(
                policy.MaximumAutomaticAttempts,
                async (revision, token) =>
                {
                    if (revision != request.ExpectedLinkingDataRevision)
                    {
                        throw new LinkingDataStaleException(
                            request.ExpectedLinkingDataRevision,
                            revision);
                    }

                    var state = await confirmedState.LoadAsync(request.DoorId, token);
                    if (state is null)
                    {
                        logger.LogWarning(
                            "Not found {feature} {operation} {doorId}",
                            FeatureName,
                            OperationName,
                            request.DoorId);
                        return new PreflightLinkingOperationOutcome.DoorNotFound(request.DoorId);
                    }

                    var intent = await preparation.PrepareAsync(request, state, token);
                    var classification = LinkingOperationClassifier.Classify(intent, state);
                    var preflightToken = LinkingPreflightToken.Compute(
                        request,
                        new LinkingPreflightDoorComponent(state.DoorId, state.DoorVersion),
                        LinkingPreflightToken.AffectedContributionsOf(state, classification));

                    logger.LogInformation(
                        "Completed {feature} {operation} {doorId} {sourceCount} {isNoOp} {isBlocked}",
                        FeatureName,
                        OperationName,
                        request.DoorId,
                        request.Sources.Count,
                        classification.IsNoOp,
                        classification.IsBlocked);

                    return new PreflightLinkingOperationOutcome.Success(
                        LinkingPreflightProjection.ToResult(
                            state,
                            classification,
                            preflightToken,
                            revision));
                },
                cancellationToken);
        }
        catch (LinkingInvalidDescriptorException exception)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {doorId} {violationCode} {violationField}",
                FeatureName,
                OperationName,
                "invalidDescriptor",
                request.DoorId,
                exception.Violation.Code,
                exception.Violation.Field);

            return new PreflightLinkingOperationOutcome.InvalidDescriptor(exception.Violation);
        }
        catch (LinkingSourceNotFoundException exception)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {doorId} {reference}",
                FeatureName,
                OperationName,
                "sourceNotFound",
                request.DoorId,
                exception.Reference);

            return new PreflightLinkingOperationOutcome.SourceNotFound(exception.Reference);
        }
        catch (LinkingDataStaleException)
        {
            return new PreflightLinkingOperationOutcome.LinkingDataStale();
        }
    }
}
