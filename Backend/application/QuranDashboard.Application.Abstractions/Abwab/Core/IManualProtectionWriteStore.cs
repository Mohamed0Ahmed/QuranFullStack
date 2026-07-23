using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public interface IManualProtectionWriteStore
{
    Task<ManualProtection?> FindActiveAsync(Guid categoryId, ManualProtectionType protectionType, CancellationToken cancellationToken);

    Task<IReadOnlyList<ManualProtection>> FindActiveByCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    void Add(ManualProtection protection);
}
