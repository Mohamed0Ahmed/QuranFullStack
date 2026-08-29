namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran;

internal static class QuranTashkeelIdentitySql
{
    internal const string IdentityCte = """
        WITH display_word_identity AS (
          SELECT U&'\0640\0653\06D6\06D7\06D8\06D9\06DA\06DB\06DC\06DE\06E9\200F'
                 AS ignored_tashkeel_marks
        )
        """;
}
