"""Reduce the 483 secondary-STEM candidates into a grouped review-decision matrix.

Pure reduction of segment-stem-curation-candidates.json (no DB access). Groups by a
composite key (form + secondary POS + lemma + primary POS + candidate_status), so every
candidate lands in exactly one group. Assigns DRAFT decisions only — nothing is approved.
Emits segment-stem-curation-review-matrix.{json,csv}.
"""
import csv
import json
from collections import OrderedDict

DIR = "/projects/Dashboard/App/docs/feature-018-segment-stems-and-stems-explorer/"
SRC = DIR + "segment-stem-curation-candidates.json"
JSON_OUT = DIR + "segment-stem-curation-review-matrix.json"
CSV_OUT = DIR + "segment-stem-curation-review-matrix.csv"

# De-shadda "clean" canonical stem per secondary form. Loaded from a DB-generated map
# (build_clean_map.sql) keyed by EXACT form strings to avoid hand-typed-literal mismatches.
# CLEAN[form] = (clean_stem_id, clean_stem_text); None id => no clean de-shadda row exists.
with open(DIR + "sql/clean_stem_map.json", encoding="utf-8") as fh:
    _raw_clean = json.load(fh)
CLEAN = {f: (v["clean_stem_id"], v["clean_stem_text"]) for f, v in _raw_clean.items()}

with open(SRC, encoding="utf-8") as fh:
    doc = json.load(fh)
cands = doc["candidates"]
assert len(cands) == 483, f"expected 483 candidates, got {len(cands)}"

groups = OrderedDict()
for c in cands:
    key = (
        c["segment_form_arabic_normalized"], c["segment_pos"], c["segment_lemma_id"],
        c["primary_stem_pos"], c["candidate_status"],
    )
    groups.setdefault(key, []).append(c)

assert sum(len(v) for v in groups.values()) == 483


def decide(form, lemma_id, status, flags, mech_id, mech_text):
    """Return (decision, reviewed_id, reviewed_text, confidence, priority, special, notes)."""
    clean_id, clean_text = CLEAN.get(form, (None, None))
    if status == "no_text_match":
        return ("create_or_map_canonical_law_stem", None,
                "لو (canonical) — de-shadda row لَوِ=6226 is a candidate, but a clean لو stem may be created",
                "low", "high", "no_text_match",
                "Idghām+kasra render of لو before hamza; NO shadda-form match. لَّوِ=72:16:1:3. "
                "Scholar must choose: reuse لَوِ(6226) / canonical لَوْ / new clean لو row.")
    if lemma_id == 150:  # أُمّ / ؤُمَّ
        return ("decide_canonical_umm_stem", None, "أُمّ (canonical) — no de-shadda row exists",
                "low", "high", "yabnaumma",
                "يَبْنَؤُمَّ (20:94:2:3): kinship vocative يا ابن أُمّ. Only non-function-word secondary. "
                "Mechanical 7608 (ؤُمَّ) is a clitic artifact AND equals the head stem (circular). "
                "Scholar must pick/create the canonical أُمّ stem.")
    if "artifact_stem_text" in flags:
        return ("remap_artifact_to_canonical", clean_id, clean_text,
                "low", "high", "artifact_target",
                f"Mechanical {mech_id} ({mech_text}) is a clitic-only artifact stem; do NOT link to it. "
                f"Draft clean canonical = {clean_text}({clean_id}). Scholar confirm.")
    if status == "circular_match":
        return ("treat_as_head_stem_noop", mech_id, mech_text,
                "low", "normal", "circular",
                f"Secondary form text-matches the word's OWN head stem {mech_id} ({mech_text}); "
                f"likely a no-op (count word once, no second attribution). Note the head itself is often a "
                f"shadda artifact — clean de-shadda would be {clean_text}({clean_id}); head remap is OUT OF SCOPE. "
                f"Scholar confirm whether a genuine second stem exists.")
    if status == "needs_review_contextual_idgham":
        return ("map_clean_canonical_stem", clean_id, clean_text,
                "medium", "normal", "idgham",
                f"Mechanical {mech_id} ({mech_text}) is a shadda (idghām) artifact row. Draft = de-shadda clean "
                f"stem {clean_text}({clean_id}). Scholar: confirm idghām canonicalization + variant collapse.")
    if status == "needs_review_text_match":
        return ("accept_mechanical_clean_stem", mech_id, mech_text,
                "medium", "normal", "clean_text_match",
                f"Clean form already matches a clean stem {mech_id} ({mech_text}). Remaining question is "
                f"sense-collapse (e.g. preventive vs relative ما) — scholar confirm, not auto-approved.")
    return ("UNCLASSIFIED", None, None, "low", "high", "unclassified", "Review manually.")


