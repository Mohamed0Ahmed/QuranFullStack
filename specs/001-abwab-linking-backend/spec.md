# Feature Specification: Abwab Ayah Linking — Real Persistence, Preflight, and Confirmation

**Feature Branch**: `feature/abwab-linking-frontend-prototype` (existing branch, kept by user decision — no new branch was cut)

**Created**: 2026-08-12

**Status**: Draft

**Input**: User description: "read our plan from docs/abwab-linking-backend-implementation-plan.md — The implementation will be done using a cheaper model, so the specification and everything should be super clear"

## Context

The Quran Dashboard lets a curator link Quran ayahs to **Doors** (thematic chapters of the Abwab
collection). A working V2 prototype of this flow exists, but it stores the curator's prepared work
only inside one browser and ends in a simulated confirmation — nothing is truly saved. This feature
replaces the simulated parts with real, durable behavior while keeping the proven V2 user
experience unchanged in shape: same workflow, same concepts, now real.

### Glossary

| Term | Meaning |
| --- | --- |
| **Door** | A thematic chapter in the Abwab collection that ayahs get linked to. Doors already exist in the product. |
| **Curator** | The dashboard **Owner** — the only person who can use linking. There are no other linking roles. |
| **Source** | A rule or manual pick that produces a set of ayahs. Six families exist (see FR-001). |
| **Source identity** | A canonical text key computed from a source's defining selection. Two equivalent selections always produce the same identity. |
| **Resolution** | Turning a source into its complete, validated list of matching ayahs and words. |
| **Workspace** | The curator's private, durable staging area of prepared sources, their configuration, and descriptions — before anything is confirmed into a Door. |
| **Preflight** | A read-only check run before confirmation that classifies exactly what the operation would change in the chosen Door. |
| **Confirmation** | The single atomic action that applies the operation to the Door. |
| **Contribution** | The durable record of what one source has confirmed into one Door: its ayahs, chosen words, grouping, and descriptions. |
| **Unit** | A grouping of linked ayahs inside a contribution. Automatic sources produce one single-ayah unit per ayah; a manual "grouped" source produces exactly one unit holding all its ayahs. |
| **Ayah marker** | The decorative end-of-ayah symbol that renders as part of the text but is never a linkable word. |
| **Canonical word identifier** | The permanent, durable identifier of a single Quran word. The only allowed word identity — never a screen position, list index, or the word's text. |

## Clarifications

### Session 2026-08-12

- Q: What happens when a confirmed source is re-confirmed with zero ayahs (total emptying)? → A: Rejected — a submitted source must contribute at least one ayah; total retraction stays out of scope alongside the deferred delete/restore capabilities.
- Q: What default value should the resolution size cap (maximum ayahs per resolved source) have? → A: 3,000 — headroom over the largest known real source (≈2,200 ayahs) while bounding the worst-case payload; the value is configurable.
- Q: When a confirmed source is re-confirmed and only its display label differs, how does it classify? → A: Unchanged — the label is excluded from change comparison; nothing is written, and the stored label refreshes the next time a real update rewrites the contribution.

### Remediation 2026-08-12

Alignment decisions recorded during the artifact remediation pass (behavior-level; design-level
counterparts live in research.md R20–R22 and the contracts):

- Manual Mushaf ayahs may carry **zero** selected words — the ayah still contributes; only
  automatic families guarantee at least one matched word per returned ayah (FR-008).
- Loading the workspace **never writes**: when no workspace exists an empty representation is
  returned; the workspace record is created by the first real mutation (FR-019).
- A fully-unchanged confirmation stores **no** operation record and **no** idempotency record —
  repeating it re-evaluates and returns the same no-op success (FR-049/FR-050).
- Attribution lives on authored/lifecycle records only; leaf relational rows inherit history from
  their parent aggregate (FR-052).
- Automatic families never carry user-authored word selections: their word contributions are
  derived from resolution when the word-match toggle is on, and are empty when it is off; only
  manual Mushaf sources carry user-authored selected words (FR-021/FR-023).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Complete, validated source resolution (Priority: P1)

A curator picks a linking source — any of the six families — and receives the **complete**,
validated list of matching ayahs in Quran order, with every word individually identified, in a
single step. Manual Mushaf selections go through the same validation as automatic families, so all
sources are equally trustworthy.

**Why this priority**: Every other capability (workspace, preflight, confirmation) consumes
resolved sources. It also removes today's biggest trust and performance gap: the prototype
assembles large sources from up to 20 sequential partial loads and trusts the browser's own math
for manual selections.

**Independent Test**: Resolve one source of each family and inspect the returned list — complete,
ordered, every word identified — without any workspace, preflight, or confirmation existing yet.

**Acceptance Scenarios**:

1. **Given** a Root source matching ~2,000 ayahs, **When** the curator resolves it, **Then** all
   ~2,000 ayahs return from one action, ordered by surah number then ayah number, each with its
   full in-order word list, and every word carries its canonical word identifier.
