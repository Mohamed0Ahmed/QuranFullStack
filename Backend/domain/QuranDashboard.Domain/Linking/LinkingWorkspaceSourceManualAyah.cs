namespace QuranDashboard.Domain.Linking;

public sealed class LinkingWorkspaceSourceManualAyah
{
    public long WorkspaceSourceId { get; set; }

    public int AyahId { get; set; }

    public int OrderValue { get; set; }

    public int? PageHint { get; set; }
}
