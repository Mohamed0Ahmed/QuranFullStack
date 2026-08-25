# Quranic Data Safety (Conditional Reference)

Conditional reference for source-sensitive or Quran-rendering scope. The canonical owners
are `CODING_PRINCIPLES.md` §10 (source data: no invention or silent correction, provenance,
staged imports, traceability, reports), and the implicated renderer, pipeline code, and source
manifests. The
retained DataImporter README is consulted only for CLI operation and source-package safety. This
file adds only the cross-area safeguards below; each consumer keeps its own severity and application
wording.

1. **Never trade Quran data safety for performance or convenience.** No optimization or
   "make it run" shortcut may weaken text integrity, source hashes or manifest checks,
   source-unchanged checks, validation hard checks, rollback/atomicity, report correctness,
   or provenance. "Slower but correct" is the right answer for this product.

2. **Never hide missing or unknown data.** Show a controlled empty / unknown / loading
   state instead of a plausible-looking fallback. Loading is not empty; unknown is not
   zero; no hardcoded parallel copy that can drift from the declared source of truth.

3. **Preserve Quran text/glyph readability and RTL semantics in any change.** Do not
   reduce Quran text readability, contrast, or sizing; do not break text
   selection/highlight semantics or RTL layout correctness; do not swap the correct Mushaf
   font/rendering for a "lighter" one that mis-renders glyphs or marks; do not animate or
   transition Quran glyphs; respect reduced-motion for Quran content; keep Quran-related
   actions accessible.

4. **Report uncertainty instead of guessing.** When source truth is unclear, report the
   uncertainty and recommend verification against the source — never guess, fill, or
   assert a value to look complete.
