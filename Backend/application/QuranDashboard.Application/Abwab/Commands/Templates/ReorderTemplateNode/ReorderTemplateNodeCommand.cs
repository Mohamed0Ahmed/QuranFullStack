namespace QuranDashboard.Application.Abwab.Commands.Templates.ReorderTemplateNode;

public sealed record ReorderTemplateNodeCommand(int NodeId, int Position);

// No scope, unlike the door reorder body: template nodes have one order space,
// (template_id, parent_node_id). There is no global order here — a copy is never a root door.
public sealed record ReorderTemplateNodeBody(int Position);
