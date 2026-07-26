namespace QuranDashboard.Application.Abstractions.Abwab.Templates;

// The audited action strings live in Abstractions because the writer (Application) stamps them and
// both the history read port and the restore interpreter (Infrastructure) match on them. Infrastructure
// cannot reference Application, so a private copy on either side would drift silently: a changed
// prefix would empty every history read, and a changed applied-kind would break application inversion,
// with no compile-time signal.
public static class TemplateAuditActions
{
    public const string HistoryPrefix = "template.history.";

    public const string Created = HistoryPrefix + "created";
    public const string Edited = HistoryPrefix + "edited";
    public const string Deleted = HistoryPrefix + "deleted";
    public const string Restored = HistoryPrefix + "restored";
    public const string NodeAdded = HistoryPrefix + "node_added";
    public const string NodeEdited = HistoryPrefix + "node_edited";
    public const string NodeReparented = HistoryPrefix + "node_reparented";
    public const string NodesReordered = HistoryPrefix + "nodes_reordered";
    public const string NodeRemoved = HistoryPrefix + "node_removed";
    public const string AliasAdded = HistoryPrefix + "alias_added";
    public const string AliasEdited = HistoryPrefix + "alias_edited";
    public const string AliasRemoved = HistoryPrefix + "alias_removed";
    public const string AliasRestored = HistoryPrefix + "alias_restored";

    public const string Applied = "template.applied";
}
