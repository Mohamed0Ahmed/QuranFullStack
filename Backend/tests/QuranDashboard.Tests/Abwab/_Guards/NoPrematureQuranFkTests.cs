using Microsoft.EntityFrameworkCore.Infrastructure;
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

    [Fact]
    public void RepresentativeQuranExcerptIsAPlainStringWithNoAyahValidation()
    {
        var excerptProperties = _fixture.Model.GetEntityTypes()
            .Where(IsAbwab)
            .SelectMany(entity => entity.GetProperties().Select(property => (Entity: entity, Property: property)))
            .Where(pair => pair.Property.Name == "RepresentativeQuranExcerpt")
            .ToList();

        foreach (var (entity, property) in excerptProperties)
        {
            property.ClrType.Should().Be(typeof(string), "029 defines RepresentativeQuranExcerpt as a plain string, never an ayah reference type");
            property.GetValueConverter().Should().BeNull(
                $"{entity.ShortName()}.RepresentativeQuranExcerpt must carry no value converter — a converter "
                + "is how an ayah-validated type could sneak back in behind a plain-looking string column");
        }

        _fixture.Model.GetEntityTypes()
            .Where(IsAbwab)
            .SelectMany(entity => entity.GetForeignKeys())
            .Where(fk => IsQuran(fk.PrincipalEntityType))
            .Should().BeEmpty("RepresentativeQuranExcerpt must never become an ayah foreign key");

        var designTimeModel = DesignTimeModel();
        var excerptColumnNamesByEntity = excerptProperties
            .ToDictionary(pair => pair.Entity.ShortName(), pair => pair.Property.GetColumnName(), StringComparer.Ordinal);

        var checkConstraintOffenders = designTimeModel.GetEntityTypes()
            .Where(entity => excerptColumnNamesByEntity.ContainsKey(entity.ShortName()))
            .Where(entity => entity.GetCheckConstraints()
                .Any(constraint => constraint.Sql.Contains(excerptColumnNamesByEntity[entity.ShortName()], StringComparison.OrdinalIgnoreCase)))
            .Select(entity => entity.ShortName())
            .ToList();

        checkConstraintOffenders.Should().BeEmpty(
            "RepresentativeQuranExcerpt must carry no CHECK constraint — validating ayah shape/format at the "
            + "database level is exactly the premature coupling this guard forbids; offenders: "
            + string.Join(", ", checkConstraintOffenders));
    }

    [Fact]
    public void RelationshipsAndTemplatesIntroduceNoQuranForeignKeyAndKeepRepresentativeExcerptAPlainString()
    {
        var entities = _fixture.Model.GetEntityTypes()
            .Where(entity => IsAbwab(entity) &&
                (NamespaceHasSegment(entity, "Relationships") || NamespaceHasSegment(entity, "Templates")))
            .ToList();

        var foreignKeyOffenders = entities
            .SelectMany(entity => entity.GetForeignKeys())
            .Where(fk => IsQuran(fk.PrincipalEntityType))
            .Select(Describe)
            .ToList();

        foreignKeyOffenders.Should().BeEmpty(
            "030 (CategoryRelationship / DoorTemplate aggregate) must introduce no Abwab->Quran foreign key "
            + "(vacuous until those entities land); offenders: " + string.Join("; ", foreignKeyOffenders));

        var excerptOffenders = entities
            .SelectMany(entity => entity.GetProperties().Select(property => (entity, property)))
            .Where(pair => pair.property.Name == "RepresentativeQuranExcerpt" && pair.property.ClrType != typeof(string))
            .Select(pair => pair.entity.ShortName())
            .ToList();

        excerptOffenders.Should().BeEmpty(
            "TemplateNode.RepresentativeQuranExcerpt (data-model.md) must stay a plain string with no ayah "
            + "validation; offenders: " + string.Join(", ", excerptOffenders));
    }

    private IModel DesignTimeModel()
    {
        using var context = new QuranDashboardDbContext(
            new DbContextOptionsBuilder<QuranDashboardDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        return context.GetService<IDesignTimeModel>().Model;
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
