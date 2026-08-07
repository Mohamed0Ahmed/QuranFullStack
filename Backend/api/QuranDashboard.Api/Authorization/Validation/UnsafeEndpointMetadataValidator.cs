using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Application.Abstractions.Security.Permissions;

namespace QuranDashboard.Api.Authorization.Validation;

public sealed class UnsafeEndpointMetadataValidator
{
    private static readonly HashSet<string> UnsafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete,
    };

    public void Validate(IEnumerable<Endpoint> endpoints)
    {
        var failures = endpoints
            .OfType<RouteEndpoint>()
            .Select(ValidateEndpoint)
            .Where(result => !result.IsValid)
            .Select(result => result.Error!)
            .ToArray();

        if (failures.Length != 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        }
    }

    public static UnsafeEndpointMetadataValidationResult ValidateEndpoint(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
        var unsafeMethod = methods is null or { Count: 0 }
            ? "ANY"
            : methods.FirstOrDefault(method => UnsafeMethods.Contains(method));
        if (unsafeMethod is null)
        {
            return UnsafeEndpointMetadataValidationResult.Valid;
        }

        var route = endpoint.RoutePattern.RawText ?? string.Empty;
        var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?.DisplayName
            ?? endpoint.DisplayName
            ?? "unknown";
        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return Invalid(route, unsafeMethod, action, "declares anonymous access");
        }

        var permissions = endpoint.Metadata.OfType<RequirePermissionAttribute>().ToArray();
        var owners = endpoint.Metadata.OfType<RequireOwnerAttribute>().ToArray();
        if (permissions.Length == 0 && owners.Length == 0)
        {
            var hasAuthorizeData = endpoint.Metadata.OfType<IAuthorizeData>().Any();
            return Invalid(
                route,
                unsafeMethod,
                action,
                hasAuthorizeData
                    ? "has authenticated-only metadata without a permission or Owner classification"
                    : "has no permission or Owner classification");
        }

        if (permissions.Length != 0 && owners.Length != 0)
        {
            return Invalid(route, unsafeMethod, action, "declares both permission and Owner classifications");
        }

        if (endpoint.Metadata.OfType<IAuthorizeData>().Any(metadata =>
                metadata is not RequirePermissionAttribute and not RequireOwnerAttribute))
        {
            return Invalid(
                route,
                unsafeMethod,
                action,
                "declares additional authorization metadata outside its permission or Owner classification");
        }

        if (permissions.Length > 1)
        {
            return Invalid(route, unsafeMethod, action, "declares multiple permission classifications");
        }

        if (owners.Length > 1)
        {
            return Invalid(route, unsafeMethod, action, "declares multiple Owner classifications");
        }

        if (permissions.Length == 1 && !AbwabPermissionCatalogue.All.Any(permission =>
                string.Equals(permission.Code, permissions[0].PermissionCode, StringComparison.Ordinal)))
        {
            return Invalid(route, unsafeMethod, action, "declares an unknown permission code");
        }

        return UnsafeEndpointMetadataValidationResult.Valid;
    }

    private static UnsafeEndpointMetadataValidationResult Invalid(
        string route,
        string method,
        string action,
        string reason) => new(
        false,
        $"Unsafe endpoint '{method} {route}' action '{action}' {reason}.");
}

public sealed record UnsafeEndpointMetadataValidationResult(bool IsValid, string? Error)
{
    public static UnsafeEndpointMetadataValidationResult Valid { get; } = new(true, null);
}
