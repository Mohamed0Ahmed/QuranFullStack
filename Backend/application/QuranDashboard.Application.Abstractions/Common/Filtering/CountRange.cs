namespace QuranDashboard.Application.Abstractions.Common.Filtering;

public readonly record struct CountRange(int? Min, int? Max)
{
    public static readonly CountRange Unbounded = new(null, null);

    public bool IsActive => Min.HasValue || Max.HasValue;

    public bool IsValid =>
        (Min is null || Min.Value >= 0)
        && (Max is null || Max.Value >= 0)
        && (Min is null || Max is null || Min.Value <= Max.Value);

    public bool Includes(int value) =>
        (Min is null || value >= Min.Value) && (Max is null || value <= Max.Value);
}
