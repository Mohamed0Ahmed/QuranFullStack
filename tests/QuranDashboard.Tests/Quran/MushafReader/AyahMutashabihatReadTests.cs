using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahMutashabihat;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class AyahMutashabihatReadTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetAyahMutashabihat_returns_empty_groups_for_ayah_without_mutashabihat()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahMutashabihatHandler>();

        var outcome = await handler.HandleAsync(new GetAyahMutashabihatQuery("1:1"), CancellationToken.None);

        var response = outcome.Should().BeOfType<GetAyahMutashabihatOutcome.Success>().Subject.Response;
        response.VerseKey.Should().Be("1:1");
        response.GroupCount.Should().Be(0);
        response.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAyahMutashabihat_returns_multiple_separate_groups_for_selected_ayah()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahMutashabihatHandler>();

        var outcome = await handler.HandleAsync(new GetAyahMutashabihatQuery("2:25"), CancellationToken.None);

        var response = outcome.Should().BeOfType<GetAyahMutashabihatOutcome.Success>().Subject.Response;
        response.GroupCount.Should().Be(2);
        response.Groups.Should().HaveCount(2);
        response.Groups.Select(group => group.SourceGroupId).Should().BeEquivalentTo([90001, 90002]);
        response.Groups.Select(group => group.GroupKey).Should().BeEquivalentTo(
            ["mutashabihat:90001", "mutashabihat:90002"]);
    }

    [Fact]
    public async Task GetAyahMutashabihat_includes_sibling_occurrences_within_each_group()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahMutashabihatHandler>();

        var outcome = await handler.HandleAsync(new GetAyahMutashabihatQuery("2:25"), CancellationToken.None);

        var response = outcome.Should().BeOfType<GetAyahMutashabihatOutcome.Success>().Subject.Response;
        var group90001 = response.Groups.Single(group => group.SourceGroupId == 90001);

        group90001.OccurrenceCount.Should().Be(2);
        group90001.Occurrences.Should().HaveCount(2);
        group90001.Occurrences.Select(occurrence => occurrence.VerseKey).Should().BeEquivalentTo(["2:25", "2:26"]);
    }

    [Fact]
    public async Task GetAyahMutashabihat_marks_selected_ayah_occurrences_and_selected_occurrence_lists()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahMutashabihatHandler>();

        var outcome = await handler.HandleAsync(new GetAyahMutashabihatQuery("2:25"), CancellationToken.None);

        var response = outcome.Should().BeOfType<GetAyahMutashabihatOutcome.Success>().Subject.Response;
        var group90001 = response.Groups.Single(group => group.SourceGroupId == 90001);
        var group90002 = response.Groups.Single(group => group.SourceGroupId == 90002);

        group90001.SelectedOccurrences.Should().ContainSingle();
        group90001.SelectedOccurrences[0].VerseKey.Should().Be("2:25");
        group90001.SelectedOccurrences[0].IsRepresentative.Should().BeTrue();

        group90002.SelectedOccurrences.Should().ContainSingle();
        group90002.SelectedOccurrences[0].WordFrom.Should().Be(3);
        group90002.SelectedOccurrences[0].WordTo.Should().Be(4);

        var selectedOccurrence = group90001.Occurrences.Single(occurrence => occurrence.IsSelectedAyah);
        selectedOccurrence.IsRepresentative.Should().BeTrue();
        group90001.Occurrences.Single(occurrence => occurrence.VerseKey == "2:26").IsSelectedAyah.Should().BeFalse();
    }

    [Fact]
    public async Task GetAyahMutashabihat_derives_phrase_text_from_canonical_words()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahMutashabihatHandler>();

        var outcome = await handler.HandleAsync(new GetAyahMutashabihatQuery("2:25"), CancellationToken.None);

        var response = outcome.Should().BeOfType<GetAyahMutashabihatOutcome.Success>().Subject.Response;
        var group90001 = response.Groups.Single(group => group.SourceGroupId == 90001);
        var group90002 = response.Groups.Single(group => group.SourceGroupId == 90002);

        group90001.PhraseTextUthmani.Should().Be("وَبَشِّرِ ٱلَّذِينَ");
        group90001.SelectedOccurrences[0].PhraseTextUthmani.Should().Be("وَبَشِّرِ ٱلَّذِينَ");

        var siblingOccurrence = group90001.Occurrences.Single(occurrence => occurrence.VerseKey == "2:26");
        siblingOccurrence.PhraseTextUthmani.Should().Be("ٱللَّهُ");
        siblingOccurrence.TextUthmani.Should().Be("ٱللَّهُ لَا إِلَٰهَ إِلَّا هُوَ ٱلْحَىُّ ٱلْقَيُّومُ");

        group90002.PhraseTextUthmani.Should().Be("ءَامَنُوا۟ وَعَمِلُوا۟");
        group90002.SelectedOccurrences[0].PhraseTextUthmani.Should().Be("ءَامَنُوا۟ وَعَمِلُوا۟");
    }

    [Fact]
    public async Task GetAyahMutashabihat_sorts_groups_and_occurrences_in_mushaf_order()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahMutashabihatHandler>();

        var outcome = await handler.HandleAsync(new GetAyahMutashabihatQuery("2:25"), CancellationToken.None);

        var response = outcome.Should().BeOfType<GetAyahMutashabihatOutcome.Success>().Subject.Response;
        response.Groups.Select(group => group.SourceGroupId).Should().ContainInOrder(90001, 90002);

        var group90001 = response.Groups.Single(group => group.SourceGroupId == 90001);
        group90001.Occurrences.Select(occurrence => occurrence.VerseKey).Should().ContainInOrder("2:25", "2:26");
    }
}
