"""Build the FINAL curated segment-stem artifact from the reviewed decision.

Pure reduction of segment-stem-curation-candidates.json + the DB-derived clean-stem map.
Decision (this feature):
  - 4 named cases stay UNRESOLVED (reviewed_stem_id = null, explicit reason): no new stems,
    no remap now.
  - every other secondary candidate is APPROVED to its clean/de-shadda stem from the matrix.
  - head / word-level stem is separate and unchanged (not represented here).
No DB writes, no code/migration/importer changes.
"""
import json
from collections import OrderedDict

DIR = "/projects/Dashboard/App/docs/feature-018-segment-stems-and-stems-explorer/"
CANDS = DIR + "segment-stem-curation-candidates.json"
CLEAN_MAP = DIR + "sql/clean_stem_map.json"
JSON_OUT = DIR + "segment-stem-corrected-arabic.json"

# Intentional unresolved exceptions: segment_location -> reason.
EXCEPTIONS = {
    "78:1:1:2": "Mechanical target 17791 (مَّ) is a clitic-only artifact stem. No clean remap "
                "and no new stem this feature — intentional unresolved exception (عَمَّ).",
    "86:5:3:2": "Mechanical target 17791 (مَّ) is a clitic-only artifact stem and equals the head "
                "(circular). No clean remap and no new stem this feature — intentional unresolved "
                "exception (مِمَّ).",
    "72:16:1:3": "No clean stem match: لَّوِ is the idghām+kasra render of لو before a hamza "
                 "(وَأَلَّوِ). Creating/mapping a canonical لو stem is deferred — intentional "
                 "unresolved exception.",
    "20:94:2:3": "Only non-function-word secondary (kinship vocative يا ابن أُمّ). No de-shadda "
                 "clean stem row exists; mechanical 7608 (ؤُمَّ) is an artifact and circular. "
                 "Canonical أُمّ decision deferred — intentional unresolved exception (يَبْنَؤُمَّ).",
}

BASIS = {
    "needs_review_contextual_idgham": "deshadda_clean",
    "needs_review_text_match": "clean_text_match",
    "circular_match": "circular_resolved_to_clean",
}

with open(CANDS, encoding="utf-8") as fh:
    doc = json.load(fh)
cands = doc["candidates"]
assert len(cands) == 483, f"expected 483 candidates, got {len(cands)}"

with open(CLEAN_MAP, encoding="utf-8") as fh:
    clean_map = json.load(fh)

mappings = []
for c in sorted(cands, key=lambda x: (x["quran_word_id"], x["segment_number"])):
    loc = c["segment_location"]
    form = c["segment_form_arabic_normalized"]
    base = OrderedDict([
        ("location", c["location"]),
        ("quran_word_id", c["quran_word_id"]),
        ("word_text_uthmani", c["word_text_uthmani"]),
        ("segment_id", c["segment_id"]),
        ("segment_location", loc),
        ("segment_number", c["segment_number"]),
        ("segment_pos", c["segment_pos"]),
        ("segment_form_arabic_normalized", form),
        ("segment_lemma_id", c["segment_lemma_id"]),
        ("segment_lemma_text", c["segment_lemma_text"]),
        ("mechanical_candidate_stem_id", c["mechanical_candidate_stem_id"]),
        ("candidate_status", c["candidate_status"]),
    ])
    if loc in EXCEPTIONS:
        base["review_decision"] = "unresolved_exception"
        base["reviewed_stem_id"] = None
        base["reviewed_stem_text"] = None
        base["decision_basis"] = "intentional_unresolved_exception"
        base["reason"] = EXCEPTIONS[loc]
    else:
        cm = clean_map[form]
        assert cm["clean_stem_id"] is not None, f"no clean stem for approved form {form} @ {loc}"
        base["review_decision"] = "approved"
        base["reviewed_stem_id"] = cm["clean_stem_id"]
        base["reviewed_stem_text"] = cm["clean_stem_text"]
        base["decision_basis"] = BASIS[c["candidate_status"]]
        base["reason"] = None
    mappings.append(base)

approved = [m for m in mappings if m["review_decision"] == "approved"]
unresolved = [m for m in mappings if m["review_decision"] == "unresolved_exception"]

# --- validation ---
assert len(mappings) == 483
assert len(approved) == 479, f"approved={len(approved)}"
assert len(unresolved) == 4, f"unresolved={len(unresolved)}"
assert all(m["reviewed_stem_id"] is not None for m in approved)
assert all(m["reviewed_stem_id"] is None and m["reason"] for m in unresolved)
seen = [m["segment_location"] for m in mappings]
assert len(seen) == len(set(seen)) == 483, "duplicate or missing segment_location"
assert set(m["segment_location"] for m in unresolved) == set(EXCEPTIONS), "exception set mismatch"

by_decision = OrderedDict()
for m in mappings:
    k = m["review_decision"]
    by_decision[k] = by_decision.get(k, 0) + 1
by_basis = OrderedDict()
for m in approved:
    k = m["decision_basis"]
    by_basis[k] = by_basis.get(k, 0) + 1
by_reviewed_stem = OrderedDict()
for m in approved:
    k = f'{m["reviewed_stem_id"]} ({m["reviewed_stem_text"]})'
    by_reviewed_stem[k] = by_reviewed_stem.get(k, 0) + 1

artifact = OrderedDict([
    ("feature", "018-segment-stems-and-stems-explorer"),
    ("artifactType", "segment-stem-corrected-arabic"),
    ("status", "curated_final_approved_with_exceptions"),
    ("generatedAtUtc", doc["generatedAtUtc"]),
    ("sourceArtifacts", [
        "segment-stem-curation-candidates.json",
        "segment-stem-curation-review-matrix.json",
    ]),
    ("decisionPolicy", [
        "Secondary STEM segments of 2-STEM words only. Head / word-level stem is separate and UNCHANGED.",
        "Approved rows map the secondary segment to its clean / de-shadda stem (idghām shadda artifact "
        "rows resolved to the clean stem; clean text-matches kept; circular cases resolved to the clean "
        "stem rather than the head artifact).",
        "4 named cases are intentional unresolved exceptions: reviewed_stem_id = null. No new stems "
        "created, no remap to canonical performed for them this feature.",
    ]),
    ("counts", OrderedDict([
        ("total_secondary_candidates", 483),
        ("approved", len(approved)),
        ("unresolved_exceptions", len(unresolved)),
    ])),
    ("decisionSummary", OrderedDict([
        ("byDecision", by_decision),
        ("approvedByBasis", by_basis),
        ("approvedByReviewedStem", by_reviewed_stem),
    ])),
    ("unresolvedExceptions", [
        OrderedDict([
            ("segment_location", m["segment_location"]),
            ("word_text_uthmani", m["word_text_uthmani"]),
            ("segment_form_arabic_normalized", m["segment_form_arabic_normalized"]),
            ("mechanical_candidate_stem_id", m["mechanical_candidate_stem_id"]),
            ("reviewed_stem_id", None),
            ("reason", m["reason"]),
        ]) for m in unresolved
    ]),
    ("mappings", mappings),
])

with open(JSON_OUT, "w", encoding="utf-8") as fh:
    json.dump(artifact, fh, ensure_ascii=False, indent=2)
    fh.write("\n")

print("total:", len(mappings), "approved:", len(approved), "unresolved:", len(unresolved))
print("byDecision:", dict(by_decision))
print("approvedByBasis:", dict(by_basis))
print("approvedByReviewedStem:", dict(by_reviewed_stem))
print("VALIDATION: PASS")
