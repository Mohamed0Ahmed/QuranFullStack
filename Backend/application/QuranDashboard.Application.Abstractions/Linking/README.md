# Linking contracts (Application Abstractions)

This folder defines the shared Abwab Linking contracts every Linking story consumes: the canonical
source identity and its hash, the identity token vocabulary, the four product limits, the descriptor
well-formedness gate, the manual-ayah completeness proof, the source-resolution reader abstraction with
its response DTOs, and the five exception types the API boundary translates. It contains no reader
*implementation*, writer, cache, endpoint, or DI registration — those live in Infrastructure and the
API, and the cache arrives in a later phase.

## Source resolution: the reader port and its DTOs

`ILinkingSourceResolutionReader.ResolveAsync(descriptor, ct)` is the one port that turns a validated
descriptor into `Responses/LinkingResolvedSourceDto` — `sourceIdentity`, `resolvedAtUtc`,
`totalAyahCount`, and the ordered `ayahs`, each with its ordered complete `words` list and its
`matchedQuranWordIds`. The shapes are field-exact to
`specs/001-abwab-linking-backend/contracts/linking-sources-api.md`; the wire truth is the regenerated
`openapi/swagger.json`.

The port communicates failure by exception rather than an outcome union, because the two failures are
both exceptional for a *validated* descriptor: a referenced dimension that does not exist
(`LinkingSourceNotFoundException` → `404`) and a descriptor that is well-formed but cannot be resolved
within the product limits (`LinkingInvalidDescriptorException` → `400`). The handler translates both
into `ResolveLinkingSourceOutcome`, so the exception types never reach the API boundary and the Phase 4
cache decorator can wrap the port without re-shaping a union.
`LinkingSourceNotFoundException` exposes its `Reference` as a property so the boundary can log which id
failed without parsing the English `Message`.

## `LinkingSourceTokens` — one kebab map, two directions

The kebab tokens for source kind (`unique-word`, `manual-mushaf-ayahs`, …), unique-word mode, and
word-type selection kind live here **once**, with a forward map for rendering and an inverted map for
parsing. `LinkingSourceIdentity` renders through it and the API request-body gate parses through it.

Before Phase 3 the kind map was private to `LinkingSourceIdentity`. Adding the JSON discriminator meant
either a second hand-written copy of the six tokens or a single shared map; a second copy could drift
from a byte-exact contract silently, and nothing would catch it, so the map was promoted instead. The
promotion is a pure move — the identity algorithm is unchanged and the eight worked examples in
`contracts/source-identity.md` were re-verified byte-for-byte afterwards.

## `LinkingSourceIdentity` — a byte-exact contract with the Frontend

`LinkingSourceIdentity.For(descriptor)` must produce a string **byte-identical** to the Frontend's
`linkingSourceKey(source)`. A silent divergence would split the resolution cache and break workspace
idempotency, because identity equality is what "the same source" means across the whole feature. The
authority is `specs/001-abwab-linking-backend/contracts/source-identity.md`, read from
`Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-key.ts`.

The algorithm: encode each part, then join the encoded parts with a literal `|`. The separator is
applied after encoding and is never itself escaped. A null or absent part encodes to the empty
string, which is why consecutive and trailing pipes appear and are significant. Numbers render in
invariant decimal. **The label is never part of the identity** — it is a display snapshot that may be
renamed without changing what the source is.

### The escape set is the trap

Parts use the **JavaScript `encodeURIComponent`** escape set, which leaves
`A–Z a–z 0–9 - _ . ! ~ * ' ( )` unescaped. .NET's `Uri.EscapeDataString` follows RFC 3986 and
additionally escapes five of those: `! ' ( ) *`. `EncodePart` therefore calls `Uri.EscapeDataString`
and then converts `%21 %27 %28 %29 %2A` back to `! ' ( ) *`.

A raw `Uri.EscapeDataString` port would pass every check built from digits and plain tokens and
diverge only on codes containing those five characters — which is exactly why the rule is implemented
deliberately instead of being discovered later. The un-escaping is unambiguous: after escaping, every
`%` in the output begins a real two-hex-digit escape, so the three-character sequence `%2A` can only
ever be an escaped `*` and never a literal `%2A` from the input (that escapes to `%252A`).

Both encoders emit uppercase hex over UTF-8 bytes, so nothing else needs adjusting.

### Part order per family

| Family | Parts | Count |
| --- | --- | --- |
| Root | `root`, rootId | 2 |
| Lemma / Stem | kind, id, typeCode (absent → empty) | 3 |
| Unique Word | `unique-word`, mode, wordId | 3 |
| Manual Mushaf | `manual-mushaf-ayahs`, then each verse key | 1 + N |
| Word Type — word selection | `word-type`, `word`, tashkeelWordId, contextCode, case, tense, voice, then the five scope parts | 12 |
| Word Type — root/stem/lemma selection | `word-type`, selection kind, id, then the five scope parts | 8 |

The five scope parts are always `type`, `childCode` (absent → empty), `case`, `tense`, `voice`.
Manual sources contribute only their verse-key set, already de-duplicated and ordered by
`(surah, ayah)` by the descriptor itself.

