using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Application.Linking.Commands.ConfirmLinkingOperation;

public sealed class ConfirmLinkingOperationHandler(
    ILogger<ConfirmLinkingOperationHandler> logger,
    LinkingOperationPreparation preparation,
    ILinkingConfirmationWriter writer,
    ILinkingDataRevisionReadScope revisionScope,
    ILinkingScalabilityPolicy policy)
{
    private const string FeatureName = "Linking";
    private const string OperationName = "ConfirmLinkingOperation";

    public async Task<ConfirmLinkingOperationOutcome> HandleAsync(
        ConfirmLinkingOperationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Actor.Status != UserStatus.Active || !command.Actor.IsOwner)
        {
            throw new InvalidOperationException("Linking confirmation requires an active Owner authorization state.");
        }

        var violation = LinkingOperationValidation.ValidateConfirmation(command.Request);
        if (violation is not null)
        {
            LogInvalidRequest(command.Request.DoorId, violation);
            return new ConfirmLinkingOperationOutcome.InvalidRequest(violation);
        }

        try
        {
            var idempotencyKey = command.Request.IdempotencyKey
                ?? throw new InvalidOperationException("A validated confirmation requires an idempotency key.");
            var requestContract = new LinkingConfirmationRequestContract(
                LinkingConfirmationRequestContracts.LegacyExpanded,
                LinkingConfirmationRequestContracts.SchemaVersion,
                LinkingConfirmationRequestHasher.ComputeLegacy(command.Request),
                command.Request.ExpectedLinkingDataRevision);
            var replay = await writer.FindLegacyReplayAsync(
                command.Actor.UserId,
                command.Request.DoorId,
                idempotencyKey,
                requestContract,
                cancellationToken);
            if (replay is not null)
            {
                return Complete(command.Request.DoorId, replay, true);
            }

            var intent = await revisionScope.ExecuteAsync(
                policy.MaximumAutomaticAttempts,
                (revision, token) =>
                {
                    if (revision != command.Request.ExpectedLinkingDataRevision)
                    {
                        throw new LinkingDataStaleException(
                            command.Request.ExpectedLinkingDataRevision,
                            revision);
                    }

                    return preparation.PrepareConfirmationAsync(command.Request, token);
                },
                cancellationToken);
            var invalidReason = InvalidPreparedReasonOf(intent);

            if (invalidReason is not null)
            {
                var invalid = new LinkingOperationViolation(
                    LinkingOperationViolationCode.PreparedIntentInvalid,
                    "sources.units.ayahs",
                    LinkingPreflightTokens.ToToken(invalidReason));
                LogInvalidRequest(command.Request.DoorId, invalid);
                return new ConfirmLinkingOperationOutcome.InvalidRequest(invalid);
            }

            var writeResult = await writer.ConfirmAsync(
                command.Actor.UserId,
                command.Request,
                intent,
                requestContract,
                LinkingOperationClassifier.Classify,
                cancellationToken);

            return writeResult switch
            {
                LinkingConfirmationWriteResult.Success success => Complete(
                    command.Request.DoorId, success.Result, success.IsReplay),
                LinkingConfirmationWriteResult.DoorNotFound notFound => DoorNotFound(notFound.DoorId),
                LinkingConfirmationWriteResult.InvalidClassification => InvalidClassification(
                    command.Request.DoorId),
                _ => throw new InvalidOperationException(
                    $"Unhandled {nameof(LinkingConfirmationWriteResult)} variant."),
            };
        }
        catch (LinkingInvalidDescriptorException exception)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {doorId} {violationCode} {violationField}",
                FeatureName,
                OperationName,
                "invalidDescriptor",
                command.Request.DoorId,
                exception.Violation.Code,
                exception.Violation.Field);

            return new ConfirmLinkingOperationOutcome.InvalidDescriptor(exception.Violation);
        }
        catch (LinkingSourceNotFoundException exception)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {doorId} {reference}",
                FeatureName,
                OperationName,
                "sourceNotFound",
                command.Request.DoorId,
                exception.Reference);

            return new ConfirmLinkingOperationOutcome.SourceNotFound(exception.Reference);
        }
        catch (LinkingStaleVersionException)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {doorId}",
                FeatureName,
                OperationName,
                "staleVersion",
                command.Request.DoorId);

            return new ConfirmLinkingOperationOutcome.StaleVersion();
        }
        catch (LinkingPreflightStaleException exception)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {doorId}",
                FeatureName,
                OperationName,
                "stalePreflight",
                command.Request.DoorId);

            return new ConfirmLinkingOperationOutcome.StalePreflight(
                LinkingPreflightProjection.ToResult(
                    exception.State,
                    exception.Classification,
                    exception.FreshToken,
                    command.Request.ExpectedLinkingDataRevision));
        }
        catch (LinkingDuplicateContributionException)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {doorId}",
                FeatureName,
                OperationName,
                "duplicateContribution",
                command.Request.DoorId);

            return new ConfirmLinkingOperationOutcome.DuplicateContribution();
        }
        catch (LinkingIdempotencyConflictException)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {doorId}",
                FeatureName,
                OperationName,
                "idempotencyConflict",
                command.Request.DoorId);

            return new ConfirmLinkingOperationOutcome.IdempotencyConflict();
        }
        catch (LinkingDataStaleException)
        {
            return new ConfirmLinkingOperationOutcome.LinkingDataStale();
        }
    }

    private ConfirmLinkingOperationOutcome Complete(
        int doorId,
        LinkingConfirmationResultDto result,
        bool isReplay)
    {
        logger.LogInformation(
            "Completed {feature} {operation} {doorId} {sourceCount} {isNoOp} {isReplay}",
            FeatureName,
            OperationName,
            doorId,
            result.Sources.Count,
            result.IsNoOp,
            isReplay);

        return new ConfirmLinkingOperationOutcome.Success(result, isReplay);
    }

    private ConfirmLinkingOperationOutcome DoorNotFound(int doorId)
    {
        logger.LogWarning("Not found {feature} {operation} {doorId}", FeatureName, OperationName, doorId);
        return new ConfirmLinkingOperationOutcome.DoorNotFound(doorId);
    }

    private ConfirmLinkingOperationOutcome InvalidClassification(int doorId)
    {
        logger.LogWarning(
            "Rejected {feature} {operation} {reason} {doorId}",
            FeatureName,
            OperationName,
            "invalidClassification",
            doorId);

        return new ConfirmLinkingOperationOutcome.InvalidClassification();
    }

    private void LogInvalidRequest(int doorId, LinkingOperationViolation violation) =>
        logger.LogWarning(
            "Rejected {feature} {operation} {reason} {doorId} {violationCode} {violationField}",
            FeatureName,
            OperationName,
            "invalidRequest",
            doorId,
            violation.Code,
            violation.Field);

    private static LinkingPreflightInvalidReason? InvalidPreparedReasonOf(LinkingOperationIntent intent)
    {
        foreach (var source in intent.Sources)
        {
            if (source.InvalidReason is not null)
            {
                return source.InvalidReason;
            }

            var ayahReason = source.Units
                .SelectMany(unit => unit.Ayahs)
                .Select(ayah => ayah.InvalidReason)
                .FirstOrDefault(reason => reason is not null);

            if (ayahReason is not null)
            {
                return ayahReason;
            }
        }

        return null;
    }
}
