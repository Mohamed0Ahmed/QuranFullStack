using QuranDashboard.Application.Abstractions.Security.Permissions;

namespace QuranDashboard.Tests.Api.Access;

public sealed class AbwabPermissionCatalogueTests
{
    [Fact]
    public void Catalogue_HasTheAcceptedTwentyOneCodesInStableOrder()
    {
        AbwabPermissionCatalogue.All.Select(permission => permission.Code).Should().Equal(
            "abwab.doors.create",
            "abwab.doors.edit",
            "abwab.doors.move",
            "abwab.doors.reorder",
            "abwab.doors.archive",
            "abwab.doors.restore",
            "abwab.sections.create",
            "abwab.sections.edit",
            "abwab.sections.reorder",
            "abwab.sections.delete",
            "abwab.relations.create",
            "abwab.relations.delete",
            "abwab.templates.create",
            "abwab.templates.delete",
            "abwab.templates.apply",
            "abwab.template_nodes.create",
            "abwab.template_nodes.edit",
            "abwab.template_nodes.reorder",
            "abwab.template_nodes.delete",
            "abwab.inclusions.create",
            "abwab.inclusions.delete");
    }

    [Fact]
    public void CatalogueMetadata_IsCompleteAndCodeDefinedGroupsAreNotPersistedAuthority()
    {
        AbwabPermissionCatalogue.All.Should().HaveCount(21);
        AbwabPermissionCatalogue.All.Select(permission => permission.Code)
            .Should().OnlyHaveUniqueItems();
        AbwabPermissionCatalogue.All.Should().OnlyContain(permission =>
            !string.IsNullOrWhiteSpace(permission.ArabicLabel)
            && !string.IsNullOrWhiteSpace(permission.EnglishDescription)
            && !string.IsNullOrWhiteSpace(permission.GroupArabicLabel)
            && permission.DisplayOrder > 0);
        AbwabPermissionCatalogue.All.Select(permission => permission.DisplayOrder)
            .Should().Equal(Enumerable.Range(1, 21));
    }

    [Fact]
    public void GroupLabels_AreArabicAndAgreeWithinEachGroup()
    {
        AbwabPermissionCatalogue.All
            .GroupBy(permission => permission.GroupDisplayOrder)
            .Select(group => group.Select(permission => permission.GroupArabicLabel).Distinct().Single())
            .Should().Equal("الأبواب", "الأقسام", "العلاقات", "القوالب", "عناصر القوالب", "إدارة مصادر الباب");
    }

    [Fact]
    public void EndpointConstants_ResolveToTheCatalogueIdentity()
    {
        AbwabPermissions.Doors.Create.Should().Be("abwab.doors.create");
        AbwabPermissions.TemplateNodes.Delete.Should().Be("abwab.template_nodes.delete");
        AbwabPermissions.Inclusions.Create.Should().Be("abwab.inclusions.create");
        AbwabPermissions.Inclusions.Delete.Should().Be("abwab.inclusions.delete");
        AbwabPermissionCatalogue.All.Select(permission => permission.Code)
            .Should().Contain(AbwabPermissions.Doors.Create)
            .And.Contain(AbwabPermissions.TemplateNodes.Delete)
            .And.Contain(AbwabPermissions.Inclusions.Create)
            .And.Contain(AbwabPermissions.Inclusions.Delete);
    }
}
