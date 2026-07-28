using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Queries.GetAbwabTree;

public abstract record GetAbwabTreeOutcome
{
    private GetAbwabTreeOutcome() { }

    public sealed record Success(AbwabTreeDto Tree) : GetAbwabTreeOutcome;
}