2. **Given** a manual Mushaf selection of N verses including one verse that spans a page boundary,
   **When** resolved, **Then** exactly N complete ayahs return and the page-spanning verse appears
   as one uninterrupted, correctly ordered word list.
3. **Given** a manual selection containing one unknown or malformed verse reference, **When**
   resolved, **Then** the whole resolution is rejected with a message naming that exact verse —
   never a silent drop, never a partial result.
4. **Given** a source whose match count exceeds the configured maximum, **When** resolved,
   **Then** a controlled validation message returns and no oversized result is produced.
5. **Given** the same manual verse set entered in a different order or with duplicates, **When**
   the source identity is computed, **Then** the identity is identical to the canonical one.
6. **Given** a resolved **automatic** source, **Then** every returned ayah has at least one
   matched word (an ayah with no match is never in the set), and ayah-marker inclusion per family
   matches today's explorer behavior exactly. **Given** a resolved **manual Mushaf** source,
   **Then** every selected ayah returns its complete canonical word list and its selected/matched
   word set may be empty — the ayah still contributes.

---

### User Story 2 - Durable personal workspace (Priority: P2)

A curator prepares sources over multiple sessions. Their prepared set — sources, order, per-source
configuration — lives under their account and follows them to any browser or device. Nothing
prepared is lost by signing out.

**Why this priority**: Losing prepared work when switching browsers is the prototype's most
painful user-facing limitation. Durable preparation is valuable on its own, even before real
confirmation ships.

**Independent Test**: Prepare sources in one browser, sign out, sign in from a different browser,
and verify the workspace reappears exactly.

**Acceptance Scenarios**:

1. **Given** a curator prepares 3 sources of different families, configures each (inclusion
   choices; word selections where the family allows them, per FR-021), and reorders them, **When**
   they sign out and open a different browser, **Then** all three reappear with order and
   configuration fully preserved.
2. **Given** a source already in the workspace, **When** an equivalent selection is added again,
   **Then** no duplicate is created: the display label refreshes and the existing order and
   configuration remain untouched.
3. **Given** the same source open in two tabs, **When** both save configuration and the second
   save is based on outdated data, **Then** the second save is rejected with a clear, recoverable
   conflict message — never a silent overwrite.
4. **Given** curator A's workspace has content, **When** any other user uses linking, **Then**
   they can never see or affect curator A's workspace.
5. **Given** any workspace operation, **Then** transient view state (which sources are checked,
   active surface, search text, scroll position, review position, selected Door) stays local to
   the browser and resets exactly as it does today.

---

### User Story 3 - Per-ayah descriptions on each source (Priority: P3)

While preparing a source, the curator writes short ordered notes ("descriptions") on individual
ayahs of that source. Descriptions belong to that source's view of that ayah — two sources
covering the same ayah keep fully separate description lists.

**Why this priority**: Descriptions are locked product scope and part of what confirmation must
carry, but the workspace and resolution must exist first.

**Independent Test**: Add, edit, reorder, and remove descriptions on one source's ayahs and verify
limits and persistence, without any confirmation existing.

**Acceptance Scenarios**:

1. **Given** an ayah of a prepared source with 10 descriptions, **When** an 11th is added,
   **Then** it is refused with a clear message; the 10 remain intact.
2. **Given** a description of 2001 characters, or one that is blank after trimming, **When**
   saved, **Then** it is refused.
3. **Given** descriptions 1..N on an ayah, **When** one is removed or the order changes, **Then**
   the survivors persist renumbered contiguously 1..N with their text unchanged.
4. **Given** two sources both containing ayah X, each with its own descriptions on X, **Then** the
   two lists remain fully separate everywhere they appear.

---

### User Story 4 - Preflight: know exactly what will change (Priority: P4)

Before confirming into a Door, the curator sees a precise classification of what the operation
would do: per source and per ayah — what is new, what already exists via another source, what is
unchanged, what would update, what would be removed, and what is invalid. Items are always
individually inspectable; counts never replace them.

**Why this priority**: The locked flow makes preflight mandatory before confirmation; it is the
transparency layer that makes a real write safe to offer.

**Independent Test**: Run preflight against Doors with known existing content and verify each
classification, without ever confirming.

**Acceptance Scenarios**:

1. **Given** a Door already holding source «الرحمن» with ayahs A, B, C, **When** a new source
   «الرحيم» with ayahs A, D, E is preflighted, **Then** the source classifies as *new source*,
   ayah A classifies as *overlap via another source* (naming «الرحمن»), ayahs D and E classify as
   *new ayah*, and the counts read: 3 requested = 2 new + 1 overlapping + 0 unchanged + 0 updated
   + 0 invalid.
2. **Given** an operation containing one source identical to its confirmed state and one brand-new
   source, **When** preflighted, **Then** they classify *unchanged* and *new source* respectively,
   the operation is not blocked and is not a no-op.
3. **Given** an operation where every source is identical to its confirmed state, **When**
   preflighted, **Then** the result is flagged "nothing to change" as a normal informational state
   — not an error.
