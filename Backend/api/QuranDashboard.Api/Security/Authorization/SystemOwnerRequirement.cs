using Microsoft.AspNetCore.Authorization;

namespace QuranDashboard.Api.Security.Authorization;

// Marks a policy as requiring an authenticated, active System Owner (resolved from the membership table —
// no email/role/runtime fallback).
public sealed class SystemOwnerRequirement : IAuthorizationRequirement;
