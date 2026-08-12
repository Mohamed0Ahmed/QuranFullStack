# Linking source descriptor (Domain)

This folder defines the typed vocabulary of an Abwab **linking source** — the six families a curator
can link ayahs from — plus the word-type scope and selection value objects they build on. It holds no
persistence, no API shape, and no behavior: it is the shape every other Linking layer consumes.

## The descriptor is a closed union, not a bag of nullables

`LinkingSourceDescriptor` is an abstract class with a **private** constructor and six nested sealed
subclasses (`UniqueWord`, `Root`, `Lemma`, `Stem`, `WordType`, `ManualMushafAyahs`). The private
constructor is what closes the hierarchy: no type outside this file can extend it, so a `switch` over
the six cases is exhaustive for every descriptor that can exist.

Each subclass carries only the data its own family has, which is what makes the impossible states
named in the execution plan **unconstructable** rather than merely invalid:

- a Word Type source without a selection — `WordType` requires a non-null `LinkingWordTypeSelection`;
- a manual source with no verses — `ManualMushafAyahs` throws when the normalized verse set is empty;
- a manual source naming a verse outside the Quran — `ManualMushafAyahs` throws on any verse key whose
  surah is not `1–114` or whose ayah is not `1–286`;
- a blank label, or a non-positive dimension id — rejected in the constructor;
- a dimension selection claiming to be a word — `LinkingWordTypeSelection.Dimension` accepts only
  `Root`, `Stem`, or `Lemma`.

These are **classes, not records**, deliberately. A record would synthesize structural equality, and
for `ManualMushafAyahs` that equality would compare `IReadOnlyList<VerseKey>` by reference — two
descriptors naming the same verses would compare unequal. Descriptor equality in this feature is the
canonical identity string (`LinkingSourceIdentity`, in `Application.Abstractions/Linking/`) and its
SHA-256 hash; nothing should compare descriptors structurally. Records would also expose a protected
copy constructor and `with`, both of which can reproduce an instance without re-running validation.

## Manual verse sets are normalized at construction

`ManualMushafAyahs` stores its verses de-duplicated and ordered by `(surah, ayah)`. Normalizing in the
constructor is what makes "input order and duplicates never change the identity" a structural fact
rather than a rule the identity function has to remember.

The range guard runs **before** de-duplication and ordering, so an out-of-range key cannot be hidden by
a duplicate of an in-range one: every key the caller supplied is checked, not merely every key that
survives normalization.

De-duplication compares the **raw** `VerseKey.Value` ordinally and keeps the first occurrence, and the
sort is a stable `OrderBy`/`ThenBy`. Both choices mirror the Frontend's
`orderedUniqueLinkingVerseKeys` exactly — a JavaScript `Set` over the raw strings followed by a stable
`Array.prototype.sort`. The raw string is preserved rather than reformatted from the parsed numbers
because the identity is a byte-exact contract with the Frontend, which does not reformat either.

`VerseKey` is reused from `Quran/Words/` rather than redefined here, and it still validates only that
the key has two positive integer parts — it is shared with the whole Quran area and is not tightened
for Linking. The **Quran range** rule (surah 1–114, ayah 1–286) is therefore enforced here, by
`ManualMushafAyahs` through `LinkingGuard.RequireQuranVerseKey`, so an out-of-range manual descriptor
is unconstructable rather than merely invalid. The strict `^\d{1,3}:\d{1,3}$` digit-shape rule stays
the wire gate's job in `LinkingSourceDescriptorValidation`, because it constrains the *spelling* of a
raw request string, which a constructed `VerseKey` has already discarded.

Three things about that guard matter:

- **It rejects; it never filters.** The Frontend's `orderedUniqueLinkingVerseKeys` silently drops keys
  failing `isVerseKey`, so `[115:1, 1:1]` yields `manual-mushaf-ayahs|1%3A1` there. Dropping a verse the
  curator asked for is the wrong answer on this side; throwing is the right one. Without the guard the
  Domain could mint `manual-mushaf-ayahs|1%3A1|115%3A1` — an identity the Frontend can never produce, and
  exactly the split identity `contracts/source-identity.md` calls the feature's highest-risk failure.
