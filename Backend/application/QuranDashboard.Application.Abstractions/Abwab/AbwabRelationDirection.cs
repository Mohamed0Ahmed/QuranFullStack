namespace QuranDashboard.Application.Abstractions.Abwab;

// Request-side only: it names the ANCHOR door's role, and the writer resolves it into the stored
// AbwabDoorRelation.BroaderDoorId. Anchor-relative on purpose — the design contract used
// "broader"/"narrower" from two different perspectives in two places, so neither token may
// reach the wire.
// Starts at 1: System.Text.Json leaves an omitted property at 0, which is a caller bug, not a default.
public enum AbwabRelationDirection
{
    AnchorMoreComprehensive = 1,
    AnchorLessComprehensive = 2,
}
