using FluentAssertions;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;
using QuranDashboard.Application.Quran.Words.GenerateI3rab;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;
using QuranDashboard.Infrastructure.Files.Quran.Morphology.Irab;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Irab;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

[Collection(nameof(I3rabGenerationTestCollection))]
public sealed class I3rabIdempotencyTests(I3rabGenerationTestFixture fixture)
{
    [Fact]
    public async Task Force_rerun_twice_produces_identical_committed_segment_and_rule_state()
    {
        await fixture.ResetToCompleteMorphologyAsync();
        var counts = I3rabGenerationTestFixture.CompleteMorphologyCounts;

        var first = await fixture.RunGenerationAsync(counts, force: true);
        first.Succeeded.Should().BeTrue(first.Message);

        var firstSnapshot = await fixture.CaptureCommittedStateAsync();

        var second = await fixture.RunGenerationAsync(counts, force: true);
        second.Succeeded.Should().BeTrue(second.Message);

        var secondSnapshot = await fixture.CaptureCommittedStateAsync();
        secondSnapshot.Should().BeEquivalentTo(firstSnapshot);
    }
}