4. **Given** a source whose new state no longer contains ayah B that its confirmed contribution
   holds, **When** preflighted, **Then** B classifies *removed* for this source only, and B's
   presence via any other source is untouched.
5. **Given** an archived Door, an unknown ayah, an ayah-marker submitted as a selected word, or a
   word that does not belong to its declared ayah, **When** preflighted, **Then** each classifies
   *invalid* with a per-item reason and the operation is marked blocked.
6. **Given** any preflight call, **Then** stored data is bit-for-bit identical before and after —
   preflight never writes.

---

### User Story 5 - Atomic confirmation and update (Priority: P5)

The curator confirms the operation. The system applies it as one all-or-nothing action: new
contributions are created, changed ones are replaced in place, unchanged ones are untouched, and a
wholly-unchanged operation succeeds with a friendly "nothing new to apply" message. Re-linking the
same source to the same Door can never produce a duplicate.

**Why this priority**: This is the product's end value — real links instead of a simulated result
— but it depends on everything before it.

**Independent Test**: Confirm known operations against a local Door and inspect stored results
after each — creation, in-place update with replacement semantics, no-op, conflict, and replay.

**Acceptance Scenarios**:

1. **Given** the Door from User Story 4 scenario 1, **When** «الرحيم» (A, D, E) is confirmed,
   **Then** «الرحمن»'s stored contribution is completely unchanged, «الرحيم» is added
   independently, and ayah A is now linked in this Door via two separate contributions.
2. **Given** a source identical to its confirmed state, **When** confirmed again, **Then**
   nothing is written and the response is the success message «لا توجد تغييرات جديدة لتنفيذها».
3. **Given** a confirmed manual source whose selected words on ayah A were [w1, w2] and whose new
   state has no words on A (for an automatic source, the equivalent is switching its word-match
   toggle off), **When** confirmed, **Then** ayah A of that contribution has zero word
   contributions — replacement, never a merge of old and new.
4. **Given** a multi-source operation where one source is invalid, **When** confirmed, **Then**
   the entire operation is rejected and stored data is untouched — no partial application.
5. **Given** a confirmation that succeeded, **When** the identical submission (same operation key)
   is replayed, **Then** the original outcome is returned and nothing is written twice.
6. **Given** two simultaneous confirmations of the same new source into the same Door, **Then**
   exactly one succeeds and the other receives a conflict — never two live contributions.
7. **Given** an update to an existing contribution, **Then** the contribution keeps its identity
   (it is updated in place, not deleted and recreated).
8. **Given** a manual grouped source over ayahs {A, B} and an automatic source over {A, C} both
   confirmed, **Then** the stored grouping is [[A, B]] and [[A], [C]] — three units under two
   contributions, never collapsed into one merged group.

---

### User Story 6 - Instant repeat access (Priority: P6)

Opening a source the curator has already opened is effectively instant — the expensive matching
work is not repeated, on the server or in the browser session.

**Why this priority**: Sources are opened repeatedly during preparation and review; without result
reuse, every open of a 2,000-ayah source repeats heavy work. It is an efficiency layer over User
Story 1 and depends on it.

**Independent Test**: Resolve the same source twice and demonstrate the second resolution performs
no repeated retrieval work; resolve two nearly identical sources and demonstrate no cross-serving.

**Acceptance Scenarios**:

1. **Given** a source resolved moments ago, **When** it is resolved again, **Then** the result is
   served from the retained copy with zero repeated data-store work on the server, and reopening
   within the same browser session performs zero network requests.
2. **Given** two Word Type sources differing in exactly one scope field, **Then** they are treated
   as entirely different sources — results are never cross-served.
3. **Given** any retained result, **Then** it expires after a bounded idle period and, regardless
   of use, after a bounded absolute age; a result can never stay fresh forever.
4. **Given** several large sources warmed in memory, **Then** total memory use remains bounded
   (retained results are compact; full display text is shared, not duplicated per source).

---

### User Story 7 - Fluid editing of large sources, with provenance (Priority: P7)

The curator edits a 2,000-ayah source as one continuous scrolling list — no pagination — writes
descriptions inline, and, in review, sees each merged ayah with the union of contributed words and
the names of every source that contributed it.

**Why this priority**: These are the three known presentation gaps of the prototype; they land
last because they reshape the same per-ayah row the earlier stories feed.

**Independent Test**: Open a 2,000-ayah source in the editor and exercise scrolling, exclusion,
descriptions, and the merged review display.

**Acceptance Scenarios**:

1. **Given** a 2,000-ayah source in the editor, **Then** it renders as one continuous scrollable
   list with no pagination controls, any ayah can be excluded at any position, and the number of
   rendered elements stays bounded no matter how long the list is.
2. **Given** the curator excludes an ayah near the end, scrolls far away, and scrolls back,
   **Then** the exclusion is still applied.
3. **Given** the editor surface at wide, medium, and compact widths, **Then** exactly one region
   owns vertical scrolling.
4. **Given** a review of an ayah matched by two sources, **Then** the display shows the union of
   both sources' contributed words and names both contributing sources, and each source's
   descriptions remain listed separately.
