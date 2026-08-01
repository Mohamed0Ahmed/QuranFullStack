using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

internal sealed class EfAbwabSectionsWriter(QuranDashboardDbContext db) : IAbwabSectionsWriter
{
    public async Task<AbwabSectionDto> CreateAsync(string name, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var nextOrder = await db.AbwabSections.CountAsync(s => s.DeletedAtUtc == null, cancellationToken) + 1;

        var section = new AbwabSection
        {
            Name = name,
            OrderValue = nextOrder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.AbwabSections.Add(section);
        await SaveTranslatingWriteExceptionsAsync(name, cancellationToken);

        return ToDto(section);
    }

    public async Task<AbwabSectionDto?> RenameAsync(int id, string name, uint expectedVersion, CancellationToken cancellationToken)
    {
        var section = await db.AbwabSections
            .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAtUtc == null, cancellationToken);
        if (section is null)
        {
            return null;
        }

        // Overriding OriginalValue (not CurrentValue) is what makes SaveChanges compare the client's
        // last-seen version against the row's actual current xmin, rather than the value this same
        // query just re-read.
        db.Entry(section).Property(s => s.Version).OriginalValue = expectedVersion;

        section.Name = name;
        section.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await SaveTranslatingWriteExceptionsAsync(name, cancellationToken);

        return ToDto(section);
    }

    public async Task<AbwabSectionDeleteResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var section = await db.AbwabSections
            .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAtUtc == null, cancellationToken);
        if (section is null)
        {
            return AbwabSectionDeleteResult.NotFound;
        }

        var hasLiveDoors = await db.AbwabDoors
            .AnyAsync(d => d.SectionId == id && d.DeletedAtUtc == null, cancellationToken);
        if (hasLiveDoors)
        {
            return AbwabSectionDeleteResult.HasLiveDoors;
        }

        var now = DateTimeOffset.UtcNow;
        section.DeletedAtUtc = now;
        section.UpdatedAtUtc = now;

        // Not a bare SaveChangesAsync: Version is rowversion-mapped, so this UPDATE carries
        // `AND xmin = @original` and a rename landing between the query above and this save affects zero
        // rows. Without the translation that surfaces as a raw DbUpdateConcurrencyException crossing the
        // Infrastructure seam into a 500, while the other ten writes answer 409.
        await SaveTranslatingConcurrencyAsync(cancellationToken);

        return AbwabSectionDeleteResult.Deleted;
    }

    public async Task<AbwabSectionDto?> ReorderAsync(int id, int position, uint expectedVersion, CancellationToken cancellationToken)
    {
        var section = await db.AbwabSections
            .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAtUtc == null, cancellationToken);
        if (section is null)
        {
            return null;
        }

        // The reader's own order (EfAbwabTreeReader), not a bare OrderBy(OrderValue): duplicate
        // OrderValues are reachable today (create assigns count(live) + 1, delete resequences
        // nothing), so tie-breaking on Id is what makes the clicked position and the computed
        // index agree with what the tree renders.
        var liveSections = await db.AbwabSections
            .Where(s => s.DeletedAtUtc == null)
            .OrderBy(s => s.OrderValue).ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        if (position < 1 || position > liveSections.Count)
        {
            throw new AbwabInvalidPositionException();
        }

        db.Entry(section).Property(s => s.Version).OriginalValue = expectedVersion;

        liveSections.RemoveAll(s => s.Id == id);
        liveSections.Insert(position - 1, section);
        Resequence(liveSections);

        section.UpdatedAtUtc = DateTimeOffset.UtcNow;

        // A reorder only moves rows out of the unique-name scope, so 23505 is structurally
        // impossible here (Writes/Abwab/README.md), same reasoning as DeleteAsync above.
        await SaveTranslatingConcurrencyAsync(cancellationToken);

        return ToDto(section);
    }

    private static void Resequence(IEnumerable<AbwabSection> orderedSections)
    {
        var position = 1;
        foreach (var section in orderedSections)
        {
            section.OrderValue = position++;
        }
    }

    // Shared by create/rename: DbUpdateConcurrencyException never carries a Postgres inner
    // exception (EF raises it itself on a zero-row affected count), so the `when` filter below never
    // intercepts it — it propagates to the concurrency catch instead.
    private async Task SaveTranslatingWriteExceptionsAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AbwabStaleVersionException();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new AbwabDuplicateNameException(name);
        }
    }

    // Delete's counterpart, mirroring EfAbwabDoorsWriter: a soft delete only ever moves a row OUT of the
    // unique index's live scope, so 23505 is structurally impossible and only the token can fail.
    private async Task SaveTranslatingConcurrencyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AbwabStaleVersionException();
        }
    }

    private static AbwabSectionDto ToDto(AbwabSection section) =>
        new(section.Id, section.Name, section.OrderValue, section.Version);
}
