using System.Data;
using System.Text.Json;
using Npgsql;
using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;
using QuranDashboard.Application.Quran.Mutashabihat.ImportMutashabihat;
using QuranDashboard.Domain.Quran.Mutashabihat;

namespace QuranDashboard.Tests.Quran.Mutashabihat;

[Collection(nameof(MutashabihatImportTestCollection))]
public sealed class MutashabihatReadQueryTests(MutashabihatImportTestFixture fixture)
{
    private const string GroupsOfAyahSql = """
        SELECT g.id, g.source_group_id
        FROM quran_mutashabihat_occurrences o
        JOIN quran_mutashabihat_groups g ON g.id = o.group_id
        WHERE o.ayah_id = @ayahId
        ORDER BY g.id
        """;

    private const string OccurrencesOfGroupSql = """
        SELECT id, ayah_id, word_from, word_to, is_representative
        FROM quran_mutashabihat_occurrences
        WHERE group_id = @groupId
        ORDER BY id
        """;

    private const string OutgoingLinksSql = """
        SELECT id, source_ayah_id, target_ayah_id, score, coverage
        FROM quran_similar_ayah_links
        WHERE source_ayah_id = @ayahId
        ORDER BY id
        """;

    private const string IncomingLinksSql = """
        SELECT id, source_ayah_id, target_ayah_id, score, coverage
        FROM quran_similar_ayah_links
        WHERE target_ayah_id = @ayahId
        ORDER BY id
        """;

    private const string MutashabihatTablesSql = """
        SELECT table_name
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND (
            table_name LIKE 'quran_mutashabihat%'
            OR table_name = 'quran_similar_ayah_links'
          )
        ORDER BY table_name
        """;

    private const string TableExistsSql = """
        SELECT EXISTS (
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name = @tableName
        )
        """;

    private const string IndexOnColumnSql = """
        SELECT EXISTS (
            SELECT 1
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND tablename = @tableName
              AND indexdef ILIKE '%' || @columnName || '%'
        )
        """;

    private static readonly MutashabihatExpectedCounts ReadQueryExpectedCounts = new(
        Groups: 2,
        RawOccurrences: 4,
        StoredOccurrences: 4,
        SimilarSources: 2,
        SimilarLinks: 2,
        DistinctAyahs: 3);

