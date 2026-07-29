using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabDoorsWriter
{
    // Throws AbwabParentNotFoundException, AbwabSectionNotFoundException, AbwabDuplicateNameException,
    // AbwabSectionParentMismatchException. Under a parent the section is DERIVED from that parent; a null
    // sectionId means "unspecified", and a stated one that disagrees is refused rather than overwritten.
    Task<AbwabDoorDto> CreateAsync(
        int? sectionId,
        int? parentId,
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken);

    // Null = door missing or archived. Throws AbwabStaleVersionException, AbwabDuplicateNameException.
    // Aliases replace the live set wholesale; removed ones are soft-deleted, not hard-deleted.
    Task<AbwabDoorDto?> EditAsync(
        int id,
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        uint expectedVersion,
        CancellationToken cancellationToken);

    // Null = door missing or archived. Throws AbwabStaleVersionException, AbwabParentNotFoundException,
    // AbwabSectionNotFoundException, AbwabCycleException, AbwabDuplicateNameException. targetSectionId
    // is only honored when targetParentId is null — nesting under a parent always inherits that
    // parent's section, so the two can never disagree. A section change carries the door's whole subtree
    // with it, archived descendants included.
    Task<AbwabDoorDto?> MoveAsync(
        int id,
        int? targetSectionId,
        int? targetParentId,
        uint expectedVersion,
        CancellationToken cancellationToken);

    // Null = door missing or archived. Throws AbwabStaleVersionException, AbwabInvalidPositionException,
    // AbwabScopeNotApplicableException (Global on a nested door). Section renumbers OrderValue in the
    // door's own (section, parent) scope; Global renumbers GlobalOrderValue across every live root,
    // and never touches OrderValue — the two spaces are independent (plan §5).
    Task<AbwabDoorDto?> ReorderAsync(int id, int position, AbwabReorderScope scope, uint expectedVersion, CancellationToken cancellationToken);

    // Throws AbwabNotFoundException, AbwabParentNotFoundException, AbwabSectionNotFoundException,
    // AbwabCycleException, AbwabDuplicateNameException, AbwabStaleVersionException. All-or-nothing.
    // Like MoveAsync, a section change carries each moved door's whole subtree with it.
    Task<IReadOnlyList<AbwabDoorDto>> BulkMoveAsync(
        IReadOnlyList<AbwabBulkDoorRef> doors,
        int? targetSectionId,
        int? targetParentId,
        CancellationToken cancellationToken);

    // Throws AbwabNotFoundException, AbwabStaleVersionException. All-or-nothing. Archives every listed
    // door's whole live subtree too, and returns every archived door id (including swept-in descendants).
    Task<IReadOnlyList<int>> BulkArchiveAsync(IReadOnlyList<AbwabBulkDoorRef> doors, CancellationToken cancellationToken);

    // False = door missing or already archived. Throws AbwabStaleVersionException. Archives the door
    // and its whole live subtree in one transaction.
    Task<bool> DeleteAsync(int id, uint expectedVersion, CancellationToken cancellationToken);

    // Null = door missing. Throws AbwabStaleVersionException, AbwabParentStillArchivedException,
    // AbwabDuplicateNameException. Restores the door plus exactly the descendants its OWN archive swept
    // in (matched on the archive's timestamp) — never one archived by an earlier, separate operation.
    // Renumbers the scope it lands back in to 1..N, and detaches the door (and everything restored with
    // it) to "outside every section" when its section was archived meanwhile. DetachedFromArchivedSection
    // reports that detach, and covers the whole restored subtree, not just the named door.
    Task<AbwabRestoredDoorDto?> RestoreAsync(int id, uint expectedVersion, CancellationToken cancellationToken);
}
