namespace QuranDashboard.Application.Abstractions.Abwab.Responses;

// DetachedFromArchivedSection is the only way a caller learns the restore moved the door (and its
// restored subtree) out of its section: the door's own SectionId is null either way, and the caller
// cannot tell "was never in a section" from "its section was retired meanwhile" without prior state.
public sealed record AbwabRestoredDoorDto(AbwabDoorDto Door, bool DetachedFromArchivedSection);
