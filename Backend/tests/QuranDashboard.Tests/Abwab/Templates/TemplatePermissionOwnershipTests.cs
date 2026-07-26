using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Abwab.Templates;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Tests.Abwab.Templates;

// §5.2 is FROZEN: template.add grants nothing beyond creating the aggregate, every node/alias
// internal is template.edit, lifecycle is template.delete/restore, application is template.apply
// alone. Backend enforcement is authoritative — frontend hiding authorizes nothing, so the matrix is
// asserted on the endpoints themselves.
public sealed class TemplatePermissionOwnershipTests
{
    private static readonly IReadOnlyDictionary<string, string> ExpectedPolicyByAction = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [nameof(TemplatesController.GetTemplates)] = PermissionCatalogue.TemplateView,
        [nameof(TemplatesController.GetTemplate)] = PermissionCatalogue.TemplateView,
        [nameof(TemplatesController.GetHistory)] = PermissionCatalogue.TemplateView,
        [nameof(TemplatesController.Add)] = PermissionCatalogue.TemplateAdd,
        [nameof(TemplatesController.Edit)] = PermissionCatalogue.TemplateEdit,
        [nameof(TemplatesController.Delete)] = PermissionCatalogue.TemplateDelete,
        [nameof(TemplatesController.Restore)] = PermissionCatalogue.TemplateRestore,
        [nameof(TemplatesController.AddNode)] = PermissionCatalogue.TemplateEdit,
        [nameof(TemplatesController.EditNode)] = PermissionCatalogue.TemplateEdit,
        [nameof(TemplatesController.ReparentNode)] = PermissionCatalogue.TemplateEdit,
        [nameof(TemplatesController.ReorderNodes)] = PermissionCatalogue.TemplateEdit,
        [nameof(TemplatesController.RemoveNode)] = PermissionCatalogue.TemplateEdit,
        [nameof(TemplatesController.AddNodeAlias)] = PermissionCatalogue.TemplateEdit,
        [nameof(TemplatesController.EditNodeAlias)] = PermissionCatalogue.TemplateEdit,
        [nameof(TemplatesController.RemoveNodeAlias)] = PermissionCatalogue.TemplateEdit,
        [nameof(TemplatesController.RestoreNodeAlias)] = PermissionCatalogue.TemplateEdit,
        [nameof(TemplatesController.Apply)] = PermissionCatalogue.TemplateApply,
    };

    [Fact]
    public void EveryTemplateEndpoint_CarriesExactlyTheFrozenOwningPermission()
    {
        var actual = TemplateActions().ToDictionary(
            action => action.Name,
            action => action.GetCustomAttributes<AuthorizeAttribute>().Select(a => a.Policy).ToList(),
            StringComparer.Ordinal);

        // Set equality, not containment: a NEW endpoint the matrix does not name must fail here
        // rather than sail through carrying whatever policy its author happened to pick.
        actual.Keys.Should().BeEquivalentTo(ExpectedPolicyByAction.Keys,
            "the frozen §5.2 matrix must name every template endpoint, and no other");

        foreach (var (action, expectedPolicy) in ExpectedPolicyByAction)
        {
            actual[action].Should().BeEquivalentTo([expectedPolicy],
                $"{action} must require exactly {expectedPolicy} — no borrowed verb, no second policy");
        }
    }

    [Fact]
    public void NoTemplateEndpoint_IsLeftUnauthorized()
    {
        var unauthorized = TemplateActions()
            .Where(action => !action.GetCustomAttributes<AuthorizeAttribute>().Any())
            .Select(action => action.Name)
            .ToList();

        unauthorized.Should().BeEmpty("an unauthorized template endpoint would let any partial grant reach it");
    }

    [Fact]
    public void TheTemplateCatalogueCodes_AreExactlyTheSixFrozenSection52Codes()
    {
        PermissionCatalogue.Codes
            .Where(code => code.StartsWith("template.", StringComparison.Ordinal))
            .Should().BeEquivalentTo([
                "template.view",
                "template.add",
                "template.edit",
                "template.delete",
                "template.restore",
                "template.apply",
            ], "no synonym and no child-CRUD permission may be invented for aggregate subresources");
    }

    [Fact]
    public void NoEndpointOtherThanAggregateCreation_BorrowsTemplateAdd()
    {
        var borrowing = TemplateActions()
            .Where(action => action.Name != nameof(TemplatesController.Add))
            .Where(action => action.GetCustomAttributes<AuthorizeAttribute>().Any(a => a.Policy == PermissionCatalogue.TemplateAdd))
            .Select(action => action.Name)
            .ToList();

        borrowing.Should().BeEmpty("template.add creates ONLY the aggregate (§5.2)");
    }

    private static IEnumerable<MethodInfo> TemplateActions() =>
        typeof(TemplatesController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
}
