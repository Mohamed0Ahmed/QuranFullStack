# Quranic Data Safety (Shared Reference)

Single source of truth for the Quran data-safety rules shared across the Quran
Dashboard review skills: `engineering-review`, `performance-backend-review`,
`performance-angular-review`, and the Spec Kit add-on
(`SPEC_KIT_IMPLEMENTATION_REVIEW.md`). Those skills point here for the core rules and
keep only their own severity scale and stack-specific framing.

This product curates Quran source data. Source-sensitive data is the
**highest-priority safety area**. Correctness, provenance, and readability always win
over convenience, cleverness, or speed.

## The rules

1. **Never invent Quran text or data.** Do not fabricate or hallucinate ayah text,
   word text, roots, lemmas/stems, morphology/i3rab, tafsir, translations, counts,
   statistics, or gates. If a value is not in the source, it does not exist.

2. **Never silently correct Quran text or data.** Do not "fix", normalize, or adjust
   source-sensitive data in frontend or backend without explicit, traceable handling.
   A silent correction with no trace is a defect even when it looks more "right".

3. **Never hide missing or unknown data.** Show a controlled empty / unknown / loading
   state instead of masking absent data or substituting a plausible-looking fallback.
   Loading is not empty; unknown is not zero.

4. **Never drop traceability / provenance / source checks.** Preserve source and
   traceability metadata, source hashes, manifest and source-unchanged checks,
   validation hard checks, report gates, and rollback / atomicity for imported or
   generated data. No partial-state imports; no hardcoded parallel copy that can drift
   from the declared single source of truth.

5. **Never trade Quran data safety for performance.** No optimization may weaken text
   integrity, provenance, validation, atomicity, or report correctness. If something
   cannot be made faster without touching one of these, say so plainly and stop —
   "slower but correct" is the right answer for this product.

6. **Preserve Quran text/glyph readability and RTL semantics.** Do not reduce Quran
   text readability, contrast, or sizing; do not break text selection / highlight
   semantics or RTL layout correctness; do not swap the correct Mushaf font / rendering
   for a "lighter" one that mis-renders glyphs or marks; do not animate or transition
   Quran glyphs; respect reduced-motion for Quran content; keep Quran-related actions
   accessible.

7. **Report uncertainty instead of guessing.** When source truth is unclear, report the
   uncertainty and recommend verification against the source — never guess, fill, or
   assert a value just to look complete.

## Severity

Any violation is a high-priority safety issue. Each skill applies its own severity
scale:

- `engineering-review` and the Spec Kit add-on: treat as **BLOCKING** or **MAJOR**
  depending on impact.
- `performance-backend-review` and `performance-angular-review`: a performance or
  visual recommendation that trades away any rule above is itself the defect — never
  propose it, and flag it if the diff already makes the trade.
