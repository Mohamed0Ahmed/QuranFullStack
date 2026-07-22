# Contract: Tree read / search / snapshot + composite-read redaction

**Feature**: `029-abwab-core` | **Source**: Master Plan §11 (Tree/read), §5.1, §6.6, §18.3 steps
1 & 4. Realizes §18.3 only; §11/§5.1 own the details.

## Scope

Read-only surface delivered in **Stage 1** (no mutation endpoint/editable UI at that checkpoint)
and consumed by the **Stage 4** frontend slice. Envelope: existing `ApiResponse<T>`. Every
actionable read DTO carries `TimelineGeneration`; mock and HTTP ports share the versioned contract
and cannot manufacture a current expectation.

## Operations

- **Complete tree snapshot** — `AbwabTreeSnapshot`: a **versioned complete snapshot** (not a paged
  hierarchy) exposing generation/revision/schema/server-time plus sections and categories, the
  `كل الأبواب` projection over independent root orders, ancestry/depth, and explicit child order.
- **Category search** — over **normalized name + aliases** (§5.1 normalization; Description is not
  in the primary search contract).
- **Dedicated effective-protection read** — direct/inherited protection with source ancestor and
  server-derived expiry (requires `protection.view`; see `manual-protection-contract.md`).

## Composite-read redaction (backend DTO projection — authoritative)

| Caller permissions | Tree / search | Manual-protection metadata |
|---|---|---|
| `category.view` + `section.view` | Returned, **with only** generic server-derived action-blocked / effective-manual-protection flags | **Omitted**: type/scope/actor/time/direct/inherited/source-ancestor |
| `category.view` + `section.view` + `protection.view` | Returned | Full metadata + dedicated effective-protection read |
| missing `category.view` **or** `section.view` | **Denied** — no tree/search | — |

- Every result exposes section/path context, so tree/search requires **both** `category.view` and
  `section.view`.
- **No partial response** may leak type/scope/actor/source-ancestor data. Redaction is enforced by
  **backend DTO projection, not frontend hiding**; the frontend mirrors visibility for UX only.
- Ordinary 24-hour last-editor/time/expiry is Category protection state (§6.6) returned with an
  authorized category view.

## Tests (parity across backend DTO, core mock, HTTP mapping, UI)

- Composite-read tests cover **every** grant combination of `category.view` / `section.view` /
  `protection.view`; assert the redaction table above with **0** leaks.
- Snapshot completeness/versioning, `كل الأبواب` projection, independent root orders, ancestry/depth
  read correctly against **real PostgreSQL**.
- Search parity over the shared §5.1 normalization fixture corpus (backend/db/API/frontend).
- Mock ≡ HTTP parity; neither can fabricate `TimelineGeneration`.
