using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task SynchronizeRelationalDoorStateAsync(
        int doorId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var affectedAyahIds = await db.Database.SqlQuery<int>(
                $"""
                SELECT ayah_id AS "Value"
                FROM linking_confirmation_affected_ayahs
                ORDER BY ayah_id
                """)
            .ToListAsync(cancellationToken);
        await new RelationalDoorStateRebuilder(db).RebuildAsync(
            doorId,
            affectedAyahIds,
            actorUserId,
            true,
            cancellationToken);

        var updated = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE abwab_doors
            SET updated_at = CURRENT_TIMESTAMP,
                updated_by = {actorUserId}
            WHERE id = {doorId}
            """,
            cancellationToken);
        if (updated != 1)
        {
            throw new LinkingStaleVersionException();
        }
    }
}
