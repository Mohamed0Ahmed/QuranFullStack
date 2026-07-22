namespace QuranDashboard.Application.Abstractions.Abwab;

// Raised at SavingChanges when an IAbwabAuditable entity is added/modified/deleted with no tracked
// ChangeSet in the same unit of work. Every Abwab mutation must run inside exactly one ChangeSet.
public sealed class AbwabWriteWithoutChangeSetException()
    : Exception("An Abwab auditable mutation was attempted without a tracked ChangeSet in the same unit of work.");
