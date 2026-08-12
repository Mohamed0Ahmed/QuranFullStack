namespace QuranDashboard.Domain.Linking;

public sealed class LinkingWorkspaceSourceWord
{
    public long WorkspaceSourceId { get; set; }

    public int QuranWordId { get; set; }

    public int AyahId { get; set; }
}
