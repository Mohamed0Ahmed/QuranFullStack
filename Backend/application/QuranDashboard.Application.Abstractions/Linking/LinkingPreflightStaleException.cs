using QuranDashboard.Application.Abstractions.Linking.Preflight;

namespace QuranDashboard.Application.Abstractions.Linking;

public sealed class LinkingPreflightStaleException : Exception
{
    public LinkingPreflightStaleException(
        LinkingConfirmedDoorState state,
        LinkingOperationClassification classification,
        string freshToken)
        : base("The supplied preflight token no longer matches the door's confirmed state.")
    {
        State = state;
        Classification = classification;
        FreshToken = freshToken;
    }

    public LinkingConfirmedDoorState State { get; }

    public LinkingOperationClassification Classification { get; }

    public string FreshToken { get; }
}
