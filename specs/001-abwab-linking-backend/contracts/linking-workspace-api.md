# Contract: Workspace API

> Wire truth is the regenerated swagger + generated models (research.md R16). All routes
> `[RequireOwner]`; the workspace is **always the caller's own** — ownership resolves from
> `AuthorizationState.UserId` (F10); there is no `?userId=` and no admin view (spec FR-026).
> All responses use the standard `ApiResponse<T>` envelope with Arabic messages.

## Routes

| Route | Concurrency token | Purpose |
| --- | --- | --- |
| `GET /api/linking/workspace` | — | Load — **strictly read-only** (spec FR-019, research.md R21): returns the stored workspace, or an **empty representation** (`workspaceVersion: null`, empty `sources`) when none exists. **Never inserts.** |
| `POST /api/linking/workspace/sources` | workspace version (`null` allowed while no workspace exists) | Add a source — **idempotent by `sourceIdentity`** (uniqueness enforced via `source_identity_hash` + raw final guard, research.md R20): re-adding an equivalent descriptor refreshes the label only, leaving order and configuration untouched (spec FR-004/FR-020). The **first** mutation creates the missing workspace row atomically inside its own transaction; concurrent first mutations are serialized by `UNIQUE (user_id)` |
| `DELETE /api/linking/workspace/sources/{id}` | workspace version | Remove one source (children cascade) |
| `PUT /api/linking/workspace/sources/order` | workspace version | Reorder — body carries the complete ordered source-id list |
| `PUT /api/linking/workspace/sources/{id}/configuration` | **source** version | Replace that source's configuration **wholesale** (plan D9) |
| `DELETE /api/linking/workspace/sources` | workspace version | Clear all sources |

Version placement: structural routes carry `workspaceVersion`, the configuration route carries
`sourceVersion` — in the request body for POST/PUT and as a required query parameter for DELETEs
(exact wire placement is whatever the generated contract records; the semantic rule is fixed:
**every modifying call carries the version the client last read**, spec FR-027).

## `LinkingWorkspaceDto` (GET response, and returned by every mutation)

```jsonc
{
  "workspaceVersion": 1234,          // xmin, opaque to the client; null until the workspace row exists
  "sources": [                        // ordered by orderValue
    {
      "id": 7, "sourceVersion": 5678, "orderValue": 1,
      "descriptor": { /* LinkingSourceDescriptorBody shape — see linking-sources-api.md */ },
      "sourceIdentity": "root|42",
      "inclusionMode": "all_except",          // "all_except" | "only"
      "ayahOverrides": [ 262, 263 ],           // ayahIds interpreted per inclusionMode
      "selectedWords": [ { "ayahId": 262, "quranWordId": 12345 } ],   // manual Mushaf only — user-authored; always [] for automatic sources
      "automaticWordMatchesEnabled": true,     // automatic families only, else null; ON: words derive from resolution — OFF: ayahs contribute zero words
      "manualLinkShape": null,                 // "grouped" | "independent", manual family only
      "manualAyahs": [                          // manual family only — identity-bearing, read-only here
        { "ayahId": 262, "verseKey": "2:255", "orderValue": 1, "pageHint": 42 } ],
      "descriptions": [ { "ayahId": 262, "orderValue": 1, "body": "…" } ],
      "lastResolvedCount": 1994, "lastResolvedAtUtc": "2026-08-12T10:00:00Z"
    }
  ]
}
```

## Configuration document (`PUT …/{id}/configuration` body)

One complete document; the writer diffs children, resequences description order `1..N`, and
hard-deletes what is absent (research.md R10):

```jsonc
{
  "sourceVersion": 5678,
  "label": "جذر: قول",
  "inclusionMode": "only",
  "ayahOverrides": [ 262 ],
  "selectedWords": [ { "ayahId": 262, "quranWordId": 12345 } ],   // manual Mushaf only — MUST be empty/absent for automatic sources (rejected otherwise)
  "automaticWordMatchesEnabled": false,   // XOR manualLinkShape — kind coherence, FR-022; the ONLY word control automatic sources have
  "manualLinkShape": null,
  "descriptions": [ { "ayahId": 262, "orderValue": 1, "body": "…" } ]
}
```

**The manual verse set is NOT part of this document.** It is identity-bearing (see
`source-identity.md`): changing the verses changes the identity, i.e. produces a *different
source* — the flow for that is add-new-source. `manualAyahs` is written once at add time from the
descriptor and is read-only thereafter.

## Validation (writer-enforced; DB CHECKs back the coherence rules — data-model.md)

- Descriptor valid per family; add is refused above `MaxPreparedSources` (default 100, FR-029).
- Kind/configuration coherence: `automaticWordMatchesEnabled` non-null iff automatic family;
  `manualLinkShape` non-null iff manual family (FR-022).
- `selectedWords` are legal **only on manual Mushaf sources** (FR-021, research.md R22); a
  non-empty `selectedWords` on an automatic source is rejected with 400 regardless of the words'
  own validity. Each manual selected word: exists, is non-marker, belongs to its declared ayah,
  and that ayah belongs to the source's manual verse set (FR-023). Words arrive as canonical
  `quranWordId` already (the Frontend resolves click coordinates via the resolved source,
  Phase 11). A manual ayah with zero selected words is valid (FR-008).
- Automatic sources carry only `automaticWordMatchesEnabled` — their word contributions are
  derived server-side from resolution (toggle on) or empty (toggle off); never authored.
- Every `ayahId` in overrides/descriptions references a real ayah (FK-backed, FR-024) and, for
  descriptions, an ayah of that source's own set (FR-034).
- Descriptions: ≤10 per (source, ayah), 1–2000 chars trimmed non-blank, contiguous `1..N`
  (FR-031..FR-033).
- Reorder body must list exactly the workspace's current source ids (a permutation — nothing
  added, nothing missing).

## Status mapping

| Status | When |
| --- | --- |
| 200 | Success (all routes; GET is read-only — an absent workspace reads as an empty representation) |
| 400 | Validation failure (any rule above) — Arabic message naming the offense |
| 404 | Source id not found *in the caller's own workspace* |
| 409 | Stale `workspaceVersion`/`sourceVersion` (`LinkingStaleVersionException`) or duplicate-identity race (`LinkingDuplicateContributionException`) — never last-writer-wins (FR-027); client reloads and re-presents (spec US2-3) |

## Explicitly never stored (spec FR-028)

Checked/unchecked source state, active surface, search text, viewport/scroll, review position,
selected Door — all remain client-side. The workspace is durable preparation state, **not** a
UI-state sync channel, and **not a cache** (state this in `Persistence/Writes/Linking/README.md`).
