using QuranDashboard.Application.Abstractions.Abwab.Core;

namespace QuranDashboard.Application.Abwab.Tree;

public sealed class GetAbwabTreeSnapshotHandler(IAbwabCoreReadPort readPort)
{
    public async Task<AbwabTreeSnapshotDto> HandleAsync(GetAbwabTreeSnapshotQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await readPort.GetTreeSnapshotAsync(ct);
    }
}
