namespace QuranDashboard.Domain.Quran.Words;

public sealed class QuranWord
{
    public int Id { get; set; }
    public string Location { get; set; } = string.Empty;
    public int AyahId { get; set; }
    public short SurahNumber { get; set; }
    public short AyahNumber { get; set; }
    public short WordNumber { get; set; }
    public short PageNumber { get; set; }
    public short LineNumber { get; set; }
    public short LineWordOrder { get; set; }
    public string QpcGlyph { get; set; } = string.Empty;
    public string TextUthmani { get; set; } = string.Empty;
    public string TextUthmaniSimple { get; set; } = string.Empty;
    public string TextImlaeiSimple { get; set; } = string.Empty;
    public string WordKeyImlaeiSimple { get; set; } = string.Empty;
    public bool IsAyahMarker { get; set; }
    public int? UniqueTashkeelWordId { get; set; }
    public int? UniqueSimpleWordId { get; set; }

    public Ayahs.Ayah Ayah { get; set; } = null!;
    public MushafPages.MushafPage Page { get; set; } = null!;
}
