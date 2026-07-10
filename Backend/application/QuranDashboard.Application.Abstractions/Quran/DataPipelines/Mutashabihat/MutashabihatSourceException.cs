namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;

public sealed class MutashabihatSourceException : Exception
{
    public MutashabihatSourceException(string message)
        : base(message)
    {
    }

    public MutashabihatSourceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
