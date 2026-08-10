# Test Guard — C# / .NET Patterns

Concrete applications of the nine rules for .NET projects (xUnit/NUnit/MSTest, ASP.NET Core, EF Core, PostgreSQL). Read this when reviewing or writing Backend C# tests.

## Framework choice

- Prefer **xUnit** when the project has no existing choice: de-facto .NET standard, `[Theory]` + `[InlineData]`/`[MemberData]`/`[ClassData]` for data-driven tests, parallel by default, DI via the test-class constructor plus `IClassFixture<T>`/`ICollectionFixture<T>`.
- If the solution already standardizes on **NUnit** (`[TestCase]`/`[TestCaseSource]`) or **MSTest** (`[DataRow]`/`[DynamicData]`), follow it — never introduce a second framework into a solution that already chose one.
- Use one assertion approach consistently (the framework's `Assert`, or FluentAssertions if the project already uses it). Don't mix.

## Rule 1 + Rule 7: Behavior, not call chains or the framework

- Assert what a handler/endpoint/service **returns or changes** from the caller's perspective: the returned `ApiResponse<T>` (`IsSuccess` / `Message` / `Data` / `Errors`), the persisted row, the emitted domain event.
- Do **not** assert that a controller called `_handler.Handle(...)`, that a handler called `_repository.GetAsync(...)`, or that an internal helper was invoked. Those are internal call chains (Rule 1) — they break on refactor and catch nothing.
- Do **not** re-test ASP.NET Core / EF Core framework guarantees (Rule 7): that EF Core actually saves, that the model binder binds, that `[ApiController]` returns 400 on invalid `ModelState`, that `[Authorize]` blocks anonymous requests, that the DI container resolves. Test **your** logic on top — your validation rule, your mapping, your business invariant. If your code *adds* behavior on top of a framework guarantee (e.g. you reshape the 400 into an `ApiResponse` with specific `Errors`), test that added behavior, not the framework's part.
- Smell: a test that would still pass if you deleted all the project's logic and kept only ASP.NET Core / EF Core defaults.

## Rule 2: Mock only true boundaries

Justified mock targets in .NET:

- **Outbound HTTP** to external services: inject `HttpClient` via `IHttpClientFactory` and stub a fake `HttpMessageHandler` (or WireMock.Net) — mock at the transport boundary, not your own typed client.
- **Third-party SDKs** (payment, storage, email) behind your own interface.
- **Clock/time:** depend on `TimeProvider` (.NET 8+) or an `IClock` abstraction; never call `DateTime.Now`/`DateTimeOffset.Now` directly in code under test.
- **Randomness / GUID generation:** inject the generator.
- **Filesystem outside controlled temp paths:** abstract via `System.IO.Abstractions`, or write to a per-test temp directory.

Do **not** mock:

- **EF Core `DbContext`, `DbSet<T>`, `IQueryable`/LINQ query providers, or EF internals** to "unit test a query." A mocked provider skips the real LINQ→SQL translation, so the test passes while the real query throws or returns the wrong rows. Test queries against a real database (Rule 9).
- **Domain entities, value objects, aggregates** — construct real instances (Rule 8).
- **DTOs / API request+response contracts / application state models** — construct real instances (Rule 8).
- Your own application services/handlers just to isolate a "unit" — if wiring is painful, that's design feedback; add a builder/factory, don't fake the collaborator.

When you mock a boundary, assert what your code **does with the response**, not that the mock received specific arguments.

## Rule 8: Entities, value objects, DTOs are real, never mocked

```csharp
// Wrong — a mock hides field-name and validation bugs
var ayah = new Mock<Ayah>();           // don't

// Right — construct the real type; its invariants and validation run
var ayah = new Ayah(surah: 2, number: 255, text: syntheticArabic);
```

If construction needs many fields, add a small test data **builder** (`AyahBuilder`) or Object Mother — not a mock. (See Quranic data safety below for the `text` value.)

## Rule 9 + ASP.NET Core: integration tests for API and persistence

Prefer integration tests where the real bugs live: the HTTP boundary and persistence.

**API boundary — `WebApplicationFactory<TEntryPoint>`** (Microsoft.AspNetCore.Mvc.Testing):

- Boot the app in-memory and exercise the **real** pipeline: routing, model binding, filters, middleware, and the `ApiResponse<T>` envelope end to end.
- In `ConfigureTestServices`, override **only true boundaries** (swap external HTTP/LLM clients for fakes); keep the real pipeline and the real database.
- Assert on the HTTP status code **plus** the deserialized `ApiResponse<T>` — `IsSuccess`, `Message`, `Data`, `Errors` — matching `API_GUIDELINES.md` §5 / `Contracts/ApiResponse.cs` (indexed by `docs/contracts/response-envelope.md`). Do not assert internal call chains.

**Database — real PostgreSQL via Testcontainers:**

- When a **query, migration, mapping, constraint, or persistence behavior is the subject**, run against real Postgres, applying the real EF Core migrations, seeding via fixtures, and isolating each test.
- **In this repository a fixture must not construct its own `PostgreSqlContainer`** — fixtures lease a database from the shared, project-owned runtime. The fixture/serialization rules are owned by `Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/README.md`; read the README before writing or reviewing a database fixture, and treat a new container start as a finding.
- Mocking the `DbContext` here tests nothing (Rules 2 and 9).

**SQLite fallback — acceptable / not acceptable:**

- **Acceptable** only for **provider-independent** logic: persistence is an incidental side effect, the assertion is about your behavior (not Postgres semantics), and the queries use only portable features. The EF Core **SQLite** provider runs real SQL and migrations, so it is a reasonable lightweight option for simple, portable CRUD round-trips.
- **Not acceptable** when behavior depends on **PostgreSQL-specific** semantics: provider-specific SQL or migrations, JSONB, full-text search, collation / case-insensitivity (this matters for Arabic text), `citext`, array columns, `ILIKE`, Postgres functions, indexing behavior, concurrency tokens, or any query semantics that differ across providers. SQLite behaves differently and gives false confidence — use Testcontainers Postgres for these.
- **Never** use the **EF Core In-Memory provider** for query correctness: it is not a relational store, ignores constraints and SQL translation, and is explicitly not recommended for testing query behavior. At most use it for trivial non-query wiring.

## Rule 3: Data-driven variants

```csharp
[Theory]
[InlineData("Hello World", "hello-world")]
[InlineData("  padded  ", "padded")]
public void Slugify_normalizes_input(string raw, string expected)
    => Assert.Equal(expected, Slugify(raw));
```

Use `[MemberData]`/`[ClassData]` for richer cases. Merge tests that share setup and differ only by input/output. Keep tests separate when setup, assertions, or boundary mocks genuinely differ.

## Rule 5: Name tests for the scenario

`Method_Scenario_ExpectedOutcome` or a requirement-style sentence, e.g. `GetGate_WhenGateMissing_ReturnsFailureApiResponse` — not `TestGetGate2`.

## Rule 6: Production regression tests are sacred

Reproduce a real bug, reference the incident (issue ID / date) in the test name or a comment, and never delete it. Exempt from Rule 4.

## Prefer integration tests for

- **API boundary behavior** (`WebApplicationFactory` + `ApiResponse<T>` assertions).
- **Persistence behavior** (queries, migrations, mappings, constraints) against real Postgres.
- **Importers / processors / generators** — assert their reports (totals, missing, duplicates, warnings, validation result).
- **Validation and mapping of Quranic data** — assert the safety guarantees, not just the happy path.

## Quranic data safety in tests (overrides convenience)

The canonical rules are `CODING_PRINCIPLES.md` §10, which applies to test data in full:
synthetic-only unless loaded from a traceable fixture source, obvious placeholders, never
hand-typed "real" scripture. The .NET application of it: import/persistence tests for
Quranic data assert the safety guarantees themselves — totals, missing records, duplicates,
validation result — not just the happy path.

## .NET-specific smells

- `Mock<DbSet<T>>` / `Mock<DbContext>` / mocked `IQueryable` provider — Rule 2/9 violation; use a real test database.
- `Verify(x => x.Handle(...), Times.Once)` on internal handlers/repositories — Rule 1 violation; assert the observable result instead.
- `Mock<SomeEntity>` / `Mock<SomeDto>` — Rule 8 violation; construct the real object or use a builder.
- EF Core In-Memory provider used to test query correctness — false confidence.
- A `[Fact]` per input value where a `[Theory]` fits — Rule 3 bloat.
- Tests asserting `[ApiController]` 400 behavior or `[Authorize]` redirects with no added behavior — Rule 7 framework guarantees.