5. **Given** any of these screens, **Then** Quran text rendering (glyphs, spacing, line metrics)
   is unchanged from today.

---

### Edge Cases

- A manual verse set entered with duplicates or out of order — identity and resolution treat it as
  the de-duplicated, canonically ordered set.
- An ayah whose stored word data is incomplete or non-contiguous — resolution fails with a
  blocking message naming the verse; a partial ayah is never published to any consumer.
- A resolution larger than the configured maximum — controlled validation failure, never a
  truncated or unbounded response.
- A source selection referencing a dimension (root, lemma, stem, word) that does not exist —
  controlled "not found" failure naming the problem.
- Re-adding an equivalent source with a different display label — label updates; identity, order,
  and configuration are untouched.
- Adding a source beyond the prepared-sources maximum (default 100) — controlled refusal.
- Two tabs, two devices, or two rapid submissions editing the same thing — one wins, the other
  gets a recoverable conflict; nothing is silently overwritten and nothing half-applies.
- The Door's confirmed content changes between preflight and confirm — the confirm detects
  staleness, returns a conflict carrying a fresh classification, and the screen re-presents it
  instead of failing.
- An automatic source configured with a manual-only setting (or vice versa) — rejected even if a
  defective client submits it.
- A user-authored selected word that is an ayah marker, belongs to a different ayah, or names an
  ayah outside the manual source's own verse set — rejected during validation and classified
  invalid in preflight. User-authored words submitted on an **automatic** source — rejected
  outright as an invalid combination (FR-021/FR-023).
- A manual Mushaf ayah with zero selected words — valid everywhere: the ayah still contributes,
  and nothing forces a word choice (FR-008).
- An update that empties an ayah's words (or its descriptions) — applied literally per replacement
  semantics; nothing old survives by accident.
- A source submitted with zero ayahs (everything excluded) — rejected with a controlled validation
  message at both preflight and confirmation: a submitted source must contribute at least one ayah
  (FR-044a). Total retraction of a source's links is out of scope.
- The curator's old browser-local prototype data — never migrated; cleared after the first
  successful load of the server workspace so a stale copy cannot resurface.

## Requirements *(mandatory)*

### Functional Requirements

#### Source definition and identity

- **FR-001**: The system MUST support exactly six source families, each defined by its own
  parameters:
  1. **Unique Word** — a chosen word plus a matching mode: *simple spelling* or *exact
     diacritics*.
  2. **Root** — a chosen morphological root.
  3. **Lemma** — a chosen lemma, optionally narrowed by a word-type code.
  4. **Stem** — a chosen stem, optionally narrowed by a word-type code.
  5. **Word Type** — a selection made inside the word-type explorer: the kind of item selected
     (word, root, stem, or lemma), the selected item, optional grammatical context (context code,
     case, tense, voice), and the word-type scope the selection was made within.
  6. **Manual Mushaf** — an explicit set of verses the curator picked from the Mushaf, plus a link
     shape: *grouped* (one combined link) or *independent* (one link per verse).
- **FR-002**: Every source MUST have a deterministic **source identity** computed only from its
  defining selection — never from its display label, the user, a Door, or any configuration. The
  same selection always yields the same identity; for manual sources, verse order and duplicates
  MUST NOT affect the identity.
- **FR-003**: The identity the server computes MUST be character-for-character identical to the
  identity the existing V2 prototype computes for the same selection. During acceptance, one
  worked example per family MUST be compared against the prototype's output by hand. (A silent
  divergence would split result reuse and break workspace de-duplication.)
- **FR-004**: A source's display label is a snapshot for humans. It MUST never participate in
  identity, and re-adding an equivalent source MUST refresh the label only. The label also MUST
  NOT participate in change classification: confirming a source whose only difference is its label
  classifies *unchanged* and writes nothing — the stored label refreshes the next time a real
  update rewrites the contribution.

#### Source resolution

- **FR-005**: Resolving a source MUST return its complete validated ayah set in a single
  operation, up to the configured maximum — never partial pages the client must walk.
- **FR-006**: Resolution output MUST be deterministically ordered: ayahs by surah number then ayah
  number; each ayah's words by their position in the ayah. Two resolutions of the same source MUST
  produce identically ordered results.
- **FR-007**: Every returned word MUST carry its canonical word identifier. Screen positions, list
  indexes, and word text MUST be rejected wherever a word identity is accepted.
- **FR-008**: Each returned ayah MUST state which of its words matched. For **automatic**
  families, every returned ayah MUST have at least one matched word — an ayah with no match is
  never in the set. For **manual Mushaf** sources, the selected/matched word set MAY be empty: a
  manually chosen ayah with zero selected words is valid and still contributes the ayah, and its
  complete canonical word list is returned regardless.
- **FR-009**: Ayah-marker inclusion MUST match today's explorer behavior per family: Unique Word
  results include markers (flagged as markers); Root, Lemma, Stem, and Word Type results exclude
  them. (Manual Mushaf has no explorer counterpart and includes markers, flagged, because its
  completeness proof counts non-marker words and the manual reader renders markers deliberately.)
