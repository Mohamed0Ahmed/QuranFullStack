using QuranDashboard.Application.Abstractions.Linking.Preflight;

namespace QuranDashboard.Application.Abstractions.Linking;

public interface ILinkingConfirmedStateReader
{
    Task<LinkingConfirmedDoorState?> LoadAsync(int doorId, CancellationToken cancellationToken);
}
