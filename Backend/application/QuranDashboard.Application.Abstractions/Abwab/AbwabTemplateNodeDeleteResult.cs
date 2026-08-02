namespace QuranDashboard.Application.Abstractions.Abwab;

// The AbwabSectionDeleteResult convention: a delete whose refusals are ordinary answers, not faults.
public enum AbwabTemplateNodeDeleteResult
{
    Deleted = 1,
    NotFound = 2,
    IsRoot = 3,
}