- **FR-010**: Manual Mushaf verses MUST be validated by the system of record before being served:
  the verse exists; its full word list is present, in order, and contiguous; the word count
  matches the canonical count; every word's placement is consistent with the verse. Any failure
  MUST block the whole resolution with a message naming the exact verse.
- **FR-011**: A resolution exceeding the configured maximum ayah count (default **3,000**) MUST
  fail with a controlled validation message. The default clears the largest known legitimate
  source (≈2,200 ayahs) with headroom; verifying this guard is done by lowering the configured
  value in a local environment, never by hunting for an oversized real source.
- **FR-012**: A selection referencing a dimension that does not exist MUST fail with a controlled
  "not found" message.
- **FR-013**: All existing explorer and Mushaf-reader behavior MUST remain byte-identical:
  existing routes, response shapes, page sizes, and result contents are untouched by this feature.

#### Repeat-access performance

- **FR-014**: Resolving a source identical to a recently resolved one MUST NOT repeat the
  expensive matching work: the server serves its retained result with no data-store reads, and a
  browser session that already holds the result performs no network request.
- **FR-015**: Retained results MUST be keyed only by the source's defining selection — never by
  user, Door, inclusion choices, selected words, descriptions, or any workspace state — so one
  retained result is safely shared by everyone.
- **FR-016**: Two sources differing in any defining field MUST never cross-serve each other's
  results.
- **FR-017**: Retained results MUST have a bounded lifetime: they expire after a configured idle
  period (default 30 minutes) and, regardless of use, after a configured absolute age (default 4
  hours). Total retained memory MUST stay bounded. A service restart clearing retained results is
  acceptable; no data-change-driven invalidation is required because the underlying Quran data
  never changes at runtime.
- **FR-018**: Concurrent identical resolutions MUST collapse into one computation, and a failed
  computation MUST NOT be retained — the next request recomputes.

#### Workspace

- **FR-019**: Each user MUST have exactly one workspace, with no setup step and no separate
  creation action. Loading the workspace MUST be strictly read-only: when no workspace exists yet,
  the load returns an empty workspace representation **without creating anything**; the workspace
  record is created only by the first real mutation (such as adding the first source).
- **FR-020**: The workspace MUST support: loading its full content; adding a source (idempotent by
  source identity); removing a source; reordering sources; replacing one source's configuration as
  a whole document; and clearing all sources.
- **FR-021**: Per-source configuration MUST cover: inclusion mode — *all except* an exclusion list
  or *only* an inclusion list of ayahs; the word contribution rule for the source's family; the
  link shape *grouped* or *independent* (manual family only); the ordered per-ayah descriptions
  (FR-031..FR-035); and the display label. Word contributions are family-specific and MUST NOT
  cross over:
  - **Automatic families** (Unique Word, Root, Lemma, Stem, Word Type) carry **only** the
    automatic word-match toggle. Toggle **on** ⇒ the ayah's word contributions are exactly the
    words the resolution matched. Toggle **off** ⇒ the ayah is still included with **zero** word
    contributions. The curator never authors individual words on an automatic source.
  - **Manual Mushaf** sources carry user-authored per-ayah selected words (canonical word
    identifiers only) — and may leave any ayah with zero selected words (FR-008).
- **FR-022**: Incoherent configuration MUST be rejected even if a defective client submits it: an
  automatic source can never carry a manual link shape, and a manual source can never carry the
  automatic word-match toggle. This coherence MUST hold in durable storage itself, not only in
  request validation.
- **FR-023**: User-authored selected words exist only on manual Mushaf sources. Every one MUST be
  validated on save: it exists, is not an ayah marker, belongs to the ayah it is declared under,
  and that ayah belongs to the source's own manual verse set. A submission that authors words on
  an **automatic** source MUST be rejected outright (FR-021) — the combination is invalid
  regardless of the words' own validity.
- **FR-024**: Manual verse selections and ayah inclusion/exclusion entries MUST be validated as
  references to real ayahs — not merely well-formed text.
- **FR-025**: Workspace content MUST persist across sign-out, sign-in, browsers, and devices.
- **FR-026**: A workspace MUST be strictly private to its owner. Ownership MUST derive from the
  authenticated identity of the caller; no request may name a different user's workspace.
- **FR-027**: Every workspace-modifying action MUST carry the version the client last read, and a
  stale version MUST be rejected with a recoverable conflict — never last-writer-wins. Structural
  actions (add, remove, reorder, clear) are versioned at the workspace level; configuration
  replacement is versioned per source, so edits to two different sources never falsely conflict.
- **FR-028**: Transient view state (checked sources, active surface, search text, scroll position,
  review position, selected Door) MUST remain client-side and MUST NOT be stored in the workspace.
- **FR-029**: The number of prepared sources per workspace MUST be bounded (default maximum 100);
  exceeding it is a controlled refusal.
