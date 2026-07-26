# Smoke — real-run pipeline harness

Boots the REAL API composition and drives every registered route through the full MVC pipeline
(routing, authorization, model binding, serialization). Exists because a Critical model-binding bug
(`[property: Required]` on positional records → every template POST 500) passed 2,000+ unit tests:
only the real pipeline can catch that class of defect.

## What runs where

- `Guards/` — host integrity: environment is `Testing`, active connection string is the test
  container's, authentication scheme inventory is exactly `["Bearer"]` with the JwtBearer handler,
  health serves through BOTH hosts, Kestrel listens on a real loopback http port.
- `Personas/` — persona behavior through the real pipeline (401 envelope, DB-backed 403/200,
  SystemOwner policy, provisioning of a fresh sub through the fake profile source).
- `Pipeline/` — the route catalog (`SmokeRouteCatalog`), the coverage parity gate
  (`SmokeCoverageParityTests`), and the assertion-contract passes (`PipelineSmokeTests`).
- `Data/` — skip-gated real-data reads for the legacy Quran surfaces over the restored canonical dump.

Run: `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke"` (see
`TESTING_STRATEGY.md` §3 "Smoke suite"). Selection is namespace-based; no traits.

## Invariants (do not break)

- **Environment is `Testing`, never Development/Production.** Only base `appsettings.json` loads;
  every needed key is injected in-memory by `SmokeHostConfigurator`. This is what keeps a smoke host
  from ever reaching a real database or the real Logto tenant.
- **Real JwtBearer, behavioral parity.** No replacement auth scheme. Fixtures only
  `PostConfigure<JwtBearerOptions>` (test issuer + RSA key) and mint `sub`-only tokens via
  `TestJwtTokens`. Personas get DB rows through the real security handlers (`SmokeSeed`), never
  injected role claims. `IExternalUserProfileSource` is always faked — no outbound Logto calls.
- **Own collections.** `SmokeCollection` and `QuranDataSmokeCollection` own their containers. Never
  join `AbwabDbCollection` — its classes full-reset the substrate per test and would wipe smoke state.
- **Seed-once tolerance contract.** `SmokeSeed` seeds once per fixture; pipeline passes tolerate
  domain 400/404/409 from accumulated writes but NEVER tolerate 401/403 (when authorized) or any 5xx.
  Destructive cases target the `Expendable*` seed entities only.
- **Every route needs a catalog entry.** `SmokeCoverageParityTests` compares `SmokeRouteCatalog`
  against the live `ApiExplorer` endpoint table in both directions and needs no DB — it always runs,
  including CI. Add/rename an endpoint ⇒ update the catalog in the same change.
- **Quran data comes only from the verified canonical dump.** `resources/db-dumps/quran-canonical/`
  (dump + sha256/row-count manifest) is produced by `Backend/scripts/create-smoke-dump` from a local
  DB seeded by the documented DataImporter chain — a derived cache of the canonical import, never
  synthetic, never committed (`resources/` is gitignored). Missing dump ⇒ data-smoke SKIPS (normal in
  CI); present-but-corrupt/stale dump (sha256 or migration mismatch) ⇒ FAIL LOUD, never skip.
- **Restore mechanics.** The data fixture migrates first (schema incl. `__EFMigrationsHistory` comes
  from EF migrations; the dump is data-only), then runs HOST `pg_restore --data-only
  --disable-triggers` against the container's mapped port. The data container runs `postgres:18`
  because the archive carries the dumping client's PG17+ SET preamble; the pipeline substrate stays
  on `postgres:16-alpine`.

## Related

- Strategy tier + commands: `../../../../TESTING_STRATEGY.md` §3
- Auth wiring this harness exercises: `../../../api/QuranDashboard.Api/Authentication/README.md`
- Parity enumeration source: `../Abwab/Ci/ApiContractSources.cs`
