using Microsoft.AspNetCore.Mvc;

namespace QuranDashboard.Api.Controllers.Words;

/// <summary>
/// Roots Explorer (Feature 015) read-only endpoints under the existing Words
/// area. Route base: <c>api/words/roots</c>. Mirrors Feature 014
/// <c>UniqueWordsController</c>.
/// </summary>
/// <remarks>
/// Foundational skeleton: handler dependencies and action methods are added by
/// each user story (US1 list/summary T027, US2 ayahs T038, US3 words T047,
/// US4 surahs T056, US5 lemmas/stems T064). Kept route-attributed and empty so
/// the composition compiles before any story lands.
/// </remarks>
[ApiController]
[Route("api/words/roots")]
public sealed class RootsController : ControllerBase
{
}
