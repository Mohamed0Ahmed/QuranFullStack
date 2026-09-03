namespace QuranDashboard.Infrastructure.Testing.DatabaseActivity;

public enum RehearsalTargetKind
{
    ScratchEmpty,
    RehearsalFull,
}

public sealed record ValidatedRehearsalTarget(
    RehearsalTargetKind Kind,
    string Database,
    string Subtype);
