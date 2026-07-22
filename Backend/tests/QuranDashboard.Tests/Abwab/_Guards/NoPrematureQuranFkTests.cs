using Microsoft.EntityFrameworkCore.Metadata;
using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab._Guards;

// FR-009: the first Abwab->Quran foreign key MUST stay prohibited until this feature's exit is
// accepted. This guard structurally enforces that FK prohibition (the normative requirement): it
// reflects the REAL migrated EF model and fails the moment any Abwab entity is wired to a Quran
// entity by a foreign key — 029's premature FK, or a mistaken 028 change. It stays green on 028's
// own Quran-FK-free substrate (audit/timeline/concurrency). Per-writer barrier governance (that
// every Abwab writer takes the write barrier) is a separate concern owned by the US3 write-barrier
// registry test (T029), not this FK guard. A companion assertion proves the Quran side is actually
// classified, so the boundary check can never pass vacuously.
[Collection(nameof(AbwabDbCollection))]
public sealed class NoPrematureQuranFkTests
{
    private readonly PostgresFixture _fixture;

    public NoPrematureQuranFkTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public void NoForeignKeyConnectsAbwabAndQuran()
    {
        var offenders = _fixture.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys())
            .Where(CrossesAbwabQuranBoundary)
            .Select(Describe)
            .ToList();

        offenders.Should().BeEmpty(
            "FR-009 prohibits the first Abwab->Quran foreign key until feature 028 exits; offending FK(s): "
            + string.Join("; ", offenders));
    }

    [Fact]
    public void QuranEntitiesAreClassifiedSoTheGuardIsNotVacuous()
    {
        _fixture.Model.GetEntityTypes().Where(IsQuran).Should().NotBeEmpty(
            "the guard's Quran detection must see real Quran entities, otherwise the FK boundary check proves nothing");
    }

    [Fact]
    public void AbwabEntitiesAreClassifiedSoTheGuardIsNotVacuous()
    {
        // Symmetric companion: now that 028 maps real Abwab substrate entities, the guard's Abwab detection
        // must see at least one, otherwise the cross-boundary FK check could pass vacuously from the Abwab side.
        _fixture.Model.GetEntityTypes().Where(IsAbwab).Should().NotBeEmpty(
            "the guard's Abwab detection must see real Abwab entities, otherwise the FK boundary check proves nothing");
    }

    private static bool CrossesAbwabQuranBoundary(IForeignKey fk)
    {
        var dependent = fk.DeclaringEntityType;
        var principal = fk.PrincipalEntityType;
        return (IsAbwab(dependent) && IsQuran(principal))
            || (IsQuran(dependent) && IsAbwab(principal));
    }

    private static bool IsAbwab(IReadOnlyEntityType entity) =>
        NamespaceHasSegment(entity, "Abwab") || TableStartsWith(entity, "abwab") || SchemaEquals(entity, "abwab");

    private static bool IsQuran(IReadOnlyEntityType entity) =>
        NamespaceHasSegment(entity, "Quran") || TableStartsWith(entity, "quran_");

    private static bool NamespaceHasSegment(IReadOnlyEntityType entity, string segment) =>
        (entity.ClrType.Namespace ?? string.Empty)
            .Split('.')
            .Contains(segment, StringComparer.Ordinal);

    private static bool TableStartsWith(IReadOnlyEntityType entity, string prefix) =>
        (entity.GetTableName() ?? string.Empty).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private static bool SchemaEquals(IReadOnlyEntityType entity, string schema) =>
        string.Equals(entity.GetSchema(), schema, StringComparison.OrdinalIgnoreCase);

    private static string Describe(IForeignKey fk) =>
        $"{fk.DeclaringEntityType.ShortName()} -> {fk.PrincipalEntityType.ShortName()}";
}