    [Fact]
    public async Task Read_queries_answer_groups_occurrences_and_incoming_links_without_extra_tables()
    {
        await fixture.SeedSyntheticAyahsAsync((1, "900:1"), (2, "900:2"), (3, "900:3"));
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync(
            phrases: ReadQueryPhrases,
            similarAyahs: ReadQuerySimilarAyahs);

        var result = await fixture.RunImportAsync(sourcePath, expectedCounts: ReadQueryExpectedCounts);
        result.ExitCode.Should().Be(ImportMutashabihatResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider(services => services.AddMutashabihatImportServices())
            .CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var connection = await OpenConnectionAsync(dbContext);

        (await TableExistsAsync(connection, "phrase_verses")).Should().BeFalse(
            "phrase_verses is a derivable reverse index and must not be stored");

        var groupsForSharedAyah = await QueryGroupsOfAyahAsync(connection, ayahId: 2);
        groupsForSharedAyah.Should().HaveCount(2, "ayah 900:2 appears in both phrase groups");
        groupsForSharedAyah.Select(group => group.SourceGroupId).Should().BeEquivalentTo([90_001, 90_002]);

        var firstGroupId = groupsForSharedAyah[0].Id;
        var occurrences = await QueryOccurrencesOfGroupAsync(connection, firstGroupId);
        occurrences.Should().HaveCount(2);
        occurrences.Select(occurrence => occurrence.AyahId).Should().BeEquivalentTo([1, 2]);

        var outgoingFromAyah1 = await QueryOutgoingLinksAsync(connection, ayahId: 1);
        outgoingFromAyah1.Should().ContainSingle();
        outgoingFromAyah1[0].TargetAyahId.Should().Be(2);

        var incomingToAyah2 = await QueryIncomingLinksAsync(connection, ayahId: 2);
        incomingToAyah2.Should().HaveCount(2, "incoming links enable an undirected view at read time");
        incomingToAyah2.Select(link => link.SourceAyahId).Should().BeEquivalentTo([1, 3]);

        var storedLinks = await dbContext.SimilarAyahLinks.AsNoTracking().ToListAsync();
        storedLinks.Should().HaveCount(2);
        storedLinks.Should().NotContain(link => link.SourceAyahId == 2 && link.TargetAyahId == 1,
            "directed links are stored faithfully — no synthesized reverse rows");
        storedLinks.Should().NotContain(link => link.SourceAyahId == 2 && link.TargetAyahId == 3);
    }

    [Fact]
    public async Task Schema_has_exactly_three_mutashabihat_tables_and_read_indexes()
    {
        await fixture.SeedSyntheticAyahsAsync((1, "900:1"), (2, "900:2"), (3, "900:3"));
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync(
            phrases: ReadQueryPhrases,
            similarAyahs: ReadQuerySimilarAyahs);

        var result = await fixture.RunImportAsync(sourcePath, expectedCounts: ReadQueryExpectedCounts);
        result.ExitCode.Should().Be(ImportMutashabihatResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var connection = await OpenConnectionAsync(dbContext);

        var mutashabihatTables = await QueryMutashabihatTableNamesAsync(connection);
        mutashabihatTables.Should().BeEquivalentTo(
        [
            "quran_mutashabihat_groups",
            "quran_mutashabihat_occurrences",
            "quran_similar_ayah_links"
        ],
        "only the three foundation tables are stored — no phrase_verses or reverse-link table");

        (await TableExistsAsync(connection, "phrase_verses")).Should().BeFalse();
        (await TableExistsAsync(connection, "quran_mutashabihat_reverse_links")).Should().BeFalse();

        (await IndexOnColumnExistsAsync(connection, "quran_mutashabihat_occurrences", "ayah_id"))
            .Should().BeTrue("occurrences(ayah_id) supports all-groups-of-an-ayah reads");
        (await IndexOnColumnExistsAsync(connection, "quran_similar_ayah_links", "target_ayah_id"))
            .Should().BeTrue("links(target_ayah_id) supports incoming/undirected reads");

        (await ExplainUsesIndexAsync(
            connection,
            "SELECT * FROM quran_mutashabihat_occurrences WHERE ayah_id = 2"))
            .Should().BeTrue();
        (await ExplainUsesIndexAsync(
            connection,
            "SELECT * FROM quran_similar_ayah_links WHERE target_ayah_id = 2"))
            .Should().BeTrue();
    }

    private static Dictionary<string, object> ReadQueryPhrases => new()
    {
        ["90001"] = new
        {
            surahs = 1,
            ayahs = 2,
            count = 2,
            source = new { key = "900:1", from = 1, to = 2 },
            ayah = new Dictionary<string, object[]>
            {
                ["900:1"] = [new[] { 1, 2 }],
                ["900:2"] = [new[] { 1, 2 }]
            }
        },
        ["90002"] = new
        {
            surahs = 1,
            ayahs = 2,
            count = 2,
            source = new { key = "900:2", from = 1, to = 2 },
            ayah = new Dictionary<string, object[]>
            {
                ["900:2"] = [new[] { 1, 2 }],
                ["900:3"] = [new[] { 1, 2 }]
            }
        }
    };

    private static Dictionary<string, object> ReadQuerySimilarAyahs => new()
    {
        ["900:1"] = new object[]
        {
            new
            {
                matched_ayah_key = "900:2",
                score = 80,
                coverage = 50,
                matched_words_count = 2,
                match_words = new[] { new[] { 1, 2 } }
            }
        },
        ["900:3"] = new object[]
        {
            new
            {
                matched_ayah_key = "900:2",
                score = 70,
                coverage = 40,
                matched_words_count = 1,
                match_words = new[] { new[] { 1 } }
            }
        }
    };

    private static async Task<NpgsqlConnection> OpenConnectionAsync(QuranDashboardDbContext dbContext)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return (NpgsqlConnection)connection;
    }

    private static async Task<IReadOnlyList<(int Id, int SourceGroupId)>> QueryGroupsOfAyahAsync(
        NpgsqlConnection connection,
        int ayahId)
    {
        await using var command = new NpgsqlCommand(GroupsOfAyahSql, connection);
        command.Parameters.AddWithValue("ayahId", ayahId);

        var groups = new List<(int Id, int SourceGroupId)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            groups.Add((reader.GetInt32(0), reader.GetInt32(1)));
        }

        return groups;
    }