### The hash

`HashOf` returns the **32-byte SHA-256 digest of the UTF-8 bytes of the identity string, verbatim** —
no normalization beyond the canonical algorithm above. This is the future `source_identity_hash`
column. Uniqueness boundaries use the hash rather than the raw text because a manual identity grows
without bound with its verse set and would overflow a btree index entry; the raw `source_identity` is
still stored and compared as the final equality guard on collision-sensitive paths. This is a storage
decision only — it does not change identity semantics.

### Leading-zero verse keys are a deliberate parity artifact

`["2:255", "002:255"]` passes validation — both spellings match `^\d{1,3}:\d{1,3}$` and both resolve to
surah 2, ayah 255 — and produces `manual-mushaf-ayahs|2%3A255|002%3A255`. Two spellings of one verse
therefore yield an identity that does not dedupe against `manual-mushaf-ayahs|2%3A255`, and
`source_identity_hash` uniqueness will not collapse them.

**The shipped Frontend behaves identically**, verified against `utils/linking-verse-order.ts`: its `Set`
de-duplicates the **raw** strings, and its comparator ties on equal `(surah, ayah)` under a stable sort,
so it also emits both spellings in input order. The Backend is parity-correct exactly as written, and
canonicalizing verse keys here alone would **break** the byte-exact contract rather than fix anything.

Whether verse keys should be canonicalized at all is a **contract-owner decision** for
`specs/001-abwab-linking-backend/contracts/source-identity.md`, applied to both sides in one change. It
is not an implementation choice, and must never be "fixed" on one side only.

## `LinkingSourceDescriptorValidation` — the wire-boundary gate

`TryValidate` reports descriptor well-formedness as a message instead of an exception. It takes an
**already-constructed** `LinkingSourceDescriptor`, which is exactly why the ordering of the two gates
matters:

1. **Domain construction is the first gate, and it throws.** A blank label, a non-positive id, a token
   outside its vocabulary, and an empty manual verse set are all rejected by the constructors in
   `Domain/Linking/` with `ArgumentException` / `ArgumentOutOfRangeException`. A caller that hands
   untrusted input straight to a constructor must still catch, because construction happens before
   `TryValidate` can be reached.
2. **`TryValidate` is the wire-boundary gate.** Its check set is applied to the request **body** —
   *before* a descriptor is constructed — so the endpoint answers a controlled `400` naming the
   offending field instead of catching `ArgumentException` out of a constructor. Every check in the set
   becomes reachable at that point.

That body-shaped path is why the individual checks are `public` rather than private helpers:
`IdentifierError`, `TokenError`, `RequiredTextError`, `OptionalTextError`, and `VerseKeyError` each take
a **raw** value and a field name, so `Api/Contracts/Linking/LinkingSourceDescriptorBodyMapper` can run
them against the un-typed body and return a `LinkingDescriptorViolation` carrying the offending field.
`VerseKeyError` deliberately takes a `string?` and parses the digits itself rather than accepting a
`VerseKey`, because constructing a `VerseKey` is itself a throwing operation that the gate exists to
prevent. `TryValidate(descriptor, …)` keeps its original descriptor-shaped behaviour unchanged and is
still run by the handler as a second, defence-in-depth gate before the reader — and therefore before
`LinkingSourceIdentity` — is reached.

## `LinkingManualAyahCompleteness` — the proof, pure

`Verify(requestedVerseKey, ayah)` returns `null` when the verse is complete and a
`LinkingDescriptorViolation` naming that exact verse key when it is not. It checks that the ayah exists
and its `verse_key` matches the requested spelling, that the non-marker `word_number`s are contiguous
`1..N`, that `N == quran_ayahs.words_count_real`, and that every non-marker `location` is unique with a
matching `(surah, ayah)` prefix. It takes already-loaded rows (`LinkingManualAyah` /
`LinkingManualAyahWord`) and touches no database, so the rule is testable by inspection and the reader
owns only the fetching. Any failure blocks the **whole** resolution — a partial ayah is never published.

The overlap between the two is **deliberate, not redundant**: the same rule has to hold at the wire (as
a returned failure the endpoint can shape) and in the Domain (as a state that cannot be constructed at
all). Applied to an already-constructed descriptor — the only caller shape that exists today — the
verse-key rules are the only checks that can still fire, because construction rejected the rest first.
That is not a reason to drop the others: removing them would leave the Phase 3 body gate incomplete and
push those cases back to a caught `ArgumentException`.

The verse-key rule is split across the two gates, and the split is exact. `VerseKey` itself accepts any
two positive integer parts, while a linking verse key must additionally match `^\d{1,3}:\d{1,3}$` with
surah 1–114 and ayah 1–286.

