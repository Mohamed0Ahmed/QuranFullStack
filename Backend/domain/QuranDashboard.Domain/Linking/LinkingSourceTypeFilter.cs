namespace QuranDashboard.Domain.Linking;

public static class LinkingSourceTypeFilter
{
    public static IReadOnlyList<string> TypeCodesOf(LinkingSourceDescriptor descriptor) => descriptor switch
    {
        LinkingSourceDescriptor.UniqueWord source => source.TypeCodes,
        LinkingSourceDescriptor.Root source => source.TypeCodes,
        LinkingSourceDescriptor.Lemma source => source.TypeCodes,
        LinkingSourceDescriptor.Stem source => source.TypeCodes,
        _ => [],
    };

    public static LinkingSourceDescriptor Apply(
        LinkingSourceDescriptor descriptor,
        IEnumerable<string>? typeCodes) => descriptor switch
        {
            LinkingSourceDescriptor.UniqueWord source => new LinkingSourceDescriptor.UniqueWord(
                source.Mode, source.WordId, typeCodes, source.Label),
            LinkingSourceDescriptor.Root source => new LinkingSourceDescriptor.Root(
                source.RootId, typeCodes, source.Label),
            LinkingSourceDescriptor.Lemma source => new LinkingSourceDescriptor.Lemma(
                source.LemmaId, typeCodes, source.Label),
            LinkingSourceDescriptor.Stem source => new LinkingSourceDescriptor.Stem(
                source.StemId, typeCodes, source.Label),
            _ => throw new ArgumentException(
                "The linking source does not support word type filters.",
                nameof(descriptor)),
        };

    public static bool Supports(LinkingSourceDescriptor descriptor) => descriptor is
        LinkingSourceDescriptor.UniqueWord
        or LinkingSourceDescriptor.Root
        or LinkingSourceDescriptor.Lemma
        or LinkingSourceDescriptor.Stem;
}