    private static async Task<IReadOnlyList<MutashabihatOccurrence>> QueryOccurrencesOfGroupAsync(
        NpgsqlConnection connection,
        int groupId)
    {
        await using var command = new NpgsqlCommand(OccurrencesOfGroupSql, connection);
        command.Parameters.AddWithValue("groupId", groupId);

        var occurrences = new List<MutashabihatOccurrence>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            occurrences.Add(new MutashabihatOccurrence
            {
                Id = reader.GetInt32(0),
                GroupId = groupId,
                AyahId = reader.GetInt32(1),
                WordFrom = reader.GetInt16(2),
                WordTo = reader.GetInt16(3),
                IsRepresentative = reader.GetBoolean(4)
            });
        }

        return occurrences;
    }

    private static async Task<IReadOnlyList<SimilarAyahLink>> QueryOutgoingLinksAsync(
        NpgsqlConnection connection,
        int ayahId) =>
        await QueryLinksAsync(connection, OutgoingLinksSql, ayahId);

    private static async Task<IReadOnlyList<SimilarAyahLink>> QueryIncomingLinksAsync(
        NpgsqlConnection connection,
        int ayahId) =>
        await QueryLinksAsync(connection, IncomingLinksSql, ayahId);

    private static async Task<IReadOnlyList<SimilarAyahLink>> QueryLinksAsync(
        NpgsqlConnection connection,
        string sql,
        int ayahId)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("ayahId", ayahId);

        var links = new List<SimilarAyahLink>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            links.Add(new SimilarAyahLink
            {
                Id = reader.GetInt32(0),
                SourceAyahId = reader.GetInt32(1),
                TargetAyahId = reader.GetInt32(2),
                Score = reader.GetInt16(3),
                Coverage = reader.GetInt16(4)
            });
        }

        return links;
    }

    private static async Task<IReadOnlyList<string>> QueryMutashabihatTableNamesAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(MutashabihatTablesSql, connection);
        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static async Task<bool> TableExistsAsync(NpgsqlConnection connection, string tableName)
    {
        await using var command = new NpgsqlCommand(TableExistsSql, connection);
        command.Parameters.AddWithValue("tableName", tableName);
        var result = await command.ExecuteScalarAsync();
        return result is true;
    }

    private static async Task<bool> IndexOnColumnExistsAsync(
        NpgsqlConnection connection,
        string tableName,
        string columnName)
    {
        await using var command = new NpgsqlCommand(IndexOnColumnSql, connection);
        command.Parameters.AddWithValue("tableName", tableName);
        command.Parameters.AddWithValue("columnName", columnName);
        var result = await command.ExecuteScalarAsync();
        return result is true;
    }

    private static async Task<bool> ExplainUsesIndexAsync(NpgsqlConnection connection, string sql)
    {
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var disableSeqScan = new NpgsqlCommand("SET LOCAL enable_seqscan = off", connection, transaction))
        {
            await disableSeqScan.ExecuteNonQueryAsync();
        }

        await using var explain = new NpgsqlCommand($"EXPLAIN (FORMAT JSON) {sql}", connection, transaction);
        var json = (string?)await explain.ExecuteScalarAsync();
        json.Should().NotBeNullOrWhiteSpace();

        using var document = JsonDocument.Parse(json!);
        var planText = document.RootElement[0].GetProperty("Plan").ToString();
        await transaction.CommitAsync();

        return planText.Contains("Index", StringComparison.OrdinalIgnoreCase);
    }
}
