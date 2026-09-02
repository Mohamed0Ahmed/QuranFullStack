using System.Text.Json;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.DataImporter.Import.QuranTopicsBook;

internal sealed partial class QuranTopicsBookImporter
{
    private async Task<IReadOnlyDictionary<string, AbwabSection>> InsertSectionsAsync(
        QuranTopicsBookDocument document,
        int actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sections = document.Sections
            .OrderBy(section => section.Order)
            .Select(section => new AbwabSection
            {
                Name = section.Name.Trim(),
                OrderValue = section.Order,
                CreatedAtUtc = now,
                CreatedBy = actorUserId,
                UpdatedAtUtc = now,
                UpdatedBy = actorUserId,
            })
            .ToList();
        db.AbwabSections.AddRange(sections);
        await db.SaveChangesAsync(cancellationToken);
        return document.Sections
            .OrderBy(section => section.Order)
            .Zip(sections)
            .ToDictionary(pair => pair.First.Key, pair => pair.Second, StringComparer.Ordinal);
    }

    private async Task<IReadOnlyDictionary<string, AbwabDoor>> InsertDoorsAsync(
        QuranTopicsBookDocument document,
        IReadOnlyDictionary<string, AbwabSection> sectionsByKey,
        int actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var definitions = document.Sections
            .SelectMany(section => section.Doors.Select(door => (section, door)))
            .ToDictionary(item => item.door.Key, StringComparer.Ordinal);
        var persisted = new Dictionary<string, AbwabDoor>(StringComparer.Ordinal);
        while (persisted.Count < definitions.Count)
        {
            var ready = definitions.Values
                .Where(item => !persisted.ContainsKey(item.door.Key)
                    && (item.door.ParentKey is null || persisted.ContainsKey(item.door.ParentKey)))
                .OrderBy(item => item.section.Order)
                .ThenBy(item => item.door.GlobalOrder ?? int.MaxValue)
                .ThenBy(item => item.door.Order)
                .ToList();
            if (ready.Count == 0)
            {
                throw new QuranTopicsBookImportException("The validated door hierarchy could not be ordered parent-first.");
            }

            foreach (var item in ready)
            {
                var door = new AbwabDoor
                {
                    SectionId = sectionsByKey[item.section.Key].Id,
                    ParentId = item.door.ParentKey is null ? null : persisted[item.door.ParentKey].Id,
                    Name = item.door.Name.Trim(),
                    OrderValue = item.door.Order,
                    GlobalOrderValue = item.door.GlobalOrder,
                    CreatedAtUtc = now,
                    CreatedBy = actorUserId,
                    UpdatedAtUtc = now,
                    UpdatedBy = actorUserId,
                };
                persisted.Add(item.door.Key, door);
                db.AbwabDoors.Add(door);
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        return persisted;
    }

    private async Task InsertLinksAsync(
        QuranTopicsBookDocument document,
        IReadOnlyDictionary<string, AbwabDoor> doorsByKey,
        IReadOnlyDictionary<string, Ayah> ayahsByVerseKey,
        int actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var definition in document.Sections
                     .SelectMany(section => section.Doors)
                     .Where(door => door.AyahGroups.Count > 0)
                     .OrderBy(door => doorsByKey[door.Key].Id))
        {
            var door = doorsByKey[definition.Key];
            var groups = definition.AyahGroups.OrderBy(group => group.Order).ToList();
            var distinctAyahs = groups
                .SelectMany(group => group.VerseKeys)
                .Distinct(StringComparer.Ordinal)
                .Select(verseKey => ayahsByVerseKey[verseKey])
                .ToList();
            var operation = new LinkingOperation
            {
                DoorId = door.Id,
                ActorUserId = actorUserId,
                IdempotencyKey = Guid.NewGuid(),
                ConfirmedAtUtc = now,
                SourceCount = groups.Count,
                AyahCount = distinctAyahs.Count,
                OutcomeJson = JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    kind = "quran_topics_book_import",
                    doorKey = definition.Key,
                }),
            };
            db.LinkingOperations.Add(operation);
            await db.SaveChangesAsync(cancellationToken);

            var preparedGroups = groups.Select(group => PrepareGroup(
                document.Source.FileName,
                definition,
                group,
                door.Id,
                operation.Id,
                ayahsByVerseKey,
                actorUserId,
                now)).ToList();
            db.LinkingSourceContributions.AddRange(preparedGroups.Select(item => item.Contribution));
            db.LinkingUnits.AddRange(preparedGroups.Select(item => item.Unit));
            await db.SaveChangesAsync(cancellationToken);

            foreach (var item in preparedGroups)
            {
                db.LinkingSourceContributionUnits.Add(new LinkingSourceContributionUnit
                {
                    SourceContributionId = item.Contribution.Id,
                    UnitId = item.Unit.Id,
                    OrderValue = 1,
                });
                db.LinkingUnitAyahs.AddRange(item.Ayahs.Select((ayah, index) => new LinkingUnitAyah
                {
                    UnitId = item.Unit.Id,
                    AyahId = ayah.Id,
                    OrderValue = index + 1,
                }));
            }

            db.LinkingDoorAyahs.AddRange(distinctAyahs.Select(ayah => new LinkingDoorAyah
            {
                DoorId = door.Id,
                AyahId = ayah.Id,
                CreatedAtUtc = now,
                CreatedBy = actorUserId,
            }));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static PreparedGroup PrepareGroup(
        string sourceFileName,
        QuranTopicsBookDoor door,
        QuranTopicsBookAyahGroup group,
        int doorId,
        long operationId,
        IReadOnlyDictionary<string, Ayah> ayahsByVerseKey,
        int actorUserId,
        DateTimeOffset now)
    {
        var ayahs = group.VerseKeys.Select(verseKey => ayahsByVerseKey[verseKey]).ToList();
        var contextKey = $"book:{sourceFileName}:door:{door.Key}:reference:{group.Order}";
        var descriptor = new LinkingSourceDescriptor.ManualMushafAyahs(
            group.VerseKeys.Select(verseKey => new VerseKey(verseKey)),
            door.Name,
            contextKey);
        var isGrouped = group.Kind == QuranTopicsBookContract.ConsecutiveRangeGroupKind;
        var mode = isGrouped ? LinkingContributionMode.ManualGrouped : LinkingContributionMode.ManualSingle;
        var contributionIdentity = LinkingContributionIdentity.For(descriptor, mode);
        var intents = ayahs.Select(ayah => new LinkingOperationAyahIntent(
            ayah.Id,
            ayah.VerseKey,
            ayah.SurahNumber,
            ayah.AyahNumber,
            [],
            [],
            null,
            [])).ToList();
        var unitIdentity = LinkingUnitIdentity.For(isGrouped, intents);

        return new PreparedGroup(
            new LinkingSourceContribution
            {
                OperationId = operationId,
                DoorId = doorId,
                OrderValue = group.Order,
                ContributionMode = mode,
                SourceKind = LinkingSourceKind.ManualMushafAyahs,
                SourceIdentity = contributionIdentity,
                SourceIdentityHash = LinkingSourceIdentity.HashOf(contributionIdentity),
                Label = door.Name,
                ScopeJson = JsonSerializer.Serialize(new { schemaVersion = 1, contextKey }),
                ResolvedAyahCount = ayahs.Count,
                ResolvedAtUtc = now,
                CreatedAtUtc = now,
                CreatedBy = actorUserId,
                UpdatedAtUtc = now,
                UpdatedBy = actorUserId,
            },
            new LinkingUnit
            {
                DoorId = doorId,
                Identity = unitIdentity,
                IdentityHash = LinkingUnitIdentity.HashOf(unitIdentity),
                IsGrouped = isGrouped,
                CreatedAtUtc = now,
                CreatedBy = actorUserId,
            },
            ayahs);
    }
}
