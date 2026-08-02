namespace QuranDashboard.Application.Abstractions.Abwab;

// The root is not an ordinary node: it has no siblings to reorder among, and deleting it would leave
// a rootless template with no children to enumerate — apply would refuse it empty regardless.
// Deleting the template is the way. One type, two refusals — each handler maps it to its own
// message. Do not split it: the empty-template apply refusal is a third, unrelated case and gets
// its own type (AbwabTemplateEmptyException) instead of a third refusal on this one.
public sealed class AbwabTemplateRootNodeException : Exception;
