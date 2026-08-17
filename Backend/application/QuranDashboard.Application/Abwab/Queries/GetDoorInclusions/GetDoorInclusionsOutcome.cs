using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Queries.GetDoorInclusions;

public abstract record GetDoorInclusionsOutcome
{
    private GetDoorInclusionsOutcome() { }

    public sealed record Success(AbwabDoorInclusionTopologyDto Topology) : GetDoorInclusionsOutcome;

    public sealed record InvalidRequest : GetDoorInclusionsOutcome;

    public sealed record NotFound : GetDoorInclusionsOutcome;
}
