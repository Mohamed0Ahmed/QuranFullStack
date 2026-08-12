# Abwab Linking source-resolution cache

**Layer:** Infrastructure · caching decorator · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`

## What this area does

Makes a repeat resolution of an already-seen linking source cost **zero database commands**, while
keeping the wire response indistinguishable from a cold one. `CachedLinkingSourceResolutionReader`
decorates `EfLinkingSourceResolutionReader` behind `ILinkingSourceResolutionReader`; the endpoint,
handler, DTO, ordering, marker rules, and error behaviour are unchanged.

## Key pieces

- `LinkingResolvedSourceCompact.cs` — the cached value. Per ayah `{ayahId, quranWordIds[],
  matchedQuranWordIds[]}` plus the ordered ayah-id list. **No Uthmani text and no display
  metadata**; those come from `LinkingAyahTextCache`.
- `LinkingAyahTextCache.cs` — ayah-keyed Uthmani text + display metadata, shared by every source
  that touches that ayah, so overlapping sources hydrate from one copy.
- `LinkingSourceCacheKeys.cs` — the descriptor-only key and the ayah-text key.
- `LinkingSourceCacheEntryOptions.cs` — expiration and both size limits, bound from `appsettings`.
- `LinkingSourceResolutionCache.cs` — the dedicated `MemoryCache` and the single-flight gate.
- `CachedLinkingSourceResolutionReader.cs` — split on the way in, rehydrate on the way out.

## The three deliberate divergences — do NOT "harmonize" these

These differ from the surrounding caching code **on purpose**. Each one is load-bearing; changing
it back reintroduces the exact defect it was chosen to avoid.

### 1. A dedicated `MemoryCache`, never the shared `IMemoryCache` (repo fact F7)

`LinkingSourceResolutionCache` and `LinkingAyahTextCache` each construct their own
`new MemoryCache(new MemoryCacheOptions { SizeLimit = … })`. Every other cache in this tree uses
the shared `IMemoryCache` that `AddMemoryCache()` registers (three times) as a **size-less**
singleton.

A `SizeLimit` on the shared instance would make **every existing size-less `Set` call in the
codebase throw** — `MemoryCache` requires an entry `Size` on every entry once a `SizeLimit` exists.
So Linking must own separate instances. Do not add `SizeLimit` to the shared instance, do not
migrate these two caches onto it, and do not add an entry `Size` to any existing `Set`.

Entry `Size` is the **resolved ayah count**, so the limit is expressed in ayahs and is directly
reasonable to reason about. `LinkingAyahTextCache` uses `Size = 1` per ayah, so its limit is
literally "how many ayahs of text may be resident".

### 2. `Task<T>` stored in the entry, never `CacheLoadGate` (repo fact F8)

The cache stores `Task<LinkingResolvedSourceCompact>`, not the value. The entry itself is the
single-flight gate: concurrent identical requests observe the same in-flight task and collapse to
**one** database load.

`CacheLoadGate` is the repo's other single-flight helper and it is **forbidden here**. Its own
comment says so: it holds a `SemaphoreSlim` per key for the process lifetime and must not be reused
"for unbounded or caller-supplied keys without adding eviction". Linking source keys are
caller-supplied and combinatorial (every root × lemma × stem × word-type scope × manual verse set),
so gates would accumulate without bound. A task in the entry is evicted with the entry, so the gate
lifetime is the entry lifetime, automatically.

Failure handling is part of the same rule: a **faulted or cancelled task is removed from the cache
immediately**, so the next caller retries instead of being served a cached failure. A caller whose
own token is not cancelled retries the load itself rather than surfacing another caller's
`OperationCanceledException` — the same per-caller semantics `CacheLoadGate` documents.

**The in-flight task outlives the request that started it, and that has one more consequence than
cancellation.** The entry lives in a **singleton** cache, but the `load` closure stored in it captures the
**scoped** `EfLinkingSourceResolutionReader` and `DbContext` of whichever request happened to start the
load. If that request's DI scope is disposed mid-load — a client disconnect or an aborted request — the
load faults with `ObjectDisposedException`, and every other caller awaiting the same shared task used to
receive it too and answer `500` to a request that did nothing wrong. So the recovery is now the same one a
foreign cancellation already had: a waiter that observes a **shared** load fail this way retries, and once
the attempt budget is spent falls through to loading via its **own** reader. `GetOrStartAsync` reports
whether the caller initiated the task it returned (`initiatedHere`); the recovery is conditioned on that
flag, so a caller whose **own** scope died still surfaces its `ObjectDisposedException` rather than
retrying two more doomed loads against the same dead `DbContext`. The catch is deliberately narrow: any
other shared failure — including `LinkingSourceNotFoundException` — still propagates to the waiter
unchanged. Measured on the fixed code, with the initiator's load throwing `ObjectDisposedException`: the
initiator surfaces it, the waiter returns a correct value after exactly **one** invocation of its own
loader. With the initiator throwing `InvalidOperationException` instead, the waiter still propagates it and
invokes its own loader **zero** times — the two runs differ only in the exception type, which is what shows
the pre-fix path was propagation.

**Every eviction is ownership-checked.** No failure path calls `_cache.Remove(key)` outright; they
all go through `RemoveIfCurrent`, which removes the entry only when the task sitting at the key is
reference-identical to the task that failed, under the same lock that installs entries. This
matters because an in-flight entry reserves `MaxResolvedAyahs` against the size budget, so above
roughly twenty concurrent distinct in-flight loads size-pressure compaction can drop an in-flight
entry while its load is still running. A later caller then installs a *different* task under the
same key, and an unconditional remove would delete that innocent entry — either a good warm value
(a spurious miss) or another caller's in-flight gate (a lost single-flight, so a third caller
starts a third load). No wrong data is served either way, but the protection this cache exists to
provide would be silently dropped under exactly the burst load it is meant to absorb. The check
applies to all three failure paths: exception, cancellation, and the identity-mismatch eviction
described below.

`MaxSharedLoadAttempts` (2) bounds the whole retry loop, and identity mismatches, cancellation retries
and disposed-scope retries **share that one budget**. Once it is exhausted the caller stops contending for
the entry and resolves directly through the loader, returning a correct but **uncached** result rather than
looping.

**The whole entry lifecycle runs under `_pendingLock`** — the initial `Set` that installs an
in-flight task, the success `Set` that replaces it with its real size, and `RemoveIfCurrent`. The
success `Set` is inside the lock specifically so that `RemoveIfCurrent` cannot read "the entry is
still mine", have a concurrent success `Set` install a freshly-loaded good value underneath it, and
then delete that innocent value. `completion.SetResult` stays **outside** the lock — a task is never
completed while a lock is held — and nothing is awaited inside it. No `PostEvictionCallback` is
registered on these entries (none exists anywhere in the Backend), so `Set` cannot re-enter this
lock through an eviction callback.

While a load is in flight the entry reserves `MaxResolvedAyahs` of the size budget (the true worst
case, since the real count is unknown until the load returns); on success the entry is replaced
with its actual ayah count, **floored at 1**.

The floor is required by **this project's own guard**, not by the framework:
`LinkingSourceCacheEntryOptions.Entry` validates its `size` argument with
`ArgumentOutOfRangeException.ThrowIfNegativeOrZero`. `MemoryCache` itself is happy to store an entry
with `Size = 0` under a `SizeLimit` (measured: the entry is stored and retrievable); it rejects only
a **negative** `Size`, and separately a **missing** one. So the floor is load-bearing for a real
reason — a source that resolved to zero ayahs would otherwise throw inside the success path and
fault an otherwise-valid load. Keep it.

### 3. No user and no Door in the key — this is what makes sharing correct

The key is `linking:source:v1:{kind}:{sha256(EncodePart(canonicalScope))[..16]}`, derived **only**
from the typed `LinkingSourceDescriptor`. The `EncodePart` wrapper is **not** cosmetic and the hash
is **not** taken over the raw identity — see the escaping note below before reproducing a key.

**Never in the key or the value**: user, Door, ayah inclusion/exclusion, selected words,
descriptions, workspace membership, preflight or confirm state, or the display `label`. A resolved
source is pure Quran/morphology truth — the same descriptor resolves to the same ayahs for every
actor — so the entry is safe to share across actors, and sharing is the entire point. Adding a user
to the key "to be safe" would destroy all reuse between the very requests this cache exists to
serve, without improving safety by any measure. Authorization is enforced at the endpoint
(`[RequireOwner]`), which is where it belongs; the cache is not an authorization boundary.

The canonical scope fed to the hash is `LinkingSourceIdentity.For(descriptor)` — the same
byte-exact canonicalizer that defines source equality everywhere else in this feature (and is
byte-exact with the Frontend). Deriving the key from it means *same identity ⇔ same key* by
construction, so a second canonicalizer can never drift away from the first one. That identity
string is then hashed by **calling** `WordTypesCacheKeys.HashParts` (escape `\` and `|`, join on
`|`, SHA-256, first 8 bytes as lowercase hex) — the one routine, not a copy of it.

`HashParts` was widened `private` → `internal` for this: a **visibility change only**, with the
signature untouched and no existing call site rewritten; nothing became `public`. (Phase 3's
`EfWordTypesReader.MatchedMorphologyQuery` is a related but distinctly *larger* precedent — it went
`private` → `internal` **and** instance → `static` **and** gained a `QuranDashboardDbContext`
parameter, rewriting both of its existing call sites. Do not cite it as an equivalent minimal
widening.)

**The escaping is not inert here — it changes the hashed bytes for every source kind.** The
intuition that one already-percent-encoded part cannot be altered is wrong, because
`LinkingSourceIdentity.Join` percent-encodes each *part* but then joins the parts with a **raw
`|`**. Every identity string therefore contains separator pipes — one for a Root, seven for the
Word Type dimension form below, more for a Word selection or a long manual verse set — and
`WordTypesCacheKeys.EncodePart` re-escapes each of them to `\|` before hashing. Measured
for these sample descriptors, the emitted suffix always equals `sha256(escaped)`, never
`sha256(raw)`:

```
kind                 identity (raw)                          sha256(raw)[..16]  emitted = sha256(escaped)[..16]
unique-word          unique-word|simple|123                  0009a034b8cf34db   ccfce7331b54df49
root                 root|4                                  7e05e033ac8d60e3   751c97de2a9e6226
lemma                lemma|12|noun                           792ecdba541f4a5e   9bce44c82bea68e1
stem                 stem|7|verb                             af24a10a446a4381   70c8b4163c4b8bdb
word-type            word-type|root|4|verb||all|past|active  df1896b8357740fe   2c8711aba4fee9b0
manual-mushaf-ayahs  manual-mushaf-ayahs|2%3A255|3%3A18      5db05c1e09520072   de1b2578c6d02345
```

The escaping is harmless and deterministic — it is a pure function of the identity, so *same
identity ⇔ same key* still holds by construction. But **anyone reproducing a key outside this code**
(a debug tool, a Frontend parity check, a manual cache probe) must apply the same `\`-then-`|`
escaping first; hashing the raw identity yields a completely different key, as every row above
shows.

This is also the real argument for **calling** the shared routine instead of hand-rolling a second
one. The escaping is not riding along for tidiness — it materially determines the key — so a local
copy would be a second implementation of a rule that actually affects the output, free to diverge
the moment the escaping rule changed for one caller and not the other.

Because 16 hex characters is a 64-bit truncation, the **full identity is also stored in the cached
value and compared on every hit**; a mismatch evicts and reloads rather than cross-serving, subject
to the ownership check and the attempt limit described in §2. That
mirrors the `source_identity` + `source_identity_hash` pattern the database uses (research R20):
the hash is for lookup, the raw text is the final equality guard.

## Invariants / caveats (read before changing)

- **The decorator must stay wire-invisible.** Same DTO, same ordering, same per-family marker
  rules, same exceptions. Ayah order comes from the compact value's ordered ayah list and word
  order from its per-ayah `quranWordIds`, both captured from the EF reader's output, so the
  ordering contract (spec FR-006) is preserved verbatim. Verified by serializing a cold and a warm
  response and comparing them byte-for-byte **apart from the deliberately re-stamped
  `resolvedAtUtc`** (next bullet): for the 1,879-ayah root the payloads are identical across all
  **4,940,652 bytes** that remain once that single field is removed.
  **The raw payloads are not equal, and must not be expected to be.** They differ inside
  `resolvedAtUtc`, and even their total lengths can differ by a byte as the timestamp's trailing
  fractional digits vary (4,940,704 vs 4,940,703 on one observed pair; the field itself is 52
  characters including its trailing comma). If you re-run this check, normalise `resolvedAtUtc`
  first — a raw comparison is *supposed* to fail. Do **not** "fix" a failing raw comparison by
  echoing the cached timestamp: that would reintroduce the one wire-observable cache tell this
  whole design exists to avoid.
- **`resolvedAtUtc` is stamped fresh on every response, warm or cold.** A warm response that echoed
  the original timestamp would be the one wire-observable difference between cached and uncached —
  exactly what "indistinguishable" forbids. Nothing depends on it being the moment SQL ran: it is
  deliberately excluded from the preflight token so re-resolution can never stale a preflight
  (research R8). The compact value therefore stores no timestamp.
- **Marker rules survive because the compact stores the resolved word ids, not a rule.** The
  per-family split (Root/Lemma/Stem/Word Type marker-free; Unique Word and Manual Mushaf carrying
  markers) is already baked into the word ids the EF reader returned. `LinkingAyahTextCache` keeps
  the **union** of every word ever seen for an ayah, so a marker-free source and a marker-bearing
  source over the same ayah share one text entry and each still projects exactly its own word list.
- **Hydration is all-or-nothing.** If any ayah's text, or any single word id within it, is missing
  from `LinkingAyahTextCache`, the decorator falls back to a full database resolution instead of
  returning a partial answer. This is what makes the union-merge safe, and it is why the ayah-text
  limit is set above the whole corpus.
- **No write-driven invalidation, and no `quran_data_generation` marker** (locked decision).
  Nothing in this API mutates Quran or morphology data. A restart clears both caches. The absolute
  expiry is the backstop that guarantees no entry is fresh forever.
- **Merging ayah text never extends an entry's absolute deadline.** A merged entry keeps the
  original entry's absolute expiration, so repeatedly touching a hot ayah cannot keep stale text
  resident past its 4-hour bound.
- **The ayah-text merge is atomic.** `Store` is a read-modify-write — read the existing entry,
  union its words with the incoming ones, write the result back — so the merge runs under a
  per-ayah lock: a fixed 64-way stripe over `ayahId`, keeping the lock set bounded, since the F8
  objection to unbounded per-key gates applies to locks just as much as to semaphores. Without it,
  two threads storing the same ayah from different families each merge onto the same base and the
  last writer silently drops the other's words. That is self-healing — the missing word id makes
  `Hydrate` return `null`, forcing a full re-resolve that re-stores the union — but the
  cross-family union is exactly what lets one shared text entry serve a marker-free and a
  marker-bearing source at once, so it is enforced rather than left to heal. The lock covers only
  the merge; the already-covered fast path reads without taking it.

## Sizing — measured, not guessed

Measured on the canonical database (8 large sources: roots 4/16/19/45/25/1 plus two Word Type
scopes over root 4 — 6,364 ayah-slots, 3,311 distinct ayahs, 118,650 word-slots):

| Form | Managed heap | Per unit |
| --- | --- | --- |
| Full `LinkingResolvedSourceDto` (**not** cached) | 11.20 MB | 1,846 B per ayah-slot |
| Compact value | 1.30 MB | **214 B per ayah-slot** |
| Ayah text (deduplicated) | 7.61 MB | **2,411 B per distinct ayah** |

The 1,879-ayah root serializes to **4,940,704 bytes** of JSON (≈4.7 MiB; the managed-heap figures
above are MiB), and the 3,000-ayah `MaxResolvedAyahs` cap implies roughly 7–8 MB — which is why the
full DTO is never the cached value.

Defaults, and what they bound:

| Setting | Default | Worst case it permits |
| --- | --- | --- |
| `ResolvedSourceSizeLimitAyahs` | 60,000 ayah-slots | 12.3 MB — 20 maximum-size (3,000-ayah) sources, or ~31 sources the size of root 4 |
| `AyahTextSizeLimitAyahs` | 6,500 ayahs | 14.9 MB |
| **Total** | | **≈27 MB** |

`AyahTextSizeLimitAyahs` is deliberately set just **above the whole Quran** (6,236 ayahs), so size
pressure can never evict ayah text — only expiry can. That is what keeps the all-or-nothing
hydration fallback from firing in practice, and it means the text half of the budget saturates at
the corpus instead of growing with the number of cached sources.

`SlidingExpiration = 30 min` keeps a working set alive across a session;
`AbsoluteExpirationRelativeToNow = 4 h` is the locked requirement that a hot entry can never stay
fresh forever. Both are bound from the `LinkingSourceCache` section of `appsettings`, following
`MushafReaderOptions`' precedent.

## Related

- The reader being decorated, and the ordering/marker invariants it establishes:
  `../../Persistence/Reads/Linking/README.md`.
- Registration (F13 decorator convention): `../../DependencyInjection/LinkingDependencyInjection.cs`.
- Decisions and measurements: `specs/001-abwab-linking-backend/research.md` **R11** (this cache's
  decision record — dedicated instance, `Task<T>`-in-entry, descriptor-only key) and **R20** (the
  `source_identity` + `source_identity_hash` pattern the identity guard mirrors);
  `docs/abwab-linking-backend-implementation-plan.md` §Phase 4 and repo facts F7 / F8 / F13.
- **R19 is not about this cache.** It is the *Frontend* Angular session cache cap, whose class is
  also named `LinkingSourceCache` — the same string as the `appsettings` section above. Different
  layer, different decision, none of these measurements; do not cite it here.
- Wire contract: `specs/001-abwab-linking-backend/contracts/linking-sources-api.md`.
