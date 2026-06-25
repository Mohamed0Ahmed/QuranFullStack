using Microsoft.AspNetCore.Mvc;

namespace QuranDashboard.Api.Controllers.Words;

/// <summary>
/// Stems Explorer (Feature 016) read-only endpoints under the existing Words
/// area. Route base: <c>api/words/stems</c>. Sibling of Feature 015
/// <c>RootsController</c>. Story-phase actions are added incrementally;
/// US2/US3/US4/US5/US6 handlers are injected when their actions land. This
/// Phase 2 shell registers the route base only.
/// </summary>
[ApiController]
[Route("api/words/stems")]
public sealed class StemsController : ControllerBase
{
}
