using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Application.Linking.DoorLinks.Commands.ReplaceDoorLinkWords;

public sealed record ReplaceDoorLinkWordsCommand(
    int DoorId,
    long UnitId,
    uint ExpectedDoorVersion,
    IReadOnlyList<DoorLinkSelectedWord> SelectedWords,
    int ActorUserId);
