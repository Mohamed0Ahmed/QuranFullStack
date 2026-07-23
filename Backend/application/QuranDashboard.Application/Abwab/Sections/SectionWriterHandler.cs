using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Sections;
using QuranDashboard.Domain.Abwab.Tree;

namespace QuranDashboard.Application.Abwab.Sections;

// Master Plan §8's concurrency column for "Sections and section order" is `xmin` + TreeRevision (not
// xmin alone) — every section write below carries ExpectedTreeRevision and bumps TreeRevision once,
// matching the Category writers. §9's section rows correctly carry "No category manual target".
public sealed class SectionWriterHandler(IAbwabWriteExecutor executor, ISectionWriteStore sections)
{
    public async Task<Guid> AddAsync(AddSectionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var sectionId = Guid.NewGuid();

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (revision, operationToken) =>
            {
                AbwabRevisionGuards.GuardTreeRevision(revision, command.ExpectedTreeRevision);

                var normalized = ArabicNameNormalizer.Normalize(command.Name);
                await GuardNameConflictAsync(normalized, excludeSectionId: null, operationToken);

                var maxOrder = await sections.GetMaxSortOrderAsync(operationToken);

                var section = new Section
                {
                    SectionId = sectionId,
                    Name = command.Name,
                    NormalizedName = normalized,
                    SortOrder = maxOrder + 1,
                    IsPermanentDefault = false,
                };
                sections.Add(section);
                revision.TreeRevision += 1;

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("section.added", new { section.SectionId, section.Name, section.SortOrder })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
        return sectionId;
    }

    public async Task EditAsync(EditSectionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (revision, operationToken) =>
            {
                AbwabRevisionGuards.GuardTreeRevision(revision, command.ExpectedTreeRevision);

                var section = await RequireTrackedSectionAsync(command.SectionId, operationToken);
                AbwabRevisionGuards.GuardRowVersion(section.Version, command.ExpectedVersion);

                if (section.IsPermanentDefault)
                {
                    throw PermanentDefaultViolation();
                }

                var normalized = ArabicNameNormalizer.Normalize(command.Name);
                await GuardNameConflictAsync(normalized, section.SectionId, operationToken);

                var oldName = section.Name;
                section.Name = command.Name;
                section.NormalizedName = normalized;
                revision.TreeRevision += 1;

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("section.edited", new { section.SectionId, oldName, newName = section.Name })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    public async Task ReorderAsync(ReorderSectionsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Orders.Count == 0)
        {
            throw new ArgumentException("At least one section order entry is required.", nameof(command));
        }

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (revision, operationToken) =>
            {
                AbwabRevisionGuards.GuardTreeRevision(revision, command.ExpectedTreeRevision);

                var ids = command.Orders.Select(o => o.SectionId).ToList();
                var loaded = await sections.FindManyTrackedAsync(ids, operationToken);

                if (loaded.Count != ids.Count)
                {
                    throw Conflict(AbwabConflictCodes.RowStale, "One or more sections are unavailable.");
                }

                var byId = loaded.ToDictionary(s => s.SectionId);
                foreach (var entry in command.Orders)
                {
                    AbwabRevisionGuards.GuardRowVersion(byId[entry.SectionId].Version, entry.ExpectedVersion);
                }

                foreach (var entry in command.Orders)
                {
                    byId[entry.SectionId].SortOrder = entry.SortOrder;
                }

                revision.TreeRevision += 1;

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("section.reordered", new { Orders = command.Orders })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(DeleteSectionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (revision, operationToken) =>
            {
                AbwabRevisionGuards.GuardTreeRevision(revision, command.ExpectedTreeRevision);

                var section = await RequireTrackedSectionAsync(command.SectionId, operationToken);
                AbwabRevisionGuards.GuardRowVersion(section.Version, command.ExpectedVersion);

                if (section.IsPermanentDefault)
                {
                    throw PermanentDefaultViolation();
                }

                if (await sections.HasActiveRootCategoriesAsync(section.SectionId, operationToken))
                {
                    throw Conflict(AbwabConflictCodes.SectionNotEmpty, "Section still has active root categories.");
                }

                section.IsDeleted = true;
                section.DeletedAtUtc = DateTimeOffset.UtcNow;
                revision.TreeRevision += 1;

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("section.deleted", new { section.SectionId, section.Name })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    private async Task<Section> RequireTrackedSectionAsync(Guid sectionId, CancellationToken cancellationToken) =>
        await sections.FindTrackedAsync(sectionId, cancellationToken)
            ?? throw Conflict(AbwabConflictCodes.RowStale, "Section is unavailable.");

    private async Task GuardNameConflictAsync(string normalizedName, Guid? excludeSectionId, CancellationToken cancellationToken)
    {
        if (await sections.ActiveNormalizedNameExistsAsync(normalizedName, excludeSectionId, cancellationToken))
        {
            throw Conflict(AbwabConflictCodes.SectionNameConflict, "A section with this name already exists.");
        }
    }

    private static AbwabWriteConflictException PermanentDefaultViolation() =>
        Conflict(AbwabConflictCodes.PermanentDefaultSection, "The permanent default section may only be reordered.");

    private static AbwabWriteConflictException Conflict(string code, string message) => new(code, message);
}
