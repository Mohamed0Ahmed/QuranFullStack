using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.Api.Access;

public sealed class AccessSchemaModelTests
{
    private readonly QuranDashboardDbContext _db = new(
        new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options);

    [Fact]
    public void AccessModel_ContainsOnlyThePlannedPermissionGrantAndAuditEntities()
    {
        _db.Model.FindEntityType(typeof(Permission)).Should().NotBeNull();
        _db.Model.FindEntityType(typeof(UserPermission)).Should().NotBeNull();
        _db.Model.FindEntityType(typeof(AccessAuditEvent)).Should().NotBeNull();
        _db.Model.GetEntityTypes().Select(entity => entity.GetTableName())
            .Should().NotContain("permission_groups");
        _db.Model.GetEntityTypes().Select(entity => entity.GetTableName())
            .Should().NotContain("role_permissions");
    }

    [Fact]
    public void UserModel_UsesNormalizedIdentityAndXminConcurrency()
    {
        var entity = _db.Model.FindEntityType(typeof(User))!;
        var normalizedEmail = entity.FindProperty(nameof(User.NormalizedEmail))!;
        var version = entity.FindProperty(nameof(User.Version))!;

        normalizedEmail.IsNullable.Should().BeFalse();
        version.IsConcurrencyToken.Should().BeTrue();
        version.ValueGenerated.Should().Be(ValueGenerated.OnAddOrUpdate);

        entity.GetIndexes().Should().Contain(index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(User.NormalizedEmail) }));
        entity.GetIndexes().Should().Contain(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(User.Status), nameof(User.Id) }));
    }

    [Fact]
    public void PermissionModel_UsesImmutableCodeAndCatalogueMetadata()
    {
        var entity = _db.Model.FindEntityType(typeof(Permission))!;

        entity.FindProperty(nameof(Permission.Code))!.IsNullable.Should().BeFalse();
        entity.FindProperty(nameof(Permission.ArabicLabel))!.IsNullable.Should().BeFalse();
        entity.FindProperty(nameof(Permission.EnglishDescription))!.IsNullable.Should().BeFalse();
        entity.FindProperty(nameof(Permission.DisplayOrder))!.IsNullable.Should().BeFalse();
        entity.FindProperty(nameof(Permission.RetiredAtUtc))!.IsNullable.Should().BeTrue();
        entity.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Single().Name == nameof(Permission.Code));
    }

    [Fact]
    public void UserPermissionModel_UsesCompositeKeyAndRestrictiveForeignKeys()
    {
        var entity = _db.Model.FindEntityType(typeof(UserPermission))!;

        entity.FindPrimaryKey()!.Properties.Select(property => property.Name)
            .Should().Equal(nameof(UserPermission.UserId), nameof(UserPermission.PermissionId));
        entity.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(UserPermission.UserId));
        entity.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(UserPermission.PermissionId));
        entity.GetForeignKeys().Should().OnlyContain(foreignKey =>
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public void AuditModel_UsesRequiredIndexesAndRestrictiveForeignKeys()
    {
        var entity = _db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(AccessAuditEvent))!;

        entity.FindProperty(nameof(AccessAuditEvent.Id))!.ClrType.Should().Be(typeof(long));
        entity.FindProperty(nameof(AccessAuditEvent.ActorUserId))!.IsNullable.Should().BeTrue();
        entity.FindProperty(nameof(AccessAuditEvent.TargetUserId))!.IsNullable.Should().BeFalse();
        var occurredAtIndex = entity.GetIndexes().Single(index => index.Properties.Select(property => property.Name)
            .SequenceEqual(new[] { nameof(AccessAuditEvent.OccurredAtUtc), nameof(AccessAuditEvent.Id) }));
        occurredAtIndex.IsDescending.Should().BeEmpty();
        entity.GetIndexes().Should().Contain(index => index.Properties.Select(property => property.Name)
            .SequenceEqual(new[] { nameof(AccessAuditEvent.TargetUserId), nameof(AccessAuditEvent.OccurredAtUtc), nameof(AccessAuditEvent.Id) })
            && index.IsDescending!.SequenceEqual(new[] { false, true, true }));
        entity.GetForeignKeys().Should().OnlyContain(foreignKey =>
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
    }
}
