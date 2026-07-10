namespace QuranDashboard.Domain.Quran.Mutashabihat;

public sealed class MutashabihatOccurrence
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int AyahId { get; set; }
    public short WordFrom { get; set; }
    public short WordTo { get; set; }
    public bool IsRepresentative { get; set; }
}
