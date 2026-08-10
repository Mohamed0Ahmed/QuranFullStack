# Testing Constitution

This file is the repository's single testing-policy authority. Test READMEs own only the commands,
fixtures, and operational details for their area.

1. **The operative default is no new automated test.** The burden of proof is on adding a test, not
   on omitting one, and every exception requires explicit owner approval.
2. **Do not create a test per component, service, endpoint, or DTO.**
3. **There is no coverage-percentage target.** Coverage is not measured and may not be cited.
4. **Frontend `*.spec.ts` is prohibited.** The prohibition is absolute and machine-enforced by
   `check:no-unit-specs`.
5. **Permanent backend tests protect only business rules, critical invariants, and security or
   authorization.** This includes important domain rules, critical writes, transactions,
   concurrency, audit, restore, corruption prevention, authentication boundaries, exact
   permissions, Owner behavior, pending or disabled states, 401/403 behavior, and write protection.
6. **Quran source, importer, generator, schema, and catalogue integrity checks are change-triggered
   or release gates, not part of the daily suite.**
7. **Tests must normally survive internal refactoring when protected behavior is unchanged.** A test
   coupled to DOM structure, CSS class names, container registration, collaborator identity, or
   private cache keys is defective.
8. **Use the cheapest permitted verification layer that catches the risk:** typecheck, build, a
   minimal approved update to retained protection, then a targeted runtime, manual, or browser
   check. Do not use a higher layer when a lower one suffices.
9. **Do not duplicate protection across layers.** When route smoke already proves routing,
   authorization, binding, and serialization, a per-feature test must not prove them again.
10. **Every implementation plan has a `Testing Decision`.** Name only the tests or gates the change
    requires, or state `none` with a reason.

## The Test Freeze

The Test Freeze is the operative default from Phase 3 onward.

- Future features add no automated tests by default: no test-per-component, per-service,
  per-endpoint, or per-DTO, and no coverage target or coverage claim.
- Normal feature verification is a backend build; the following frontend commands run independently
  and in this order; targeted runtime, manual, or browser smoke where appropriate; and engineering
  review:

  ```bash
  npm run check:no-unit-specs
  npm run typecheck:app
  npm run build:verify
  ```

  `check:no-unit-specs` is ordinary verification, not only a pre-PR check, and must never be folded
  into `build:verify`.
- A retained Permanent test may be minimally updated only when an approved change intentionally
  alters the Security or critical Business invariant it protects. A refactor-only update is a signal
  that the test is defective, not permission to grow it.
- A retained release/change gate may be minimally updated only when the approved change intentionally
  alters the exact source, schema, catalogue, importer, generator, rebuilder, migration, or
  canonical-data contract it protects. The update must not broaden coverage, add a class or file,
  migrate deleted coverage, or turn the gate into a general Permanent suite.
- Creating any new backend test class, backend test method, or Playwright E2E file requires explicit
  owner approval recorded in the change's `Testing Decision`. The frontend unit-spec prohibition has
  no exception.
- Quran data, importer, generator, rebuilder, migration, schema, catalogue, and canonical-data gates
  fire before release and whenever their own protected subject changes.
- Lifting the freeze belongs to a separate future initiative named **Testing Foundation V2**, after
  the product and its major behaviors stabilize. No current work anticipates or begins that initiative.
