namespace QuranDashboard.Application.Abstractions.Linking;

public interface ILinkingScalabilityPolicy
{
    int PageSizeMaximum { get; }

    int PersistenceBatchSize { get; }

    int PreflightProcessorConcurrency { get; }

    int ConfirmationProcessorConcurrency { get; }

    int MaximumAutomaticAttempts { get; }

    int PollAfterMilliseconds { get; }

    int ActiveWorkflowsPerActor { get; }

    TimeSpan ReadyPreflightLifetime { get; }

    TimeSpan AbandonedPreflightLifetime { get; }

    TimeSpan TerminalRetention { get; }

    TimeSpan WorkerLease { get; }

    TimeSpan WorkerHeartbeat { get; }
}
