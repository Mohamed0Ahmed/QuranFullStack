using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Abwab.Tree;

namespace QuranDashboard.Tests.Abwab.Reads;

// The Story 1 checkpoint asserted zero mutation surface anywhere under Abwab; US3 (T050-T062)
// intentionally activates the section/category/protection writers behind explicit permission-gated
// endpoints in their own controllers. The one invariant that survives US3: AbwabTreeController itself
// stays read/search/snapshot only — mutations never hide behind the tree endpoint.
public sealed class NoMutationSurfaceTests
{
    [Fact]
    public void AbwabTreeController_OnlyExposesReadActions()
    {
        var actions = typeof(AbwabTreeController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        actions.Should().NotBeEmpty();
        actions.Should().OnlyContain(
            method => method.GetCustomAttribute<HttpGetAttribute>() != null,
            "the tree controller exposes read/search/snapshot only; mutations live in the dedicated Sections/Categories/Protection controllers");
    }
}
