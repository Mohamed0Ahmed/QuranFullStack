namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabWriteWithoutChangeSetException()
    : Exception("An Abwab auditable mutation was attempted without a tracked ChangeSet in the same unit of work.");
