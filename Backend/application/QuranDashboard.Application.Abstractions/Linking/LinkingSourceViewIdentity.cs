using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QuranDashboard.Application.Abstractions.Linking;

public static class LinkingSourceViewIdentity
{
    public static string Compute(string resolutionIdentity, LinkingSourcePageView view)
    {
        ArgumentNullException.ThrowIfNull(resolutionIdentity);
        ArgumentNullException.ThrowIfNull(view);

        var canonical = string.Join(
            '|',
            Encode(resolutionIdentity),
            SegmentToken(view.Segment),
            view.InclusionMode is null ? "-" : InclusionToken(view.InclusionMode.Value),
            string.Join(',', view.AyahOverrideIds.Distinct().Order()),
            string.Join(
                ',',
                view.TypeCodes
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .Select(Encode)));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public static bool Includes(LinkingSourcePageView view, int ayahId)
    {
        if (view.Segment == LinkingSourcePageSegment.All)
        {
            return true;
        }

        var overridden = view.AyahOverrideIds.Contains(ayahId);
        var included = view.InclusionMode switch
        {
            Domain.Linking.LinkingInclusionMode.AllExcept => !overridden,
            Domain.Linking.LinkingInclusionMode.Only => overridden,
            _ => throw new ArgumentException("An included or excluded source view requires inclusion state."),
        };

        return view.Segment == LinkingSourcePageSegment.Included ? included : !included;
    }

    private static string SegmentToken(LinkingSourcePageSegment segment) => segment switch
    {
        LinkingSourcePageSegment.All => "all",
        LinkingSourcePageSegment.Included => "included",
        LinkingSourcePageSegment.Excluded => "excluded",
        _ => throw new ArgumentOutOfRangeException(nameof(segment)),
    };

    private static string InclusionToken(Domain.Linking.LinkingInclusionMode mode) => mode switch
    {
        Domain.Linking.LinkingInclusionMode.AllExcept => "all_except",
        Domain.Linking.LinkingInclusionMode.Only => "only",
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    private static string Encode(string value) =>
        string.Create(CultureInfo.InvariantCulture, $"{Encoding.UTF8.GetByteCount(value)}:{value}");
}
