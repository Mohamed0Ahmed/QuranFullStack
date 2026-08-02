namespace QuranDashboard.Application.Abstractions.Abwab;

// The template's root has no live children, so there is nothing to copy. Refused as a 400, not a
// silent no-op, because the copy modal's confirm button would otherwise promise N copies and
// produce zero. Deliberately not AbwabTemplateRootNodeException: plan.md §9 forbids splitting that
// type, and overloading it with a third, unrelated refusal in a third handler is the thing §9 is
// protecting against.
public sealed class AbwabTemplateEmptyException : Exception;
