using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    private async Task<ContextAyahPageLoad> ReadAyahPageAsync(
        Guid buildId,
        long variantId,
        PhraseContextSelection selection,
        long offset,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var command = CreateContextCommand(string.Concat(
            FilteredOccurrencesSql,
            AyahPageSql));
        AddContextParameters(command, buildId, variantId, selection);
        command.Parameters.AddWithValue("offset", NpgsqlDbType.Bigint, offset);
        command.Parameters.AddWithValue("page_size", pageSize);

        var totalOccurrenceCount = 0;
        var totalAyahCount = 0;
        var hasInvalidExactIdentity = false;
        var items = new List<ContextOccurrenceRow>();
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            totalOccurrenceCount = reader.GetInt32(0);
            totalAyahCount = reader.GetInt32(1);
            hasInvalidExactIdentity = reader.GetBoolean(2);
            if (reader.IsDBNull(3))
            {
                continue;
            }

            items.Add(ReadOccurrenceRow(reader, 3));
        }

        if (hasInvalidExactIdentity)
        {
            throw new InvalidDataException("PhraseSearch context source contains a word without the selected exact identity.");
        }

        return new ContextAyahPageLoad(totalOccurrenceCount, totalAyahCount, items);
    }

    private async Task<ContextOccurrencePageLoad> ReadOccurrencePageAsync(
        Guid buildId,
        long variantId,
        PhraseContextSelection selection,
        long offset,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var command = CreateContextCommand(string.Concat(
            FilteredOccurrencesSql,
            OccurrencePageSql));
        AddContextParameters(command, buildId, variantId, selection);
        command.Parameters.AddWithValue("offset", NpgsqlDbType.Bigint, offset);
        command.Parameters.AddWithValue("page_size", pageSize);

        var totalCount = 0;
        var hasInvalidExactIdentity = false;
        ContextOccurrenceRow? representative = null;
        var items = new List<ContextOccurrenceRow>();
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            totalCount = reader.GetInt32(0);
            hasInvalidExactIdentity = reader.GetBoolean(1);
            if (reader.IsDBNull(2))
            {
                continue;
            }

            var rowNumber = reader.GetInt64(2);
            var occurrence = ReadOccurrenceRow(reader, 3);
            if (rowNumber == 1)
            {
                representative = occurrence;
            }

            if (rowNumber > offset && rowNumber <= offset + pageSize)
            {
                items.Add(occurrence);
            }
        }

        if (hasInvalidExactIdentity)
        {
            throw new InvalidDataException("PhraseSearch context source contains a word without the selected exact identity.");
        }

        if (totalCount > 0 && representative is null)
        {
            throw new InvalidDataException("PhraseSearch context query did not return its representative occurrence.");
        }

        return new ContextOccurrencePageLoad(totalCount, representative, items);
    }

    private async Task<ContextOccurrenceLoad> ReadAllFilteredOccurrencesAsync(
        Guid buildId,
        long variantId,
        PhraseContextSelection selection,
        CancellationToken cancellationToken)
    {
        await using var command = CreateContextCommand(string.Concat(
            FilteredOccurrencesSql,
            AllFilteredOccurrencesSql));
        AddContextParameters(command, buildId, variantId, selection);

        var totalOccurrenceCount = 0;
        var hasInvalidExactIdentity = false;
        var items = new List<ContextOccurrenceRow>();
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            hasInvalidExactIdentity = reader.GetBoolean(0);
            totalOccurrenceCount = reader.GetInt32(1);
            if (!reader.IsDBNull(2))
            {
                items.Add(ReadOccurrenceRow(reader, 2));
            }
        }

        if (hasInvalidExactIdentity)
        {
            throw new InvalidDataException("PhraseSearch context source contains a word without the selected exact identity.");
        }

        if (items.Count != totalOccurrenceCount)
        {
            throw new InvalidDataException("PhraseSearch context query did not return every filtered occurrence.");
        }

        return new ContextOccurrenceLoad(totalOccurrenceCount, items);
    }

    private sealed record ContextAyahPageLoad(
        int TotalOccurrenceCount,
        int TotalAyahCount,
        IReadOnlyList<ContextOccurrenceRow> Items);

    private sealed record ContextOccurrencePageLoad(
        int TotalCount,
        ContextOccurrenceRow? Representative,
        IReadOnlyList<ContextOccurrenceRow> Items);

    private sealed record ContextOccurrenceLoad(
        int TotalOccurrenceCount,
        IReadOnlyList<ContextOccurrenceRow> Items);
}
