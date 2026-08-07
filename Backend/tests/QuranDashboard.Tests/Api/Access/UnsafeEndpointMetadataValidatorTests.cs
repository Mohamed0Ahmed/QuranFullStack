using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Authorization.Owner;
using QuranDashboard.Api.Authorization.Permissions;
using QuranDashboard.Api.Authorization.Validation;
using QuranDashboard.Application.Abstractions.Security.Permissions;

namespace QuranDashboard.Tests.Api.Access;

public sealed class UnsafeEndpointMetadataValidatorTests
{
    public static TheoryData<string, object[]> InvalidUnsafeMetadata => new()
    {
        { "has no permission or Owner classification", [] },
        { "unknown permission code", [new RequirePermissionAttribute("abwab.unknown.create")] },
        {
            "multiple permission classifications",
            [
                new RequirePermissionAttribute(AbwabPermissions.Doors.Create),
                new RequirePermissionAttribute(AbwabPermissions.Doors.Edit),
            ]
        },
        {
            "both permission and Owner classifications",
            [
                new RequirePermissionAttribute(AbwabPermissions.Doors.Create),
                new RequireOwnerAttribute(),
            ]
        },
        {
            "additional authorization metadata",
            [
                new AuthorizeAttribute(),
                new RequirePermissionAttribute(AbwabPermissions.Doors.Create),
            ]
        },
        { "multiple Owner classifications", [new RequireOwnerAttribute(), new RequireOwnerAttribute()] },
        {
            "declares anonymous access",
            [
                new AllowAnonymousAttribute(),
                new RequirePermissionAttribute(AbwabPermissions.Doors.Create),
            ]
        },
        { "authenticated-only metadata", [new AuthorizeAttribute()] },
    };

    [Theory]
    [MemberData(nameof(InvalidUnsafeMetadata))]
    public void Validate_RejectsEveryInvalidUnsafeMetadataShape(string expectedReason, object[] metadata)
    {
        var endpoint = CreateEndpoint(HttpMethods.Post, metadata);

        var validate = () => new UnsafeEndpointMetadataValidator().Validate([endpoint]);

        validate.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain(expectedReason);
    }

    [Fact]
    public void Validate_AcceptsOneKnownPermissionOrOwnerClassification()
    {
        var validator = new UnsafeEndpointMetadataValidator();
        var permissionEndpoint = CreateEndpoint(
            HttpMethods.Post,
            [new RequirePermissionAttribute(AbwabPermissions.Doors.Create)]);
        var ownerEndpoint = CreateEndpoint(HttpMethods.Delete, [new RequireOwnerAttribute()]);

        var validate = () => validator.Validate([permissionEndpoint, ownerEndpoint]);

        validate.Should().NotThrow();
    }

    [Fact]
    public void Validate_LeavesPublicGetWithoutAuthorizationMetadataUntouched()
    {
        var endpoint = CreateEndpoint(HttpMethods.Get, []);

        var validate = () => new UnsafeEndpointMetadataValidator().Validate([endpoint]);

        validate.Should().NotThrow();
    }

    [Fact]
    public void MetadataAttributes_ProduceTheExactAuthorizationRequirements()
    {
        var permissionRequirements = new RequirePermissionAttribute(AbwabPermissions.Doors.Create)
            .GetRequirements()
            .Should()
            .ContainSingle()
            .Which;
        var ownerRequirements = new RequireOwnerAttribute()
            .GetRequirements()
            .Should()
            .ContainSingle()
            .Which;

        permissionRequirements.Should().BeOfType<PermissionRequirement>()
            .Which.PermissionCode.Should().Be(AbwabPermissions.Doors.Create);
        ownerRequirements.Should().BeSameAs(OwnerOnlyRequirement.Instance);
    }

    private static RouteEndpoint CreateEndpoint(string method, IEnumerable<object> metadata)
    {
        var builder = new RouteEndpointBuilder(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/test"),
            order: 0);
        builder.Metadata.Add(new HttpMethodMetadata([method]));
        foreach (var item in metadata)
        {
            builder.Metadata.Add(item);
        }

        return (RouteEndpoint)builder.Build();
    }
}
