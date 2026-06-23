using Microsoft.Extensions.Logging;

namespace QuranDashboard.Tests.TestSupport.Logging;

public sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly List<LogEntry> _entries = [];
    private readonly object _gate = new();

    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_gate)
            {
                return _entries.ToArray();
            }
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
        }
    }

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, this);

    public void Dispose()
    {
    }

    internal void Record(LogEntry entry)
    {
        lock (_gate)
        {
            _entries.Add(entry);
        }
    }

    public sealed record LogEntry(
        string CategoryName,
        LogLevel Level,
        EventId EventId,
        Exception? Exception,
        IReadOnlyList<KeyValuePair<string, object?>>? State,
        string Message);

    private sealed class RecordingLogger(string categoryName, RecordingLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            provider.Record(new LogEntry(
                categoryName,
                logLevel,
                eventId,
                exception,
                state as IReadOnlyList<KeyValuePair<string, object?>>,
                formatter(state, exception)));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}

public static class RecordingLoggerExtensions
{
    public static IReadOnlyList<KeyValuePair<string, object?>> StructuredFields(
        this RecordingLoggerProvider.LogEntry entry) => entry.State ?? [];

    public static IReadOnlyList<KeyValuePair<string, object?>> StructuredFieldsWithoutOriginalFormat(
        this RecordingLoggerProvider.LogEntry entry) =>
        entry.StructuredFields()
            .Where(pair => pair.Key != "{OriginalFormat}")
            .ToArray();

    public static IReadOnlyCollection<string> FieldNames(
        this RecordingLoggerProvider.LogEntry entry) =>
        entry.StructuredFieldsWithoutOriginalFormat()
            .Select(pair => pair.Key)
            .ToArray();

    public static T GetValue<T>(this RecordingLoggerProvider.LogEntry entry, string key)
    {
        var value = entry.StructuredFields().Single(pair => pair.Key == key).Value;
        return (T)value!;
    }
}
