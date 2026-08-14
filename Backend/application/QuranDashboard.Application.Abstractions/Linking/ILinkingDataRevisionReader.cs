namespace QuranDashboard.Application.Abstractions.Linking;

public interface ILinkingDataRevisionReader
{
    Task<long> ReadAsync(CancellationToken cancellationToken);
}
