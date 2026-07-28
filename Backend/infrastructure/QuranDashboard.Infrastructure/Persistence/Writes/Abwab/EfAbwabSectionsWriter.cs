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
        await db.SaveChangesAsync(cancellationToken);

        return AbwabSectionDeleteResult.Deleted;
    }

    // Shared by every write above: DbUpdateConcurrencyException never carries a Postgres inner
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

    private static AbwabSectionDto ToDto(AbwabSection section) =>
        new(section.Id, section.Name, section.OrderValue, section.Version);
}
