namespace QuranDashboard.Application.Abwab.Commands.Sections.RenameSection;

// The PUT wire body; the route supplies the id, merged into RenameSectionCommand by the controller.
public sealed record RenameSectionBody(string Name, uint Version);