- **FR-030**: Existing browser-local prototype data MUST NOT be migrated. After the first
  successful load of the server workspace, the obsolete local copy MUST be cleared so it cannot
  resurface.

#### Descriptions

- **FR-031**: Each (source, ayah) pair MUST hold at most 10 descriptions, ordered 1..N. An 11th
  MUST be refused.
- **FR-032**: A description MUST be 1–2000 characters after trimming, non-blank, and treated as
  plain text everywhere it is stored or displayed — no markup is ever interpreted.
- **FR-033**: Descriptions MUST be individually editable, removable, and reorderable; after any
  change the order MUST be contiguous 1..N.
- **FR-034**: A description MUST belong to one source's view of one ayah. Descriptions are never
  shared or merged across sources, and the ayah MUST belong to that source's own set.
- **FR-035**: The description limits MUST be enforced identically at every layer — the screen, the
  service, and durable storage — from a single authoritative definition, so the layers cannot
  drift apart.

#### Preflight

- **FR-036**: The confirmation flow MUST run in this order: configure sources → resolve → choose
  Door → **preflight** → review → confirm. Preflight is mandatory before confirmation: a
  confirmation submitted without the preflight stage's freshness token MUST be refused with a
  controlled validation failure, so confirmation cannot be reached while skipping preflight.
- **FR-037**: Preflight MUST classify each submitted source as exactly one of:

  | Source classification | Meaning |
  | --- | --- |
  | **New source** | No live contribution exists for this Door + source identity; one will be created. |
  | **Unchanged** | A live contribution exists and the submitted state is identical (ayahs, words, descriptions, grouping — the display label is excluded per FR-004). Nothing will be written. |
  | **Update** | A live contribution exists and something differs. It will be replaced in place. |
  | **Invalid** | The source's data is no longer valid. Blocks the whole operation. |

- **FR-038**: Preflight MUST classify each ayah of each source as exactly one of (mutually
  exclusive):

  | Ayah classification | Meaning | Blocking |
  | --- | --- | --- |
  | **New ayah** | Not currently contributed by this source, and not present in this Door via any other source. | No |
  | **Overlap via another source** | Would be newly added for this source, and already exists in this Door via at least one other source. Informational — the new contribution is still added independently. | No |
  | **Unchanged** | Present for this source with identical words, descriptions, and grouping. | No |
  | **Update** | Present for this source, but its words, descriptions, membership, or source-owned configuration changed. | No |
  | **Removed** | Present in the current confirmed contribution but absent from the newly submitted state. Removed from this contribution only. | No |
  | **Invalid** | The Door, source, ayah, word, or grouping data is no longer valid. | **Yes** |

- **FR-039**: Classification precedence MUST be: a source-owned change wins. An ayah that is
  *update* or *removed* for this source keeps that classification even when it also overlaps
  another source. *Overlap via another source* applies only where the ayah would otherwise be
  *new ayah*. Regardless of classification, every item MUST identify the other sources in this
  Door that hold the same ayah — each with its human-readable label and its source family, not
  merely a technical identity — so the display can name them meaningfully in Arabic.
- **FR-040**: Per source, the counts MUST partition the submitted set exactly — submitted = new +
  overlapping + unchanged + updated + invalid — with removed counted separately (removed items are
  not part of the submitted set). Counts MUST always accompany the itemized ayah lists and MUST
  never replace them; every item MUST expose its exact word-level and description-level
  differences.
- **FR-041**: Only *invalid* blocks. *Unchanged* and *overlap* are informational. An operation
  where every source is unchanged MUST be flagged "nothing to change" and treated as a normal,
  non-blocking state.
- **FR-042**: Preflight MUST NOT write anything. Stored data before and after any preflight call
  is identical.
- **FR-043**: The preflight result MUST carry a freshness token that confirmation **requires**
  (FR-036). Required is not trusted: the token only proves the flow passed through preflight and
  is never authority for the write — confirmation MUST re-verify everything itself (FR-045); when
  the Door's confirmed content moved since preflight, confirmation MUST return a conflict carrying
  a fresh classification, and the screen MUST re-present it rather than fail.

#### Confirmation

- **FR-044**: Confirmation MUST be one atomic, all-or-nothing operation across all submitted
  sources. If any part is rejected, stored data is untouched. Every check that depends on the
  Door's current confirmed state MUST be performed within the same atomic operation that applies
  the changes, so nothing can change between the check and the write.
- **FR-044a**: Every source submitted to preflight or confirmation MUST contribute at least one
  ayah. A source whose submitted state contains zero ayahs is a controlled validation failure —
  totally retracting a source's links from a Door is not part of this feature (it belongs with the
  deferred delete/restore capabilities).
- **FR-045**: Confirmation MUST fully re-validate on the server regardless of what preflight said:
  the Door exists and is not archived; every descriptor is valid; every submitted source
  contributes at least one ayah (FR-044a); every submitted ayah is a member
  of its source's freshly resolved result (so a tampered submission cannot inject ayahs); every
  user-authored word (manual sources only) passes the FR-023 checks while automatic word
  contributions are derived server-side per FR-021; grouping is coherent (FR-046); description limits hold
  (FR-031..FR-032); and the classification is recomputed with exactly the same meaning as
  preflight (the two can never disagree about semantics).
