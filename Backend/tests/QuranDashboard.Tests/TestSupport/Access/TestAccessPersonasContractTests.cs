using QuranDashboard.Tests.Smoke;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.Api.Access;
using Microsoft.IdentityModel.JsonWebTokens;

namespace QuranDashboard.Tests.TestSupport.Access;

public sealed class TestAccessPersonasContractTests
{
    [Fact]
    public void PersonaCatalog_CoversEveryPlannedAuthorizationState()
    {
        TestAccessPersonas.All.Select(persona => persona.Key).Should().Equal(
            "Anonymous",
            "AuthenticatedUnknown",
            "Pending",
            "Disabled",
            "ReadOnly",
            "ExactPermission",
            "NeighboringPermission",
            "Owner",
            "DisabledOwner",
            "ClaimSmuggling");

        SmokePersonas.All.Should().BeEquivalentTo(
            Enum.GetValues<SmokePersona>(),
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public void ClaimSmugglingPersona_CarriesClaimsSeparateFromLocalState()
    {
        var persona = TestAccessPersonas.For("ClaimSmuggling");

        persona.Status.Should().Be(UserStatus.Active);
        persona.RoleName.Should().BeNull();
        persona.PermissionCode.Should().BeNull();
        persona.TokenClaims.Should().ContainKeys("role", "permission");
    }

    [Fact]
    public void ClaimSmugglingPersona_CanMintClaimsWithoutChangingItsLocalDefinition()
    {
        var persona = TestAccessPersonas.For("ClaimSmuggling");
        var token = TestJwtTokens.Mint(persona.Sub!, additionalClaims: persona.TokenClaims);
        var payload = new JsonWebToken(token);

        payload.GetClaim("role")!.ToString().Should().Contain(RoleNames.Owner);
        payload.GetClaim("permission")!.ToString().Should().Contain("abwab.doors.create");
        persona.Status.Should().Be(UserStatus.Active);
        persona.RoleName.Should().BeNull();
    }

    [Fact]
    public void LocalPersonaBuilder_PreservesStatusRoleAndIdentityInputs()
    {
        var persona = TestAccessPersonas.For("DisabledOwner");
        var user = persona.BuildUser(roleId: 1, now: DateTimeOffset.Parse("2026-08-05T00:00:00Z"));

        user.LogtoSub.Should().Be(persona.Sub);
        user.Email.Should().Be("smoke-disabled-owner@example.test");
        user.Status.Should().Be(UserStatus.Disabled);
        user.RoleId.Should().Be(1);
        user.CreatedAtUtc.Should().Be(user.UpdatedAtUtc);
    }
}
