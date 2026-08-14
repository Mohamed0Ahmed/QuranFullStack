namespace QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;

public interface ILinkingPreparedPreflightProcessor
{
    Task<bool> ProcessOneAsync(CancellationToken cancellationToken);
}
