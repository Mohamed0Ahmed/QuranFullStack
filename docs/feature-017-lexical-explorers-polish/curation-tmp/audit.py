#!/usr/bin/env python3
"""Reproduce the full word-level lemma alignment audit from staged source JSON.

Mirrors docs/.../full-word-level-lemma-alignment-audit-report.md §3 methodology:
 1. Load aligned Corpus/QPC words + QUL lemma map.
 2. Build same-word Corpus-Buckwalter -> Arabic-lemma mapping from existing
    QUL lemma assignments, EXCLUDING the 63 known remove locations so they
    cannot train their own wrong assignment.
 3. reliable = unique OR (>=5 examples AND >=80% share).
 4. Classify every readable word into the audit categories A..H and emit the
    full candidate sets needed for Phase 0 curation.

Outputs JSON files in curation-tmp/:
  - reliable-mappings.json   (Buckwalter -> Arabic allow-list, 4797 expected)
  - ambiguous-mappings.json  (9 expected)
  - cat-C-shifts.json        (prev-word shift candidates incl. 63)
  - cat-E-missing-recovery.json (1595 expected)
  - cat-F-replace.json       (3 expected)
  - cat-G-multistem.json     (multi-stem mismatches)
  - cat-H-uncertain.json     (130 expected; +46 no-reliable-mapping)
  - summary.json             (global counts)
"""
from __future__ import annotations
import json, collections, sys, pathlib

SRC = pathlib.Path("resources/import-sources/quran-morphology")
CORPUS = SRC / "corpus" / "quranic-corpus-morphology-qpc-aligned.json"
QUL_LEMMA = SRC / "qul" / "word-lemma.json"
OUT = pathlib.Path("docs/feature-017-lexical-explorers-polish/curation-tmp")

# ---- the 63 known remove locations (from audit §6) ----
KNOWN63_REMOVE = {
    "2:44:6","2:75:4","2:114:17","2:216:11","2:228:8","2:247:14","3:33:7",
    "3:178:15","4:12:9","4:36:5","4:50:8","4:101:21","5:5:10","5:5:13",
    "5:106:38","5:116:20","6:5:11","6:20:11","6:92:18","6:101:10","7:89:17",
    "7:92:12","7:157:33","9:17:17","9:37:7","9:55:16","9:85:15","9:94:11",
    "11:8:21","11:17:18","12:17:15","12:24:5","12:38:9","12:64:15","14:11:19",
    "16:60:10","17:66:13","17:110:10","19:79:6","20:101:4","21:51:3","21:51:8",
    "21:73:14","24:55:30","25:15:11","25:58:10","25:59:16","26:111:3",
    "28:50:11","28:57:11","29:8:8","29:26:2","29:47:9","29:53:10","34:39:16",
    "36:30:10","40:12:10","41:21:13","41:31:13","42:9:9","45:10:17","46:8:13",
    "57:15:12","57:15:12",
}
KNOWN63_REMOVE = {x for x in KNOWN63_REMOVE}  # dedupe


def load_json(p):
    with open(p, encoding="utf-8") as f:
        return json.load(f)


def stem_lemmas(loc_rec):
    """Buckwalter STEM lemma(s) for a corpus location record."""
    if not loc_rec:
        return []
    out = []
    for seg in loc_rec.get("segments", []):
        if seg.get("kind") == "STEM" and seg.get("lemma"):
            out.append(seg["lemma"])
    return out


def all_seg_lemmas(loc_rec):
    if not loc_rec:
        return []
    return [s.get("lemma") for s in loc_rec.get("segments", []) if s.get("lemma")]


def is_multistem(loc_rec):
    if not loc_rec:
        return False
    return sum(1 for s in loc_rec.get("segments", []) if s.get("kind") == "STEM") > 1


