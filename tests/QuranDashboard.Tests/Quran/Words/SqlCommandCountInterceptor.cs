using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace QuranDashboard.Tests.Quran.Words;

/// <summary>
/// Counts EF Core database commands for integration tests that assert bounded
/// query shapes (e.g. batched ayah reads vs per-ayah N+1).
/// </summary>
public sealed class SqlCommandCountInterceptor : DbCommandInterceptor
{
    private int _commandCount;

    public int CommandCount => _commandCount;

    public void Reset() => _commandCount = 0;

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        _commandCount++;
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        _commandCount++;
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        _commandCount++;
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _commandCount++;
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        _commandCount++;
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        _commandCount++;
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }
}
