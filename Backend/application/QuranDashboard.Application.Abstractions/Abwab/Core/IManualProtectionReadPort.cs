namespace QuranDashboard.Application.Abstractions.Abwab.Core;

// Addresses a category by its immutable CategoryId only — never filters by IsDeleted — so effective
// protection reads and authorized lifts still resolve on a soft-deleted target (narrow security surface).
public interface IManualProtectionReadPort
{
    Task<CategoryProtectionContextDto?> GetProtectionContextAsync(Guid categoryId, CancellationToken cancellationToken);
}
