using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;
using System.Runtime.ExceptionServices;

namespace QuranDashboard.Application.Linking.PreparedPreflights;

public sealed class ProcessLinkingPreparedPreflightHandler(
    ILinkingPreparedPreflightStore store,
    ILinkingConfirmedStateReader confirmedStateReader,
    ILinkingDataRevisionReadScope revisionScope,
    LinkingPreparedPreflightLeaseService leaseService,
    LinkingPreparedPreflightInputBuilder inputBuilder,
    ILinkingScalabilityPolicy policy,
    ILogger<ProcessLinkingPreparedPreflightHandler> logger) : ILinkingPreparedPreflightProcessor
{
    public async Task<bool> ProcessOneAsync(CancellationToken cancellationToken)
    {
        var lease = await store.ClaimAsync(cancellationToken);
        if (lease is null)
        {
            return false;
        }

        var processingFence = await store.TryAcquireProcessingFenceAsync(
            lease,
            cancellationToken);
        if (processingFence is null)
        {
            return true;
        }

        try
        {
            var prepared = await ProcessClaimedAsync(
                lease,
                processingFence,
                cancellationToken);
            _ = await store.FinalizeReadyAsync(
                lease,
                prepared.State,
                prepared.Summary,
                prepared.IntentHash,
                prepared.PreflightToken,
                cancellationToken);
        }
        catch (LinkingPreparedPreflightLeaseLostException)
        {
            return true;
        }
        catch (LinkingDataStaleException)
        {
            await store.CompleteFailureAsync(
                lease,
                LinkingPreparedPreflightFailureCode.LinkingDataStale,
                false,
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await store.CompleteFailureAsync(
                lease,
                LinkingPreparedPreflightFailureCode.PreflightCancelled,
                false,
                cancellationToken);
        }
        catch (Exception exception) when (IsPermanentFailure(exception))
        {
            logger.LogError(
                exception,
                "Prepared linking preflight {PreflightId} attempt {AttemptCount} failed permanently.",
                lease.PreflightId,
                lease.AttemptCount);
            await store.CompleteFailureAsync(
                lease,
                LinkingPreparedPreflightFailureCode.PreparationFailed,
                false,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Prepared linking preflight {PreflightId} attempt {AttemptCount} failed.",
                lease.PreflightId,
                lease.AttemptCount);
            await store.CompleteFailureAsync(
                lease,
                LinkingPreparedPreflightFailureCode.PreparationFailed,
                true,
                cancellationToken);
        }

        return true;
    }

    private async Task<PreparedResult> ProcessClaimedAsync(
        LinkingPreparedPreflightLease lease,
        IAsyncDisposable processingFence,
        CancellationToken cancellationToken)
    {
        using var workCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var heartbeat = leaseService.RunHeartbeatAsync(
            lease,
            workCancellation.Cancel,
            workCancellation.Token);
        PreparedResult? prepared = null;
        Exception? processingFailure = null;

        try
        {
            await using (processingFence)
            {
                var work = await store.LoadWorkAsync(lease, workCancellation.Token)
                    ?? throw new LinkingPreparedPreflightLeaseLostException();
                prepared = await revisionScope.ExecuteAsync<PreparedResult>(
                    policy.MaximumAutomaticAttempts,
                    async (revision, token) => await PrepareAndPersistAsync(
                        lease,
                        work,
                        revision,
                        token),
                    workCancellation.Token);
            }
        }
        catch (Exception exception)
        {
            processingFailure = exception;
        }

        workCancellation.Cancel();
        Exception? heartbeatFailure = null;
        try
        {
            await heartbeat;
        }
        catch (OperationCanceledException) when (workCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            heartbeatFailure = exception;
        }

        var failure = heartbeatFailure ?? processingFailure;
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        return prepared ?? throw new InvalidOperationException("Prepared linking work produced no result.");
    }

    private async Task<PreparedResult> PrepareAndPersistAsync(
        LinkingPreparedPreflightLease lease,
        LinkingPreparedPreflightWork work,
        long revision,
        CancellationToken cancellationToken)
    {
        if (revision != lease.LinkingDataRevision)
        {
            throw new LinkingDataStaleException(lease.LinkingDataRevision, revision);
        }

        var input = await inputBuilder.BuildAsync(lease, work, cancellationToken);
        var contributionIdentities = input.Intent.Sources
            .Select(source => source.SourceIdentity)
            .ToList();
        var requestedAyahIds = input.Intent.Sources
            .SelectMany(source => source.Units)
            .SelectMany(unit => unit.Ayahs)
            .Select(ayah => ayah.AyahId)
            .Distinct()
            .ToList();
        var state = await confirmedStateReader.LoadAffectedAsync(
            work.DoorId,
            contributionIdentities,
            requestedAyahIds,
            cancellationToken)
            ?? throw new LinkingSourceNotFoundException($"doorId={work.DoorId}");
        input = LinkingPreparedAdditiveContentMerger.Merge(input, state);
        var request = input.Request;
        var intents = input.Intent with { IsDoorArchived = state.IsArchived };
        var classification = LinkingOperationClassifier.Classify(intents, state);
        var intentHash = LinkingPreparedPreflightToken.IntentHash(request);
        var preflightToken = LinkingPreparedPreflightToken.Compute(
            work.PreflightId,
            work.ActorUserId,
            work.RequestHash,
            intentHash,
            work.LinkingDataRevision,
            new LinkingPreflightDoorComponent(state.DoorId, state.DoorVersion),
            LinkingPreflightToken.AffectedContributionsOf(state, classification));
        var summary = await store.PersistPreparedResultAsync(
            lease,
            request,
            intents,
            state,
            classification,
            async (stage, processedSources, processedAyahs, totalAyahs, token) =>
                await leaseService.PublishProgressAsync(
                    lease,
                    stage,
                    processedSources,
                    processedAyahs,
                    totalAyahs,
                    token),
            cancellationToken);
        if (!await leaseService.ProbeAsync(lease, cancellationToken))
        {
            throw new LinkingPreparedPreflightLeaseLostException();
        }

        return new PreparedResult(state, summary, intentHash, preflightToken);
    }

    private sealed record PreparedResult(
        LinkingConfirmedDoorState State,
        LinkingPreparedResultSummary Summary,
        string IntentHash,
        string PreflightToken);

    private static bool IsPermanentFailure(Exception exception) => exception is
        LinkingSourceNotFoundException
        or LinkingInvalidDescriptorException
        or LinkingDuplicateContributionException
        or InvalidDataException;
}
