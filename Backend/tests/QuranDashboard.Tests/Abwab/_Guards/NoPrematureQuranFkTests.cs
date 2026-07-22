using Microsoft.EntityFrameworkCore.Metadata;
using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab._Guards;

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
