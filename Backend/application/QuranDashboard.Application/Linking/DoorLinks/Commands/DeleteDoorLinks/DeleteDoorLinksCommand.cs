using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Application.Linking.DoorLinks.Commands.DeleteDoorLinks;

public sealed record DeleteDoorLinksCommand(
    int DoorId,
    uint ExpectedDoorVersion,
    DoorLinkSelection Selection,
    int ActorUserId);
