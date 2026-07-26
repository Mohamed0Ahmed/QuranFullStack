namespace QuranDashboard.Application.Abstractions.Abwab.Templates;

public static class TemplateNodeOrderRules
{
    // ONE definition of "malformed reorder list", enforced at two layers on purpose: the API rejects
    // it as the framework 400 before a command exists, and the handler re-checks it because the port
    // is reachable without going through the controller. Two enforcement points, never two rules —
    // a list that repeats an id would otherwise pass the count check while leaving a real sibling
    // silently un-reordered.
    public static bool IsWellFormedOrder(IReadOnlyList<Guid> orderedTemplateNodeIds)
    {
        ArgumentNullException.ThrowIfNull(orderedTemplateNodeIds);

        return orderedTemplateNodeIds.Count > 0 &&
            orderedTemplateNodeIds.Distinct().Count() == orderedTemplateNodeIds.Count;
    }
}
