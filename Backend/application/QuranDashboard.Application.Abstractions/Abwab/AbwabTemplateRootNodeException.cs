namespace QuranDashboard.Application.Abstractions.Abwab;

// The root is not an ordinary node: it has no siblings to reorder among, and deleting it would leave
// a template that cannot be applied. Deleting the template is the way. One type, two refusals — each
// handler maps it to its own message.
public sealed class AbwabTemplateRootNodeException : Exception;
