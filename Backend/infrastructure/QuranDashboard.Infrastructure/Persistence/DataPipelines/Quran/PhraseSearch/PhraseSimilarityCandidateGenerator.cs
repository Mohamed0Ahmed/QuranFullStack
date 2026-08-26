namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseSimilarityCandidateGenerator
{
    internal PhraseSimilarityCandidateSet Create(
        IReadOnlyList<PhraseVariantVector> variants,
        short wordCount)
    {
        if (variants.Count < 2)
        {
            return new PhraseSimilarityCandidateSet(
                "none",
                0,
                0,
                0,
                GC.GetTotalMemory(false),
                false,
                new HashSet<ulong>());
        }

        var requiredMatches = (wordCount + 1) / 2;
        var prefixLength = wordCount - requiredMatches + 1;
        var featureFrequency = BuildFeatureFrequency(variants);
        var prefixes = BuildPrefixes(variants, featureFrequency, prefixLength);
        var candidateEmissionEstimate = EstimateCandidateEmissions(prefixes);
        var bruteForcePairs = PairCount(variants.Count);

        if (bruteForcePairs <= candidateEmissionEstimate)
        {
            return new PhraseSimilarityCandidateSet(
                "bounded-brute-force",
                bruteForcePairs,
                bruteForcePairs,
                bruteForcePairs,
                GC.GetTotalMemory(false),
                true,
                new HashSet<ulong>());
        }

        var (candidates, emissions) = BuildPrefixCandidates(prefixes);
        return new PhraseSimilarityCandidateSet(
            "rarity-overlap-prefix",
            emissions,
            candidates.Count,
            candidates.Count,
            GC.GetTotalMemory(false),
            false,
            candidates);
    }

    internal static int UnpackLeftIndex(ulong pair) => (int)(pair >> 32);

    internal static int UnpackRightIndex(ulong pair) => (int)(pair & uint.MaxValue);

    private static Dictionary<long, int> BuildFeatureFrequency(
        IReadOnlyList<PhraseVariantVector> variants)
    {
        var result = new Dictionary<long, int>();
        foreach (var variant in variants)
        {
            for (var position = 0; position < variant.ExactTokenIds.Length; position++)
            {
                var key = PackFeature(position, variant.ExactTokenIds[position]);
                result[key] = result.GetValueOrDefault(key) + 1;
            }
        }

        return result;
    }

    private static IReadOnlyList<long[]> BuildPrefixes(
        IReadOnlyList<PhraseVariantVector> variants,
        IReadOnlyDictionary<long, int> featureFrequency,
        int prefixLength)
    {
        var prefixes = new long[variants.Count][];
        for (var variantIndex = 0; variantIndex < variants.Count; variantIndex++)
        {
            var variant = variants[variantIndex];
            prefixes[variantIndex] = variant.ExactTokenIds
                .Select((tokenId, position) => PackFeature(position, tokenId))
                .OrderBy(feature => featureFrequency[feature])
                .ThenBy(feature => feature)
                .Take(prefixLength)
                .ToArray();
        }

        return prefixes;
    }

    private static long EstimateCandidateEmissions(IReadOnlyList<long[]> prefixes)
    {
        var counts = new Dictionary<long, long>();
        foreach (var prefix in prefixes)
        {
            foreach (var feature in prefix)
            {
                counts[feature] = counts.GetValueOrDefault(feature) + 1;
            }
        }

        return counts.Values.Sum(count => count * (count - 1) / 2);
    }

    private static (HashSet<ulong> Candidates, long Emissions) BuildPrefixCandidates(
        IReadOnlyList<long[]> prefixes)
    {
        var postings = new Dictionary<long, List<int>>();
        var candidates = new HashSet<ulong>();
        long emissions = 0;

        for (var rightIndex = 0; rightIndex < prefixes.Count; rightIndex++)
        {
            foreach (var feature in prefixes[rightIndex])
            {
                if (!postings.TryGetValue(feature, out var leftIndexes))
                {
                    leftIndexes = [];
                    postings.Add(feature, leftIndexes);
                }

                foreach (var leftIndex in leftIndexes)
                {
                    emissions++;
                    candidates.Add(PackPair(leftIndex, rightIndex));
                }

                leftIndexes.Add(rightIndex);
            }
        }

        return (candidates, emissions);
    }

    private static long PairCount(int count) => (long)count * (count - 1) / 2;

    private static long PackFeature(int position, int tokenId) =>
        ((long)position << 32) | (uint)tokenId;

    private static ulong PackPair(int leftIndex, int rightIndex) =>
        ((ulong)(uint)leftIndex << 32) | (uint)rightIndex;
}
