namespace QuranDashboard.Domain.Quran.Words;

public sealed record VerseKey
{
    public string Value { get; }

    public int Surah { get; }
    public int Ayah { get; }

    public VerseKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Verse key cannot be empty.", nameof(value));

        var parts = value.Split(':');
        if (parts.Length != 2)
            throw new ArgumentException($"Verse key must have 2 parts separated by ':', got '{value}'.", nameof(value));

        if (!int.TryParse(parts[0], out var s) || s <= 0)
            throw new ArgumentException($"Surah part must be a positive integer in '{value}'.", nameof(value));

        if (!int.TryParse(parts[1], out var a) || a <= 0)
            throw new ArgumentException($"Ayah part must be a positive integer in '{value}'.", nameof(value));

        Value = value;
        Surah = s;
        Ayah = a;
    }

    public static VerseKey From(int surah, int ayah)
    {
        if (surah <= 0) throw new ArgumentOutOfRangeException(nameof(surah));
        if (ayah <= 0) throw new ArgumentOutOfRangeException(nameof(ayah));

        return new VerseKey($"{surah}:{ayah}");
    }

    public override string ToString() => Value;
}
