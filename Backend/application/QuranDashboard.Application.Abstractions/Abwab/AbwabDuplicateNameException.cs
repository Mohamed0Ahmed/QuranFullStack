namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabDuplicateNameException()
    : Exception("An Abwab entry name collides with an existing sibling in this scope.");
