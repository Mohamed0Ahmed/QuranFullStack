namespace QuranDashboard.Application.Abwab.Commands.Templates.ReorderTemplateNode;

public sealed record ReorderTemplateNodeCommand(int NodeId, int Position);

public sealed record ReorderTemplateNodeBody(int Position);
