using Microsoft.AspNetCore.Authorization;

namespace QuranDashboard.Api.Security.Authorization;

// Requires the caller to hold a specific effective permission code. One policy per catalogue code is
// registered (the "policy" layer of the 5-catalogue parity check).
public sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}
