<!--
Sync Impact Report
- Version: unratified template -> 1.0.0
- Added principles:
  - I. Explicit Scope and Authorization
  - II. Quran Data Integrity and Provenance
  - III. Contract and Layer Ownership
  - IV. Atomic, Authorized, and Controlled Mutation
  - V. Verification Follows the Testing Constitution
- Added sections:
  - Repository Constraints
  - Workflow and Quality Gates
- Removed sections: placeholder-only template sections
- Templates:
  - .specify/templates/plan-template.md: updated with a mandatory Testing Decision
  - .specify/templates/tasks-template.md: updated to defer test and Git work to explicit authority
  - .specify/templates/spec-template.md: no structural change required
- Command guidance:
  - .agents/skills/speckit-implement/SKILL.md: aligned with the Testing Constitution
  - .claude/skills/speckit-implement/SKILL.md: aligned with the Testing Constitution
- Deferred items: none
-->
# Quran Dashboard Engineering Constitution

## Core Principles

### I. Explicit Scope and Authorization

Every change MUST remain inside the user-approved feature, phase, contract, schema, and task
boundary. Work MUST stop before expanding any of those boundaries. Planning does not authorize
implementation; implementation does not implicitly authorize migration generation or application,
Git delivery, formal review, deployment, or production-state changes. `main` is protected and MUST
NOT receive direct edits or commits.

### II. Quran Data Integrity and Provenance

Quran text, identities, morphology, roots, translations, tafsir, links, and other religious source
data MUST NOT be invented, silently corrected, normalized, or mutated without explicit authority.
Source resources MUST retain their provenance and remain read-only unless the user expressly places
them in scope. Synthetic examples MUST be unmistakably synthetic and MUST NOT resemble authoritative
Quran content.

### III. Contract and Layer Ownership

Active feature intent MUST live in the selected Spec Kit artifacts; implemented truth MUST live in
code. Backend controllers own HTTP only, Application owns use cases, Domain owns business rules, and
Infrastructure owns persistence and external integrations. Frontend components own presentation,
data-access services own HTTP, and focused state owners own workflows. Public contracts MUST NOT
expose internal persistence or synchronization details unless the approved specification requires
them. Existing native routers and architecture authorities determine exact placement and style.

### IV. Atomic, Authorized, and Controlled Mutation

Any mutation spanning multiple records, layers, or derived projections MUST define its transaction,
concurrency, rollback, and ownership boundaries before implementation. Partial success MUST NOT be
reported where the feature requires atomic behavior. Unsafe routes MUST have one explicit
authorization classification. Expected domain or conflict outcomes MUST be handled as controlled
application results; centralized exception middleware is reserved for unexpected faults and MUST
not expose internal details.

### V. Verification Follows the Testing Constitution

Every implementation plan MUST contain an explicit Testing Decision governed by
`TESTING_CONSTITUTION.md`. The default is no new automated test; exceptions require explicit owner
approval. Plans and tasks MUST select only the cheapest authorized build, retained gate, runtime,
manual, or browser evidence that covers the risk. Coverage percentages and unapproved test-first
workflows MUST NOT be invented or cited.

## Repository Constraints

- Production-source changes MUST follow `CODING_PRINCIPLES.md`, including its comment policy,
  focused-change rules, Quran safety rules, and Definition of Done.
- Backend and Frontend work MUST follow their native `AGENTS.md` routers and selected architecture
  authorities.
- Arabic-first, RTL-aware product behavior MUST follow `PRODUCT.md`, `DESIGN.md`, and the Frontend
  UI authorities whenever user-facing scope is involved.
- Generated migrations, API models, permission constants, and other generated outputs MUST use the
  repository-sanctioned tooling and MUST NOT be hand-edited.
- Planning artifacts MUST follow the lifecycle in `docs/README.md` and `specs/README.md`.

## Workflow and Quality Gates

1. An active feature MUST be selected explicitly and identified in the root agent router while it
   remains open.
2. Specification and clarification establish product behavior; planning establishes technical
   design; tasks establish the executable dependency order.
3. Constitution checks MUST run before research and again after design. Any violation requires an
   explicit complexity justification or a separate constitution amendment.
4. Migration generation, database application, destructive data operations, tests outside the
   approved Testing Decision, Git delivery, formal review, and deployment each require their own
   authority.
5. Implementation MUST follow task order and stop at the authorized phase boundary. Final readiness
   requires the selected evidence, a clean scope check, and the separately requested review flow.

## Governance

This constitution is the non-negotiable governance source for Spec Kit workflows in this repository.
The user's explicit instruction and the root safety router remain controlling; native area routers
and retained authorities provide more specific rules but MUST NOT weaken these principles.

Amendments require explicit user authorization, a documented Sync Impact Report, semantic
versioning, and synchronization of affected templates and command guidance. MAJOR versions remove or
redefine a principle, MINOR versions add or materially expand governance, and PATCH versions clarify
wording without changing obligations. Every plan, task set, analysis, implementation pass, and
formal review MUST check the applicable principles and report unresolved violations rather than
silently waiving them.

**Version**: 1.0.0 | **Ratified**: 2026-08-17 | **Last Amended**: 2026-08-17
