using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace QuranDashboard.Tests.Quran.Words;

public sealed class RowMaterializationInterceptor : DbCommandInterceptor
{
    private readonly List<int> _readCounts = [];

    public IReadOnlyList<int> ReadCounts => _readCounts;

    public int MaxReadCount => _readCounts.Count == 0 ? 0 : _readCounts.Max();

    public int TotalReadCount => _readCounts.Sum();

    public void Reset() => _readCounts.Clear();

    public override InterceptionResult DataReaderClosing(
        DbCommand command,
        DataReaderClosingEventData eventData,
        InterceptionResult result)
    {
        _readCounts.Add(eventData.ReadCount);
        return base.DataReaderClosing(command, eventData, result);
    }

    public override ValueTask<InterceptionResult> DataReaderClosingAsync(
        DbCommand command,
        DataReaderClosingEventData eventData,
        InterceptionResult result)
    {
        _readCounts.Add(eventData.ReadCount);
        return base.DataReaderClosingAsync(command, eventData, result);
    }
}
