namespace QuranDashboard.Tests.Smoke;

public sealed class SmokeRouteAccessContractTests
{
    [Fact]
    public void AccessKinds_RepresentEveryPlannedRouteClassification()
    {
        SmokeRouteAccess.Public.Kind.Should().Be(SmokeRouteAccessKind.Public);
        SmokeRouteAccess.AuthenticatedOnly.Kind.Should().Be(SmokeRouteAccessKind.AuthenticatedOnly);
        SmokeRouteAccess.OwnerOnly.Kind.Should().Be(SmokeRouteAccessKind.OwnerOnly);

        var permission = SmokeRouteAccess.Permission("abwab.doors.create");

        permission.Kind.Should().Be(SmokeRouteAccessKind.Permission);
        permission.PermissionCode.Should().Be("abwab.doors.create");
    }

    [Fact]
    public void PermissionClassification_RequiresANonBlankCode()
    {
        var act = () => SmokeRouteAccess.Permission("  ");

        act.Should().Throw<ArgumentException>();
    }
}