- **FR-046**: Grouping MUST be stored exactly as submitted and never inferred from display:
  an automatic source produces one single-ayah unit per ayah; a manual *grouped* source produces
  exactly one unit holding all its ayahs; a manual *independent* source produces one single-ayah
  unit per verse. A grouped manual {A, B} plus an automatic {A, C} MUST persist as [[A, B]] and
  [[A], [C]] — three units under two contributions, never [[A, B, C]].
- **FR-047**: At most one **live** contribution may exist per (Door, source identity).
  Confirming a source that already has one MUST update it in place — its identity is stable, it is
  never deleted and recreated. Two racing confirmations of the same new source MUST resolve to
  exactly one winner; the loser receives a conflict.
- **FR-048**: An update MUST replace, never merge: the newly confirmed state is the complete new
  truth for that source — its ayah set, each ayah's words, and each ayah's descriptions. Words
  [w1, w2] replaced by [] means no words. An ayah absent from the new state is removed from this
  contribution only and never from any other source's contribution.
- **FR-049**: An unchanged source MUST NOT be written at all. An operation where every source is
  unchanged MUST record nothing anywhere and MUST return the success message
  «لا توجد تغييرات جديدة لتنفيذها».
- **FR-050**: Every confirmation submission MUST carry a client-generated unique operation key.
  Replaying a key whose confirmation actually recorded an operation MUST return the originally
  stored outcome without applying anything again. Retries of the same user attempt MUST reuse the
  same key. A **fully-unchanged** operation stores no operation record and no idempotency record
  (FR-049): repeating it simply re-evaluates and returns the same no-op success again — the client
  may still send a key with it, but no durable replay record exists for a no-op.
- **FR-051**: All conflicts — a stale contribution version, a stale preflight, a duplicate live
  contribution — MUST surface as controlled, recoverable conflict responses with clear Arabic
  messages. Never a partial commit, never a silent overwrite, never an unexplained failure.
- **FR-052**: Every meaningful lifecycle or authored record MUST carry attribution — who created
  it and when, who last updated it and when, and, where removal applies, who removed it and when.
  This covers, at minimum: the workspace, each prepared source, workspace and confirmed
  descriptions, each source contribution, and each recorded operation (its actor and time). Leaf
  relational rows (grouping units, linked-ayah rows, word links, and the manual-verse/override/
  word child rows of a prepared source) inherit ownership and history from their parent aggregate
  and carry no separate attribution of their own.
- **FR-053**: Every confirmation that changes anything MUST be durably recorded as an operation:
  the Door, the acting user, the time, the source and ayah counts, and the outcome summary that a
  replayed key returns. The record is completed as part of the confirmation itself and never
  changes afterwards.
- **FR-054**: After the cutover, the confirmation experience MUST be real end-to-end: the success
  message reflects the actual result, and no simulated/prototype result or messaging remains
  anywhere in the flow.

#### Access control

- **FR-055**: Every linking capability MUST be restricted to the Owner and enforced on the server
  for every request. The acting user MUST always be derived from the authenticated session — never
  from a value in the request.
- **FR-056**: The existing permission catalogue MUST remain untouched: no new permission codes and
  no changes to how existing permissions are evaluated.

#### Presentation

- **FR-057**: The source editor MUST present the complete resolved source as one continuous
  scrollable list with no pagination controls, remaining fluid at 2,000+ ayahs by keeping the
  number of rendered elements bounded. Search, exclusion, select-all, and clear-all MUST operate
  on the complete set.
- **FR-058**: The editor surface MUST have exactly one vertical scrolling region at all supported
  widths.
- **FR-059**: The review step MUST show, per merged ayah, the union of all sources' contributed
  words with provenance naming every contributing source, while descriptions remain listed per
  source.
- **FR-060**: The preflight step MUST show, per source: its classification, its counts, and its
  itemized ayahs — each expandable to its classification, the names of overlapping sources, and
  its exact word and description differences. Invalid items MUST disable confirmation and explain
  why, per item. All classification labels appear in Arabic.
- **FR-061**: Quran text rendering — glyphs, spacing, line metrics — MUST remain unchanged in
  every touched screen, and the project's golden UI checks MUST pass.
- **FR-062**: All user-facing messages introduced by this feature MUST be in Arabic, following the
  product's existing message conventions.

### Key Entities

- **Source descriptor**: The typed definition of one source — its family plus that family's
  parameters (FR-001). The input to identity, resolution, and persistence.
- **Source identity**: The canonical text key computed from a descriptor (FR-002/FR-003). The
  basis of idempotency, result reuse, and the one-live-contribution rule.
- **Resolved source**: The complete validated output of resolving a descriptor: ordered ayahs,
  each with ordered words, matched-word marking, and canonical word identifiers.