out_groups = []
for gid, (key, members) in enumerate(groups.items(), start=1):
    form, spos, lemma_id, ppos, status = key
    first = members[0]
    flags = first["risk_flags"]
    # flags are constant within a group (determined by form/status); verify.
    for m in members:
        assert m["risk_flags"] == flags, f"non-constant risk_flags in group {key}"
    mech_id = first["mechanical_candidate_stem_id"]
    mech_text = first["mechanical_candidate_stem_text"]
    decision, rid, rtext, conf, prio, special, notes = decide(
        form, lemma_id, status, flags, mech_id, mech_text)
    members_sorted = sorted(members, key=lambda m: m["quran_word_id"])
    samples = members_sorted[:5]
    out_groups.append(OrderedDict([
        ("group_id", gid),
        ("group_key", f"{form}|{spos}|{lemma_id}|{ppos}|{status}"),
        ("secondary_form", form),
        ("secondary_pos", spos),
        ("secondary_lemma_id", lemma_id),
        ("secondary_lemma_text", first["segment_lemma_text"]),
        ("primary_pos", ppos),
        ("pos_pattern", f"{ppos}+{spos}"),
        ("candidate_status", status),
        ("risk_flags", flags),
        ("candidate_count", len(members)),
        ("mechanical_candidate_stem_id", mech_id),
        ("mechanical_candidate_stem_text", mech_text),
        ("clean_deshadda_stem_id", CLEAN.get(form, (None, None))[0]),
        ("clean_deshadda_stem_text", CLEAN.get(form, (None, None))[1]),
        ("sample_locations", [m["segment_location"] for m in samples]),
        ("sample_words", [m["word_text_uthmani"] for m in samples]),
        ("draft_decision", decision),
        ("draft_reviewed_stem_id", rid),
        ("draft_reviewed_stem_text", rtext),
        ("confidence", conf),
        ("review_required", True),
        ("review_type", "scholar"),
        ("priority", prio),
        ("special_case", special),
        ("notes", notes),
    ]))

out_groups.sort(key=lambda g: -g["candidate_count"])
for i, g in enumerate(out_groups, start=1):
    g["group_id"] = i

assert sum(g["candidate_count"] for g in out_groups) == 483

decision_summary = OrderedDict()
for g in out_groups:
    d = g["draft_decision"]
    decision_summary.setdefault(d, {"groups": 0, "candidates": 0})
    decision_summary[d]["groups"] += 1
    decision_summary[d]["candidates"] += g["candidate_count"]

matrix = OrderedDict([
    ("feature", "018-segment-stems-and-stems-explorer"),
    ("artifactType", "segment-stem-curation-review-matrix"),
    ("status", "draft_decisions_not_approved"),
    ("generatedAtUtc", doc["generatedAtUtc"]),
    ("sourceArtifact", "segment-stem-curation-candidates.json"),
    ("notice", [
        "Draft grouped decisions only — NOT approved mappings.",
        "draft_reviewed_stem_id/_text are mechanical suggestions for the curator; every group requires review.",
        "Do not generate segment-stem-corrected-arabic.json from this file mechanically.",
    ]),
    ("counts", OrderedDict([
        ("total_candidates", 483),
        ("total_groups", len(out_groups)),
        ("candidates_in_groups", sum(g["candidate_count"] for g in out_groups)),
    ])),
    ("decisionSummary", decision_summary),
    ("groups", out_groups),
])

with open(JSON_OUT, "w", encoding="utf-8") as fh:
    json.dump(matrix, fh, ensure_ascii=False, indent=2)
    fh.write("\n")

cols = [
    "group_id", "group_key", "secondary_form", "secondary_pos", "secondary_lemma_id",
    "secondary_lemma_text", "primary_pos", "pos_pattern", "candidate_status", "risk_flags",
    "candidate_count", "mechanical_candidate_stem_id", "mechanical_candidate_stem_text",
    "clean_deshadda_stem_id", "clean_deshadda_stem_text", "sample_locations", "sample_words",
    "draft_decision", "draft_reviewed_stem_id", "draft_reviewed_stem_text", "confidence",
    "review_required", "review_type", "priority", "special_case", "notes",
]
with open(CSV_OUT, "w", encoding="utf-8", newline="") as fh:
    w = csv.writer(fh)
    w.writerow(cols)
    for g in out_groups:
        row = []
        for c in cols:
            v = g[c]
            if isinstance(v, list):
                v = ";".join(str(x) for x in v)
            elif isinstance(v, bool):
                v = "true" if v else "false"
            elif v is None:
                v = ""
            row.append(v)
        w.writerow(row)

print("groups:", len(out_groups))
print("sum candidates:", sum(g["candidate_count"] for g in out_groups))
print("decision summary:")
for d, s in sorted(decision_summary.items(), key=lambda kv: -kv[1]["candidates"]):
    print(f"  {d}: {s['candidates']} cands / {s['groups']} groups")
print("high-priority groups:", sum(1 for g in out_groups if g["priority"] == "high"))
