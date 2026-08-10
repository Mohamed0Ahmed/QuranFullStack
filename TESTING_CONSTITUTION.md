# Testing Constitution

This file is the repository's single testing-policy authority. Test READMEs own only the commands,
fixtures, and operational details for their area.

1. **The default is no test.** Add a test only for a specific important risk. The burden of proof is
   on adding a test, not on omitting one.
2. **Do not create a test per component, service, endpoint, or DTO.**
3. **There is no coverage-percentage target.** Coverage is not measured and may not be cited.
4. **Frontend `*.spec.ts` is prohibited by default.** Verify the frontend with application
   typecheck, production build, Playwright journeys, and browser or visual verification. An isolated
   frontend unit test requires explicit owner approval recorded in the change's `Testing Decision`.
5. **Permanent backend tests protect only business rules, critical invariants, and security or
   authorization.** This includes important domain rules, critical writes, transactions,
   concurrency, audit, restore, corruption prevention, authentication boundaries, exact
   permissions, Owner behavior, pending or disabled states, 401/403 behavior, and write protection.
6. **Quran source, importer, generator, schema, and catalogue integrity checks are change-triggered
   or release gates, not part of the daily suite.**
7. **Tests must normally survive internal refactoring when protected behavior is unchanged.** A test
   coupled to DOM structure, CSS class names, container registration, collaborator identity, or
   private cache keys is defective.
8. **Use the cheapest verification layer that catches the risk:** typecheck, build, one assertion in
   an existing class, a new class, then a browser journey. Do not use a higher layer when a lower one
   suffices.
9. **Do not duplicate protection across layers.** When route smoke already proves routing,
   authorization, binding, and serialization, a per-feature test must not prove them again.
10. **Every implementation plan has a `Testing Decision`.** Name only the tests or gates the change
    requires, or state `none` with a reason.
