namespace QuranDashboard.Domain.Quran.Words;

public sealed record WordLocation
{
    public string Value { get; }

    public int Surah { get; }
    public int Ayah { get; }
    public int Word { get; }

    public WordLocation(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Word location cannot be empty.", nameof(value));

        var parts = value.Split(':');
        if (parts.Length != 3)
            throw new ArgumentException($"Word location must have 3 parts separated by ':', got '{value}'.", nameof(value));

        if (!int.TryParse(parts[0], out var s) || s <= 0)
            throw new ArgumentException($"Surah part must be a positive integer in '{value}'.", nameof(value));

        if (!int.TryParse(parts[1], out var a) || a <= 0)
            throw new ArgumentException($"Ayah part must be a positive integer in '{value}'.", nameof(value));

        if (!int.TryParse(parts[2], out var w) || w <= 0)
            throw new ArgumentException($"Word part must be a positive integer in '{value}'.", nameof(value));

        Value = value;
        Surah = s;
        Ayah = a;
        Word = w;
    }

    public static WordLocation From(int surah, int ayah, int word)
    {
        if (surah <= 0) throw new ArgumentOutOfRangeException(nameof(surah));
        if (ayah <= 0) throw new ArgumentOutOfRangeException(nameof(ayah));
        if (word <= 0) throw new ArgumentOutOfRangeException(nameof(word));

        return new WordLocation($"{surah}:{ayah}:{word}");
    }

    public override string ToString() => Value;
}