- **It has to live in the constructor, not only at the wire.** The HTTP body mapper and the handler's
  `TryValidate` both already reject this input, so the guard is unreachable over HTTP today. Phases 5–8
  add non-HTTP writers that construct descriptors directly and bypass both gates; the constructor is the
  only place that covers them. This restates the wire rule deliberately — the same argument the
  Abstractions README makes for the other overlapping checks — because `LinkingSourceDescriptorValidation`
  lives in `Application.Abstractions`, which depends on this project and can never be depended upon by it.
- **Leading zeros stay in range and stay accepted.** `002:255` parses to surah 2, ayah 255, passes the
  guard, and is preserved byte-for-byte in the identity as a distinct spelling. That is verified Frontend
  parity, documented in the Abstractions README, and must not be "fixed" here.

## Word type: scope, selection, and the literal `null` case token

`LinkingWordTypeScope` is `(Type, ChildCode, Case, Tense, Voice)` and owns the four token
vocabularies, which are validated on construction:

| Field | Tokens |
| --- | --- |
| `Type` | `noun`, `verb`, `particle`, `inl` |
| `Case` | `all`, `nominative`, `accusative`, `genitive`, `null` |
| `Tense` | `all`, `past`, `present`, `imperative` |
| `Voice` | `all`, `active`, `passive` |

**`"null"` is a real case token and is not the same thing as an absent value.** `Case` is a
non-nullable string that may hold the four-character string `null`, meaning "the caseless bucket";
`ChildCode` is a genuinely nullable `string?`, and absence there renders as an empty part in the
identity. Collapsing the two would silently merge two different scopes onto one identity.

`LinkingWordTypeSelection` lives in the same file as the scope because it always carries one and
draws on the same vocabularies. It is also a closed union, with two cases: `Word` (a tashkeel word id
plus its own context code, case, tense, and voice) and `Dimension` (a root, stem, or lemma id). That
mirrors the execution plan's model — `SelectionKind` plus either a dimension id or the word tuple —
and it is why the identity has 12 parts for a word selection and 8 for the other three.

## Argument guards live in `LinkingGuard`, not on the scope

The shared argument guards — `RequireToken`, `RequireAbsentOrNonBlank`, `RequireNonBlank`,
`RequirePositive`, and `RequireQuranVerseKey` (with the four Quran range bounds it enforces) — are
`internal` members of `LinkingGuard`. They began as static members of
`LinkingWordTypeScope`, which forced every descriptor constructor to reach through the word-type scope
type to check things that have nothing to do with a word-type scope: whether a **source label** is
blank is not a scope concern. `LinkingWordTypeScope` now keeps only what is genuinely its own — its
five fields and the four token vocabularies.

## Enum values are pinned, and never 0

Every enum here starts at `1` with explicit values, matching `AbwabRelationType` and `UserStatus`.
The reason is the same: a missing or unrecognized value deserializes to `0`, which must not be a
legal member, and reordering members must never silently re-map already-persisted rows.

Two different string vocabularies exist for source kind and must never be conflated:

- the **identity string** uses kebab tokens (`unique-word`, `manual-mushaf-ayahs`, …) and is mapped
  in `LinkingSourceIdentity`;
- the future **`source_kind` column** uses snake tokens (`unique_word`, `manual_mushaf_ayahs`, …).

Neither map is defined in this folder; each lives where it is used, so neither can be guessed.

## Related

- Canonical identity, limits, exceptions, validation:
  `../../../application/QuranDashboard.Application.Abstractions/Linking/README.md`
- Identity contract (the byte-exact authority): `specs/001-abwab-linking-backend/contracts/source-identity.md`
- Frontend authority: `Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-key.ts`
