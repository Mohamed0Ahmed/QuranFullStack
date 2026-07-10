namespace QuranDashboard.Domain.Quran.Mutashabihat;

public sealed class MutashabihatGroup
{
    public int Id { get; set; }
    public int SourceGroupId { get; set; }
    public int RepresentativeAyahId { get; set; }
    public short RepresentativeWordFrom { get; set; }
    public short RepresentativeWordTo { get; set; }
    public short OccurrenceCount { get; set; }
    public short DistinctAyahCount { get; set; }
    public short DistinctSurahCount { get; set; }
    public string? RawSourceCounts { get; set; }
}