def main():
    corpus = load_json(CORPUS)
    qul_lemma = load_json(QUL_LEMMA)

    readable = sorted(corpus.keys())  # aligned readable words
    n_readable = len(readable)

    # ---- 1. Build Buckwalter -> Arabic mapping from existing QUL assignments ----
    # counts[bw][arabic] = N ; only same-word STEM lemma at locations that HAVE a
    # QUL lemma AND are not in KNOWN63_REMOVE.
    bw_counts = collections.defaultdict(collections.Counter)
    for loc in readable:
        if loc in KNOWN63_REMOVE:
            continue
        arabic = qul_lemma.get(loc)
        if not arabic:
            continue
        for bw in stem_lemmas(corpus[loc]):
            bw_counts[bw][arabic] += 1

    reliable = {}      # bw -> arabic
    ambiguous = {}     # bw -> dict of candidates
    for bw, counter in bw_counts.items():
        total = sum(counter.values())
        if len(counter) == 1:
            arabic = next(iter(counter))
            reliable[bw] = arabic
            continue
        # multiple candidates: pick dominant if reliable threshold
        top_arabic, top_n = counter.most_common(1)[0]
        if top_n >= 5 and top_n / total >= 0.80:
            reliable[bw] = top_arabic
        else:
            ambiguous[bw] = {"total": total,
                             "candidates": dict(counter.most_common())}

    # ---- 2. Classify each readable word ----
    cat_C_shifts = []          # prev-word shift candidates
    cat_E_missing_recovery = []  # 1595
    cat_F_replace = []         # (the 3 known + any new detected)
    cat_G_multistem = []
    cat_H_uncertain = []
    cat_H_nomap = []           # 46 missing-with-corpus-no-reliable-mapping

    def prev_loc(loc):
        # previous word in same ayah
        s, a, w = loc.split(":")
        return f"{s}:{a}:{int(w)-1}"

    # iterate ayah-wise so prev is meaningful
    ayah_groups = collections.defaultdict(list)
    for loc in readable:
        s, a, w = loc.split(":")
        ayah_groups[f"{s}:{a}"].append((int(w), loc))
    for k in ayah_groups:
        ayah_groups[k].sort()

    loc_set = set(readable)

    # ---- Category A/B/E/F/H over QUL-present and QUL-missing ----
    # Same-word support: does this word's own STEM lemma(s) map (reliably) to its QUL lemma?
    for loc in readable:
        rec = corpus[loc]
        bw_lems = stem_lemmas(rec)
        arabic = qul_lemma.get(loc)
        multi = is_multistem(rec)

        if arabic:
            # QUL-present. Supported if any own STEM lemma reliably maps to this arabic,
            # OR arabic is among reliable[bw] for own bw.
            supported = any(reliable.get(b) == arabic for b in bw_lems if b in reliable)
            # Detect previous-word shift: own STEM lemma(s) do NOT support arabic,
            # but PREVIOUS word's STEM lemma(s) reliably map to this arabic.
            if not supported:
                ploc = prev_loc(loc)
                if ploc in loc_set:
                    pbw = stem_lemmas(corpus[ploc])
                    if any(reliable.get(b) == arabic for b in pbw if b in reliable):
                        # own-lemma candidate for THIS location?
                        own_arabic = None
                        for b in bw_lems:
                            if b in reliable:
                                own_arabic = reliable[b]; break
                        cat_C_shifts.append({
                            "location": loc,
                            "currentWord": rec.get("qpcUthmani"),
                            "rawQulLemma": arabic,
                            "ownCorpusBw": bw_lems,
                            "ownArabicCandidate": own_arabic,
                            "prevLocation": ploc,
                            "prevWord": corpus[ploc].get("qpcUthmani"),
                            "prevCorpusBw": pbw,
                            "isMultiStem": multi,
                            "inKnown63": loc in KNOWN63_REMOVE,
                        })
                        continue
                # unsupported / uncertain
                cat_H_uncertain.append({
                    "location": loc, "currentWord": rec.get("qpcUthmani"),
                    "rawQulLemma": arabic, "ownCorpusBw": bw_lems,
                    "isMultiStem": multi,
                })
        else:
            # QUL-missing.
            if not bw_lems:
                continue  # valid null (cat B)
            # reliable recovery candidate?
            cand = None
            for b in bw_lems:
                if b in reliable:
                    cand = reliable[b]; break
            if cand is not None:
                if multi and not (loc in {"28:50:11"}):
                    # multi-stem default -> uncertain review, but record
                    cat_G_multistem.append({
                        "location": loc, "currentWord": rec.get("qpcUthmani"),
                        "corpusBw": bw_lems, "candidate": cand,
                        "note": "missing-lemma multistem",
                    })
                    continue
                cat_E_missing_recovery.append({
                    "location": loc, "currentWord": rec.get("qpcUthmani"),
                    "corpusBw": bw_lems, "recoveredArabic": cand,
                    "isMultiStem": multi,
                })
            else:
                # corpus evidence but no reliable mapping
                cat_H_nomap.append({
                    "location": loc, "currentWord": rec.get("qpcUthmani"),
                    "corpusBw": bw_lems, "ambiguous": [ambiguous.get(b) for b in bw_lems if b in ambiguous],
                })

    # ---- Category F replace: the 3 known remove-locations owning their own lemma ----
    for loc in ["3:33:7", "21:51:3", "28:50:11"]:
        rec = corpus[loc]
        bw_lems = stem_lemmas(rec)
        own = None
        for b in bw_lems:
            if b in reliable:
                own = reliable[b]; break
        cat_F_replace.append({
            "location": loc, "currentWord": rec.get("qpcUthmani"),
            "rawQulLemma": qul_lemma.get(loc), "corpusBw": bw_lems,
            "ownArabic": own,
        })

    # ---- Category G multi-STEM mismatches (QUL-present + own mismatch) ----
    for loc in readable:
        rec = corpus[loc]
        if not is_multistem(rec):
            continue
        arabic = qul_lemma.get(loc)
        bw_lems = stem_lemmas(rec)
        if arabic and not any(reliable.get(b) == arabic for b in bw_lems if b in reliable):
            cat_G_multistem.append({
                "location": loc, "currentWord": rec.get("qpcUthmani"),
                "rawQulLemma": arabic, "corpusBw": bw_lems,
                "isMultiStem": True, "note": "present-mismatch",
            })

    def wj(name, obj):
        with open(OUT / name, "w", encoding="utf-8") as f:
            json.dump(obj, f, ensure_ascii=False, indent=2)

    wj("reliable-mappings.json", reliable)
    wj("ambiguous-mappings.json", ambiguous)
    wj("cat-C-shifts.json", cat_C_shifts)
    wj("cat-E-missing-recovery.json", cat_E_missing_recovery)
    wj("cat-F-replace.json", cat_F_replace)
    wj("cat-G-multistem.json", cat_G_multistem)
    wj("cat-H-uncertain.json", cat_H_uncertain)
    wj("cat-H-nomap.json", cat_H_nomap)

    summary = {
        "readable": n_readable,
        "qul_lemma_entries": len(qul_lemma),
        "reliable_mappings": len(reliable),
        "ambiguous_mappings": len(ambiguous),
        "cat_C_shifts": len(cat_C_shifts),
        "cat_C_shifts_in_known63": sum(1 for x in cat_C_shifts if x["inKnown63"]),
        "cat_C_shifts_new": sum(1 for x in cat_C_shifts if not x["inKnown63"]),
        "cat_E_missing_recovery": len(cat_E_missing_recovery),
        "cat_F_replace_known": len(cat_F_replace),
        "cat_G_multistem": len(cat_G_multistem),
        "cat_H_uncertain": len(cat_H_uncertain),
        "cat_H_nomap": len(cat_H_nomap),
    }
    wj("summary.json", summary)
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