- **Workspace**: One per user; the private durable container of prepared sources in order.
- **Workspace source**: One prepared source in a workspace: descriptor, identity, label, order,
  configuration (inclusion choices; the automatic word-match toggle for automatic families;
  user-authored word selections and link shape for the manual family), manual verse set (manual
  family), and per-ayah descriptions.
- **Linking operation**: The durable record of one confirmation that changed something: Door,
  actor, time, counts, outcome summary, and the unique operation key.
- **Source contribution**: What one source has confirmed into one Door — descriptor snapshot,
  label, order, mode, and its children below. At most one live contribution per (Door, source
  identity); removal is reversible at this level in a future stage.
- **Unit**: A grouping of linked ayahs within a contribution (FR-046).
- **Linked ayah / linked word / confirmed description**: The per-ayah rows of a unit, the
  contributed words of each linked ayah (canonical identifiers — user-authored for manual
  sources, derived from resolution for automatic sources), and the ordered descriptions carried
  into the confirmed state.
- **Door, Ayah, Word**: Pre-existing product data. Doors are linked *into*; ayahs and words are
  the canonical Quran data being linked. This feature never modifies any of them.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A curator opens any source — including one matching ~2,000 ayahs — and sees the
  complete ayah list from a single action, where the prototype needed up to 20 sequential partial
  loads.
- **SC-002**: Reopening a source already opened in the same session displays instantly, with zero
  repeated retrieval work and zero network activity.
- **SC-003**: 100% of prepared workspace content — sources, order, configuration, descriptions —
  survives sign-out and reappears on a different browser or device.
- **SC-004**: The reference scenario reproduces exactly: with a Door holding source 1 over ayahs
  A, B, C, preflighting source 2 over A, D, E reports one overlapping ayah (naming source 1) and
  two new ayahs with counts 3 = 2 + 1; after confirming, source 1's stored data is unchanged and
  ayah A is linked via both sources.
- **SC-005**: Zero partial writes: every failed or rejected confirmation leaves stored linking
  data exactly as it was, verified by before/after comparison in acceptance.
- **SC-006**: Zero duplicates under replay and concurrency: replaying a confirmation key or racing
  two identical confirmations never produces a second live contribution or repeated content.
- **SC-007**: Preflight is provably read-only: stored data is identical before and after any
  preflight call.
- **SC-008**: A 2,000-ayah source edits fluidly: one continuous scroll, no pagination controls,
  and the number of rendered elements stays bounded regardless of list length.
- **SC-009**: No user ever sees or affects another user's workspace, verified by a two-actor
  acceptance check.
- **SC-010**: The full manual acceptance matrix in the implementation plan (§14 — rows A1–F4)
  passes against a local environment.
- **SC-011**: All pre-existing explorer and reader behavior is unchanged: existing responses are
  byte-identical and the golden UI checks pass.

## Assumptions

- The implementation-level authority for *how* to build this is
  `docs/abwab-linking-backend-implementation-plan.md` (14 phases, locked decisions, verified
  repository facts). This specification defines *what* must be true and how it is verified; if a
  conflict is discovered, stop and reconcile rather than picking silently.
- This is an internal curation tool with a single privileged role (Owner) and a very small number
  of concurrent users. No approval workflow, notifications, or multi-role review is needed.
- The V2 prototype's user experience is the product reference: the workflow's shape is preserved,
  not redesigned; only the preflight step is inserted and the known presentation gaps are closed.
- The Test Freeze is in force (`TESTING_CONSTITUTION.md`): no automated tests are created or
  modified for this feature. Verification is builds, static and contract gates, manual and browser
  checks, and safe local data inspection — as itemized in the plan's acceptance matrix.
- Underlying Quran and morphology data never changes while the system runs, so time-bounded
  freshness (FR-017) is sufficient and no data-change invalidation is needed.
- Prototype browser-local data is a disposable prototype artifact; users re-prepare their sources
  once after cutover (FR-030).
- Server-side capabilities may ship dark — with no user-visible change — before the presentation
  cutover; the confirmation cutover and the large-list/descriptions presentation land together so
  a real write is never offered behind the old paginated editor.
- Default numeric limits: 10 descriptions per (source, ayah), 2000 characters per description,
  100 prepared sources per workspace, and 3,000 ayahs per resolved source (FR-011) — all
  configurable, defined once alongside the other shared limits.
- All new user-facing text is Arabic, matching the existing product convention.

## Out of Scope

- Viewing a Door's existing links (the Door-links read and its presentation) — deferred until the
  presentation is designed.
- A full audit-history system for linking (event trail beyond the attribution stamps of FR-052).
- New permission codes or a linking permission family — Owner-only via the existing mechanism.
- Distributed or cross-instance result sharing; conditional-request optimizations.
- Surah-level linking — the entire feature is ayah-based.
- Restore/undelete actions for removed contributions (the stored shape permits a future restore;
  no user-facing action is built).
- Review-step virtualization — the review list keeps its current client-side paging at 12.
- Migration of prototype browser-local data (FR-030 explicitly clears it instead).
