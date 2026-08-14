namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private const int PersistenceBatchSize = 500;

    private static IEnumerable<T[]> BatchesOf<T>(IEnumerable<T> values) =>
        values.Chunk(PersistenceBatchSize);

    private void DetachRange<T>(IEnumerable<T> entities)
        where T : class
    {
        foreach (var entity in entities)
        {
            db.Entry(entity).State = EntityState.Detached;
        }
    }
}
