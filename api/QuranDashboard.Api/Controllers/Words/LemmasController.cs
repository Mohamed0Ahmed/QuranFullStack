using Microsoft.AspNetCore.Mvc;

namespace QuranDashboard.Api.Controllers.Words;

/// <summary>
/// Lemmas Explorer (Feature 016) read-only endpoints under the existing Words
/// area. Route base: <c>api/words/lemmas</c>. Sibling of Feature 015
/// <c>RootsController</c>. Story-phase actions are added incrementally;
/// US1/US3/US4/US5/US6 handlers are injected when their actions land. This
/// Phase 2 shell registers the route base only.
/// </summary>
[ApiController]
[Route("api/words/lemmas")]
public sealed class LemmasController : ControllerBase
{
}