- The **range** half now has a Domain counterpart: `LinkingSourceDescriptor.ManualMushafAyahs` rejects
  any out-of-range key through `LinkingGuard.RequireQuranVerseKey`, so `115:1` is unconstructable rather
  than merely reportable. That closes the split-identity hole for the Phase 5–8 writers that construct
  descriptors without passing through this gate. The restatement is deliberate for the same reason the
  other overlaps are: `Application.Abstractions` depends on `Domain`, never the reverse, so the bounds
  cannot be shared as one symbol — they are held as `LinkingGuard`'s four internal constants and as this
  class's four public ones, and the two must be changed together.
- The **digit-shape** half has no Domain counterpart and needs none: it constrains the spelling of a raw
  request string, which a constructed `VerseKey` has already discarded. The digit-run check is not
  redundant with the parsed numbers — `int.TryParse` accepts leading whitespace and a sign, which the
  Frontend's regex does not.

### `TryValidate` must run before `LinkingSourceIdentity`

**Binding requirement on Phase 3: call `TryValidate` before `LinkingSourceIdentity.For` or
`HashFor` on any externally-sourced descriptor.** Byte-exact Frontend parity is guaranteed only for
**valid** descriptors, because the two sides treat an invalid verse key differently by design. The
Frontend's `orderedUniqueLinkingVerseKeys` filters out keys failing `isVerseKey` before building the
key, so a manual source containing a bad key silently loses it there while the Backend descriptor keeps
every key it can construct.

The out-of-**range** case no longer reaches this point: the Domain constructor rejects `999:999`
outright, so no descriptor and therefore no identity exists to diverge. The requirement still binds on
the **digit-shape** case, which the Domain deliberately does not police — `VerseKey` parses with
`int.TryParse`, so a spelling like `" 2:255"` constructs and yields
`manual-mushaf-ayahs|%202%3A255`, while the Frontend's regex discards it entirely.

The Backend deliberately does **not** filter, at either gate. Silently dropping a verse the curator asked
for is the wrong answer; rejecting is the right one, and `TryValidate` already rejects this exact case.
The residual divergence is fenced off by ordering the two calls, never by copying the Frontend's filter
into the descriptor.

## `LinkingLimits` — exactly four numeric limits

`MaxDescriptionsPerSourceAyah = 10`, `MaxDescriptionLength = 2000`, `MaxResolvedAyahs = 3000`,
`MaxPreparedSources = 100`.

**These four are the only numeric product limits in the feature.** There is deliberately no
per-operation ayah cap, no per-operation source cap, and no cap on a manual source's verse-set size:
earlier drafts proposed them and they were withdrawn as unapproved product rules. The structural
requirements (at least one source per operation, at least one included ayah per submitted source) are
shape rules, not numeric limits, and do not belong here. A transport or request-size limit, if one is
ever needed, must be raised as its own decision rather than added silently as a product rule.

## Exceptions

Five types, all `sealed`, all deriving directly from `Exception`, following the `Abwab/` precedent in
the sibling folder: **every type owns its own wording**, and the ones that carry data take a
**structured payload** and compose the message from it — as `AbwabRelationDuplicateException` and
`AbwabTemplateApplyCollisionException` do.

`LinkingSourceNotFoundException` (unknown dimension id → `404`) names the reference it could not
resolve. `LinkingInvalidDescriptorException` (→ `400`) carries a `LinkingDescriptorViolation`: a
`LinkingDescriptorViolationCode` plus the offending `Field` and `Value`. The three codes are the three
`400` causes in `contracts/linking-sources-api.md` §Status mapping — `MalformedDescriptor`,
`ResolvedAyahLimitExceeded`, and `ManualAyahCompletenessFailed`, the last of which puts the exact verse
key in `Value` because the contract requires the response to name it. `LinkingStaleVersionException`,
`LinkingDuplicateContributionException`, and `LinkingPreflightStaleException` are the three `409`
concerns and need no payload.

### Why the descriptor exception refuses a prose message

It previously took a free-form `reason` string, and the only strings available to pass were the English
diagnostics returned by `LinkingSourceDescriptorValidation`. `API_GUIDELINES.md` §Localization requires
user-facing `message` values to be localized — **Arabic by default** — and
`contracts/linking-sources-api.md` specifies Arabic messages, so forwarding `reason` into the response
envelope would have shipped English `400`s. A structured payload makes that mistake unavailable.

**The Arabic wording is produced at the API boundary in Phase 3, keyed off `Code`**, with `Field` and
`Value` interpolated into the localized template. No localization code lives in this folder and none
belongs here. The exception's own `Message` is an English developer diagnostic for logs and traces: it
is not the wire message and must never be copied into one. The same restriction applies to the strings
`TryValidate` returns.

Every Linking writer save must translate `DbUpdateConcurrencyException` to
`LinkingStaleVersionException` and PostgreSQL `23505` to `LinkingDuplicateContributionException`;
an untranslated save that reaches the global handler as a `500` is a defect.

## Related

- Descriptor and word-type value objects: `../../../domain/QuranDashboard.Domain/Linking/README.md`
- Reader implementation and its query-shape invariants:
  `../../../infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/README.md`
- Identity contract: `specs/001-abwab-linking-backend/contracts/source-identity.md`
- Resolution API semantics: `specs/001-abwab-linking-backend/contracts/linking-sources-api.md`
