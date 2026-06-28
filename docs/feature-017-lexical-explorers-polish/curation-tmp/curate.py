#!/usr/bin/env python3
"""Phase 0 curation generator.

Produces the schemaVersion-2 DRAFT artifact and a decisions ledger from the
reproduced audit candidate sets. Applies a strict, evidence-based decision
policy. Every candidate receives a final decision; judgment-dependent cases
that cannot be resolved by reliable evidence become `blocker` entries in the
DRAFT only (never promoted), and are reported.

Decision policy (deterministic):
  63-shift (0A):   add (target) + remove/replace (defect). 3 known replace.
  59+7 shifts(0B): add/replace where own lemma reliable; remove where own null.
                   secondary-chain detections (7) reviewed individually below.
  missing (0C):    approved `add` for reliable single-STEM mapping; else keep
                   (valid null). Never auto-add ambiguous/multistem.
  uncertain (0D):  multistem-divergence -> exception; no-own-stem -> keep;
                   own-ambiguous/unmapped -> keep; own-reliable-differs -> blocker.
  no-map (46/48):  valid-null/keep (no reliable mapping).
  multistem (0E):  known compound divergences -> exception/keep; 28:50:11 -> replace.

Outputs (curation-tmp/):
  draft.json                 schemaVersion 2 flat entries (incl. candidate/blocker)
  decisions-ledger.json      per-candidate final decision + reason
  mapping-evidence.json      structured mapping evidence for validation
  counts.json                per-class / per-kind / per-decision counts
"""
from __future__ import annotations
import json, collections, pathlib

TMP = pathlib.Path("docs/feature-017-lexical-explorers-polish/curation-tmp")
SRC = pathlib.Path("resources/import-sources/quran-morphology")
CORPUS = json.load(open(SRC/"corpus/quranic-corpus-morphology-qpc-aligned.json", encoding="utf-8"))
QUL = json.load(open(SRC/"qul/word-lemma.json", encoding="utf-8"))
REL = json.load(open(TMP/"reliable-mappings.json", encoding="utf-8"))
AMB = json.load(open(TMP/"ambiguous-mappings.json", encoding="utf-8"))

# audit ground-truth tables
S11 = json.load(open(TMP/"audit-s11-59.json", encoding="utf-8"))


def stem_lemmas(r):
    return [s["lemma"] for s in r.get("segments", []) if s.get("kind")=="STEM" and s.get("lemma")]

def is_multistem(r):
    return sum(1 for s in r.get("segments",[]) if s.get("kind")=="STEM") > 1

def own_reliable(bws):
    for b in bws:
        if b in REL:
            return REL[b], b
    return None, None


def wj(name, obj):
    with open(TMP/name, "w", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)


# we need the 63 table parsed; build from audit-s6 (produced separately). If missing,
# reconstruct from the draft.json (authoritative curated 63).
def load_63():
    d = json.load(open("docs/feature-017-lexical-explorers-polish/"
                       "word-level-lemma-alignment-corrections.draft.json", encoding="utf-8"))
    rows = []
    for e in d["entries"]:
        ops = {o["location"]: o for o in e["operations"]}
        add = next(o for o in e["operations"] if o["operationKind"]=="add")
        rem = next(o for o in e["operations"] if o["operationKind"]=="remove")
        rows.append({
            "id": e["id"],
            "target": add["location"],
            "remove": rem["location"],
            "shiftedLemma": e["shiftedLemmaArabic"],
            "shiftedBw": e["shiftedLemmaBuckwalter"],
            "targetBw": add["currentLocationCorpusLemmaBuckwalters"],
            "removeBw": rem["currentLocationCorpusLemmaBuckwalters"],
        })
    return rows

ROWS63 = load_63()

entries = []      # flat schemaVersion-2 entries
ledger = []       # decisions ledger
nid = [0]
def nid_next():
    nid[0]+=1
    return f"WLN-{nid[0]:05d}"

# ---------- mapping evidence aggregator ----------
map_ev = collections.defaultdict(lambda: {"buckwalter":None,"arabic":None,
    "supporting":0,"total":0,"ambiguity":None,"examples":[],"allowAuto":False})

def add_evidence(bw, arabic, loc):
    if not bw or not arabic: return
    e = map_ev[bw]
    e["buckwalter"]=bw; e["arabic"]=arabic
    e["supporting"]+=1
    if len(e["examples"])<3: e["examples"].append(loc)

# rebuild bw training counts (excluding 63 remove locations) for totals
K63 = set(r["remove"] for r in ROWS63)
bw_counts = collections.defaultdict(collections.Counter)
for loc,r in CORPUS.items():
    if loc in K63: continue
    a = QUL.get(loc)
    if not a: continue
    for bw in stem_lemmas(r): bw_counts[bw][a]+=1

def dominance(bw, arabic):
    c = bw_counts.get(bw)
    if not c: return (0,0,0.0)
    total = sum(c.values()); top = c.get(arabic,0)
    return (top, total, (100.0*top/total) if total else 0.0)

# ---- single-operation-per-location allocation (priority: 0A > 0B > 0C > 0D/0E) ----
ALLOCATED = {}  # loc -> (operationKind, problemClass)
dedup_log = []
def alloc(loc, kind, pclass):
    """Return True if loc is free for a mutating op of this class; else False (skip)."""
    if loc in ALLOCATED:
        prev_kind, prev_class = ALLOCATED[loc]
        dedup_log.append((loc, pclass, kind, prev_class, prev_kind))
        return False
    ALLOCATED[loc] = (kind, pclass)
    return True


# ===================== PHASE 0A — reconcile 63 =====================
cnt = collections.Counter()
for r in ROWS63:
    tloc = r["target"]; rloc = r["remove"]
    # ---- add at target ----
    tbw = r["targetBw"]
    trec = CORPUS.get(tloc, {})
    t_lemmas = stem_lemmas(trec)
    # target raw must be absent
    assert QUL.get(tloc) is None, f"target {tloc} not absent in QUL"
    add_evidence(r["shiftedBw"], r["shiftedLemma"], tloc)
    entries.append({
        "id": nid_next(), "location": tloc,
        "operationKind": "add",
        "expectedCurrentLemmaArabic": None,
        "correctedLemmaArabic": r["shiftedLemma"],
        "wordTextUthmani": trec.get("qpcUthmani"),
        "corpusLemmaBuckwalter": r["shiftedBw"],
        "corpusRootBuckwalter": next((s.get("root") for s in trec.get("segments",[]) if s.get("kind")=="STEM"), None),
        "corpusPos": next((s.get("pos") for s in trec.get("segments",[]) if s.get("kind")=="STEM"), None),
        "currentLocationCorpusLemmaBuckwalters": t_lemmas,
        "arabicMappingEvidence": f"{r['shiftedBw']} -> {r['shiftedLemma']} (reliable; {dominance(r['shiftedBw'],r['shiftedLemma'])[0]}/{dominance(r['shiftedBw'],r['shiftedLemma'])[1]}={dominance(r['shiftedBw'],r['shiftedLemma'])[2]:.1f}%)",
        "decisionStatus": "approved", "confidence": "high",
        "problemClass": "shift-63",
        "relatedLocation": rloc,
        "isMultiStem": is_multistem(trec),
        "reason": "Content-word lemma was shifted onto the following particle/pronoun word; recover it on the content word.",
        "sourceReportRef": "audit-report.md §6 (63-shift re-check)",
    })
    cnt["add"]+=1; cnt["shift-63"]+=1
    ledger.append({"problemClass":"shift-63","location":tloc,"decision":"approved-add",
                   "relatedLocation":rloc,"reason":"recover shifted content lemma"})

    # ---- remove or replace at defect location ----
    rrec = CORPUS.get(rloc, {})
    r_lemmas = stem_lemmas(rrec)
    raw = QUL.get(rloc)
    assert raw == r["shiftedLemma"], f"raw mismatch {rloc}: {raw} vs {r['shiftedLemma']}"
    own, ownbw = own_reliable(r_lemmas)
    if rloc in {"3:33:7","21:51:3","28:50:11"}:
        # replace (the 3 known)
        add_evidence(ownbw, own, rloc)
        top,total,pct = dominance(ownbw, own)
        entries.append({
            "id": nid_next(), "location": rloc,
            "operationKind": "replace",
            "expectedCurrentLemmaArabic": raw,
            "correctedLemmaArabic": own,
            "wordTextUthmani": rrec.get("qpcUthmani"),
            "corpusLemmaBuckwalter": ownbw,
            "corpusRootBuckwalter": None,
            "corpusPos": next((s.get("pos") for s in rrec.get("segments",[]) if s.get("kind")=="STEM"), None),
            "currentLocationCorpusLemmaBuckwalters": r_lemmas,
            "arabicMappingEvidence": f"{ownbw} -> {own} (reliable; {top}/{total}={pct:.1f}%)",
            "decisionStatus": "approved", "confidence": "high",
            "problemClass": "shift-63-replace",
            "relatedLocation": tloc,
            "isMultiStem": is_multistem(rrec),
            "reason": "Defect location owns its own reliable lemma (content word); replace, not remove-to-null.",
            "sourceReportRef": "audit-report.md §7",
        })
        cnt["replace"]+=1; cnt["shift-63-replace"]+=1
        ledger.append({"problemClass":"shift-63-replace","location":rloc,"decision":"approved-replace",
                       "relatedLocation":tloc,"reason":"defect owns own reliable lemma"})
    else:
        # remove-to-null (no own reliable lemma)
        assert own is None, f"unexpected own lemma at {rloc}: {own} -> should be replace"
        entries.append({
            "id": nid_next(), "location": rloc,
            "operationKind": "remove",
            "expectedCurrentLemmaArabic": raw,
            "correctedLemmaArabic": None,
            "wordTextUthmani": rrec.get("qpcUthmani"),
            "corpusLemmaBuckwalter": None,
            "corpusRootBuckwalter": None,
            "corpusPos": None,
            "currentLocationCorpusLemmaBuckwalters": r_lemmas,
            "arabicMappingEvidence": None,
            "decisionStatus": "approved", "confidence": "high",
            "problemClass": "shift-63",
            "relatedLocation": tloc,
            "isMultiStem": is_multistem(rrec),
            "reason": "Rootless particle/pronoun word with no reliable own lemma; remove shifted lemma to null.",
            "sourceReportRef": "audit-report.md §6",
        })
        cnt["remove"]+=1
        ledger.append({"problemClass":"shift-63","location":rloc,"decision":"approved-remove",
                       "relatedLocation":tloc,"reason":"no own lemma; remove shifted"})


# ===================== PHASE 0B — 59 + 7 extras =====================
# §11 table columns: [curLoc, curWord, qulLemma, ownBw, ownArabic, prevLoc, prevWord, prevBw]
s11_by_loc = {r[0]: r for r in S11}

# secondary chain detections (the 7): their prev location is itself an §11 current-location.
S11_CUR = {r[0] for r in S11}
detected = json.load(open(TMP/"cat-C-shifts.json", encoding="utf-8"))
extras = [s for s in detected if not s["inKnown63"] and s["location"] not in S11_CUR]

def emit_shift_correction(loc, source):
    """Emit add/replace/remove for an §11-style shift.
    source = dict from detected (cat-C) OR the §11 row.
    """
    if loc in s11_by_loc:
        row = s11_by_loc[loc]
        curWord = row[1]; qulLemma = row[2]; ownArabic_raw=row[4]
        prevLoc=row[5]; prevWord=row[6]
        ownArabic = None if ownArabic_raw in ("-","") else ownArabic_raw
        src = "audit-report.md §11"
    else:
        d = next(s for s in extras if s["location"]==loc)
        curWord=d["currentWord"]; qulLemma=d["rawQulLemma"]
        ownArabic=d["ownArabicCandidate"]
        prevLoc=d["prevLocation"]; prevWord=d["prevWord"]
        src = "reproduced-audit (secondary chain detection; prev location is itself §11)"

    # AUTHORITATIVE Buckwalter from the Corpus records (avoids markdown escaping issues)
    rec = CORPUS.get(loc, {}); rec_prev = CORPUS.get(prevLoc, {})
    ownBw = stem_lemmas(rec)
    prevBw = stem_lemmas(rec_prev)
    raw = QUL.get(loc); assert raw == qulLemma, f"0B raw mismatch {loc}: {raw} vs {qulLemma}"
    multi = is_multistem(rec)

    # ---- add at previous (content) location ----
    prev_own, prev_bw = own_reliable(prevBw)
    # the previous content word should be QUL-missing (lemma shifted off it)
    prev_raw = QUL.get(prevLoc)
    if prev_own is None:
        # cannot establish reliable add target -> blocker for this candidate
        ledger.append({"problemClass":"shift-59","location":loc,"decision":"blocker",
            "reason":f"previous content word {prevLoc} has no reliable lemma mapping (prevBw={prevBw}); cannot emit add"})
        entries.append({"id":nid_next(),"location":loc,"operationKind":"_candidate",
            "expectedCurrentLemmaArabic":raw,"correctedLemmaArabic":None,
            "wordTextUthmani":curWord,"decisionStatus":"candidate","confidence":"low",
            "problemClass":"shift-59","relatedLocation":prevLoc,"isMultiStem":multi,
            "reason":f"BLOCKER: no reliable add target lemma for prev {prevLoc}",
            "sourceReportRef":src,"currentLocationCorpusLemmaBuckwalters":ownBw,
            "corpusLemmaBuckwalter":None,"corpusRootBuckwalter":None,"corpusPos":None,
            "arabicMappingEvidence":None})
        cnt["blocker"]+=1
        return

    add_evidence(prev_bw, prev_own, prevLoc)
    top,total,pct = dominance(prev_bw, prev_own)
    entries.append({
        "id": nid_next(), "location": prevLoc,
        "operationKind": "add",
        "expectedCurrentLemmaArabic": prev_raw,
        "correctedLemmaArabic": prev_own,
        "wordTextUthmani": prevWord,
        "corpusLemmaBuckwalter": prev_bw,
        "corpusRootBuckwalter": next((s.get("root") for s in rec_prev.get("segments",[]) if s.get("kind")=="STEM"), None),
        "corpusPos": next((s.get("pos") for s in rec_prev.get("segments",[]) if s.get("kind")=="STEM"), None),
        "currentLocationCorpusLemmaBuckwalters": stem_lemmas(rec_prev),
        "arabicMappingEvidence": f"{prev_bw} -> {prev_own} (reliable; {top}/{total}={pct:.1f}%)",
        "decisionStatus": "approved", "confidence": "high",
        "problemClass": "shift-59",
        "relatedLocation": loc, "isMultiStem": is_multistem(rec_prev),
        "reason": "Broader previous-word shift: content lemma shifted off the content word onto the following word; recover it on the content word.",
        "sourceReportRef": src,
    })
    cnt["add"]+=1; cnt["shift-59"]+=1
    ledger.append({"problemClass":"shift-59","location":prevLoc,"decision":"approved-add",
                   "relatedLocation":loc,"reason":"broader shift: recover content lemma"})

    # ---- remove or replace at defect (current) location ----
    if ownArabic:
        own, ownbw = (ownArabic, ownBw[0]) if ownBw else (ownArabic, None)
        # validate against reliable if possible
        if ownBw and ownBw[0] in REL:
            assert REL[ownBw[0]]==ownArabic, f"0B own mismatch {loc}"
        top,total,pct = dominance(ownbw, own) if ownbw else (0,0,0.0)
        if ownbw: add_evidence(ownbw, own, loc)
        entries.append({
            "id": nid_next(), "location": loc,
            "operationKind": "replace",
            "expectedCurrentLemmaArabic": raw,
            "correctedLemmaArabic": own,
            "wordTextUthmani": curWord,
            "corpusLemmaBuckwalter": ownbw,
            "corpusRootBuckwalter": next((s.get("root") for s in rec.get("segments",[]) if s.get("kind")=="STEM"), None),
            "corpusPos": next((s.get("pos") for s in rec.get("segments",[]) if s.get("kind")=="STEM"), None),
            "currentLocationCorpusLemmaBuckwalters": ownBw,
            "arabicMappingEvidence": f"{ownbw} -> {own} (reliable; {top}/{total}={pct:.1f}%)" if ownbw else f"own Arabic candidate {own} (manual)",
            "decisionStatus": "approved", "confidence": "high",
            "problemClass": "shift-59",
            "relatedLocation": prevLoc, "isMultiStem": multi,
            "reason": "Defect location owns its own reliable content lemma; replace, not remove.",
            "sourceReportRef": src,
        })
        cnt["replace"]+=1
        ledger.append({"problemClass":"shift-59","location":loc,"decision":"approved-replace",
                       "relatedLocation":prevLoc,"reason":"defect owns own lemma"})
    else:
        entries.append({
            "id": nid_next(), "location": loc,
            "operationKind": "remove",
            "expectedCurrentLemmaArabic": raw,
            "correctedLemmaArabic": None,
            "wordTextUthmani": curWord,
            "corpusLemmaBuckwalter": None,"corpusRootBuckwalter": None,"corpusPos": None,
            "currentLocationCorpusLemmaBuckwalters": ownBw,
            "arabicMappingEvidence": None,
            "decisionStatus": "approved", "confidence": "high",
            "problemClass": "shift-59",
            "relatedLocation": prevLoc, "isMultiStem": multi,
            "reason": "Defect location has no reliable own lemma (particle/pronoun); remove shifted lemma to null.",
            "sourceReportRef": src,
        })
        cnt["remove"]+=1
        ledger.append({"problemClass":"shift-59","location":loc,"decision":"approved-remove",
                       "relatedLocation":prevLoc,"reason":"no own lemma; remove"})

# process all 59 §11 candidates
for row in S11:
    emit_shift_correction(row[0], None)
# process the 7 secondary chain detections
for s in extras:
    emit_shift_correction(s["location"], None)


# ===================== PHASE 0C — missing recovery =====================
E = json.load(open(TMP/"cat-E-missing-recovery.json", encoding="utf-8"))
for x in E:
    loc=x["location"]; rec=CORPUS.get(loc,{}); bws=x["corpusBw"]; cand=x["recoveredArabic"]
    raw=QUL.get(loc)
    assert raw is None, f"0C {loc} not missing"
    multi=x["isMultiStem"]
    if multi:
        # exclude from auto-add; valid-null/keep
        entries.append({"id":nid_next(),"location":loc,"operationKind":"_candidate",
            "expectedCurrentLemmaArabic":None,"correctedLemmaArabic":None,
            "wordTextUthmani":x["currentWord"],"decisionStatus":"accepted-exception",
            "confidence":"medium","problemClass":"missing-recovery",
            "relatedLocation":None,"isMultiStem":True,
            "reason":"Multi-STEM word excluded from automatic missing-recovery add; kept as valid null / exception.",
            "sourceReportRef":"audit-report.md §10","currentLocationCorpusLemmaBuckwalters":bws,
            "corpusLemmaBuckwalter":None,"corpusRootBuckwalter":None,"corpusPos":None,
            "arabicMappingEvidence":None})
        cnt["exception"]+=1; cnt["missing-multistem-keep"]+=1
        ledger.append({"problemClass":"missing-recovery","location":loc,"decision":"accepted-exception",
                       "reason":"multistem excluded from auto-add"})
        continue
    bw=None
    for b in bws:
        if b in REL and REL[b]==cand: bw=b; break
    if bw is None:
        # shouldn't happen (recovery came from REL), but guard
        entries.append({"id":nid_next(),"location":loc,"operationKind":"_candidate",
            "expectedCurrentLemmaArabic":None,"correctedLemmaArabic":None,
            "wordTextUthmani":x["currentWord"],"decisionStatus":"candidate",
            "confidence":"low","problemClass":"missing-recovery","relatedLocation":None,
            "isMultiStem":multi,"reason":"BLOCKER: recovery candidate not in reliable map",
            "sourceReportRef":"audit-report.md §9","currentLocationCorpusLemmaBuckwalters":bws,
            "corpusLemmaBuckwalter":None,"corpusRootBuckwalter":None,"corpusPos":None,
            "arabicMappingEvidence":None})
        cnt["blocker"]+=1
        continue
    add_evidence(bw, cand, loc)
    top,total,pct=dominance(bw,cand)
    entries.append({
        "id":nid_next(),"location":loc,"operationKind":"add",
        "expectedCurrentLemmaArabic":None,"correctedLemmaArabic":cand,
        "wordTextUthmani":x["currentWord"],
        "corpusLemmaBuckwalter":bw,
        "corpusRootBuckwalter":next((s.get("root") for s in rec.get("segments",[]) if s.get("kind")=="STEM"),None),
        "corpusPos":next((s.get("pos") for s in rec.get("segments",[]) if s.get("kind")=="STEM"),None),
        "currentLocationCorpusLemmaBuckwalters":bws,
        "arabicMappingEvidence":f"{bw} -> {cand} (reliable; {top}/{total}={pct:.1f}%)",
        "decisionStatus":"approved","confidence":"high" if pct>=90 else "medium",
        "problemClass":"missing-recovery","relatedLocation":None,"isMultiStem":False,
        "reason":"QUL word-level lemma absent; reliable Corpus Buckwalter→Arabic mapping recovers it.",
        "sourceReportRef":"audit-report.md §9",
    })
    cnt["add"]+=1; cnt["missing-recovery"]+=1
    ledger.append({"problemClass":"missing-recovery","location":loc,"decision":"approved-add",
                   "reason":"reliable mapping recovery"})


# ===================== PHASE 0D — uncertain + no-map =====================
U = json.load(open(TMP/"cat-H-uncertain.json", encoding="utf-8"))
for x in U:
    loc=x["location"]; rec=CORPUS.get(loc,{}); raw=x["rawQulLemma"]; bws=x["ownCorpusBw"]; multi=x["isMultiStem"]
    own,ownbw = own_reliable(bws)
    if multi:
        entries.append({"id":nid_next(),"location":loc,"operationKind":"_exception",
            "expectedCurrentLemmaArabic":raw,"correctedLemmaArabic":raw,
            "wordTextUthmani":x["currentWord"],"decisionStatus":"accepted-exception",
            "confidence":"high","problemClass":"multi-stem","relatedLocation":None,"isMultiStem":True,
            "reason":"Multi-STEM/compound divergence; QUL lemma accepted as-is (legitimate source modeling difference).",
            "sourceReportRef":"audit-report.md §10","currentLocationCorpusLemmaBuckwalters":bws,
            "corpusLemmaBuckwalter":ownbw,"corpusRootBuckwalter":None,"corpusPos":None,
            "arabicMappingEvidence":None})
        cnt["exception"]+=1; cnt["uncertain-multistem"]+=1
        ledger.append({"problemClass":"uncertain","location":loc,"decision":"accepted-exception","reason":"multistem divergence"})
    elif not bws:
        entries.append({"id":nid_next(),"location":loc,"operationKind":"_keep",
            "expectedCurrentLemmaArabic":raw,"correctedLemmaArabic":raw,
            "wordTextUthmani":x["currentWord"],"decisionStatus":"accepted-exception",
            "confidence":"high","problemClass":"uncertain","relatedLocation":None,"isMultiStem":multi,
            "reason":"Word has no own STEM lemma evidence (null); QUL lemma accepted; no contradicting evidence to overrule.",
            "sourceReportRef":"audit-report.md §8","currentLocationCorpusLemmaBuckwalters":[],
            "corpusLemmaBuckwalter":None,"corpusRootBuckwalter":None,"corpusPos":None,
            "arabicMappingEvidence":None})
        cnt["keep"]+=1
        ledger.append({"problemClass":"uncertain","location":loc,"decision":"keep","reason":"no own stem lemma"})
    elif own and own!=raw:
        # own reliable mapping DIFFERS from QUL.
        # Mechanical exception: QUL lemma is structurally invalid (contains a space,
        # i.e. a two-word token stored as a single word's lemma) -> auto-replace.
        if raw and " " in raw.strip():
            top,total,pct=dominance(ownbw,own)
            add_evidence(ownbw, own, loc)
            entries.append({"id":nid_next(),"location":loc,"operationKind":"replace",
                "expectedCurrentLemmaArabic":raw,"correctedLemmaArabic":own,
                "wordTextUthmani":x["currentWord"],"decisionStatus":"approved",
                "confidence":"high","problemClass":"uncertain","relatedLocation":None,"isMultiStem":multi,
                "reason":f"QUL lemma '{raw}' is structurally invalid (multi-word token on a single word); replace with own reliable lemma '{own}'.",
                "sourceReportRef":"audit-report.md §8","currentLocationCorpusLemmaBuckwalters":bws,
                "corpusLemmaBuckwalter":ownbw,"corpusRootBuckwalter":None,"corpusPos":None,
                "arabicMappingEvidence":f"{ownbw} -> {own} (reliable; {top}/{total}={pct:.1f}%)"})
            cnt["replace"]+=1
            ledger.append({"problemClass":"uncertain","location":loc,"decision":"approved-replace",
                           "reason":f"invalid multi-word QUL lemma '{raw}' -> '{own}'"})
            continue
        # otherwise: genuine scholarly blocker
        entries.append({"id":nid_next(),"location":loc,"operationKind":"_candidate",
            "expectedCurrentLemmaArabic":raw,"correctedLemmaArabic":own,
            "wordTextUthmani":x["currentWord"],"decisionStatus":"candidate",
            "confidence":"low","problemClass":"uncertain","relatedLocation":None,"isMultiStem":multi,
            "reason":f"BLOCKER: own reliable mapping '{own}' ({ownbw}) conflicts with QUL '{raw}'; requires Quranic-linguistic review to overrule QUL.",
            "sourceReportRef":"audit-report.md §8","currentLocationCorpusLemmaBuckwalters":bws,
            "corpusLemmaBuckwalter":ownbw,"corpusRootBuckwalter":None,"corpusPos":None,
            "arabicMappingEvidence":f"{ownbw} -> {own} (reliable; {dominance(ownbw,own)[0]}/{dominance(ownbw,own)[1]}={dominance(ownbw,own)[2]:.1f}%)"})
        cnt["blocker"]+=1
        ledger.append({"problemClass":"uncertain","location":loc,"decision":"blocker",
                       "reason":f"own reliable '{own}' conflicts with QUL '{raw}'; scholarly review required"})
    else:
        # ambiguous-only or own==raw(unmapped edge): keep
        entries.append({"id":nid_next(),"location":loc,"operationKind":"_keep",
            "expectedCurrentLemmaArabic":raw,"correctedLemmaArabic":raw,
            "wordTextUthmani":x["currentWord"],"decisionStatus":"accepted-exception",
            "confidence":"medium","problemClass":"uncertain","relatedLocation":None,"isMultiStem":multi,
            "reason":"Own Corpus evidence ambiguous/unmapped (below reliable threshold); QUL lemma accepted; insufficient evidence to overrule.",
            "sourceReportRef":"audit-report.md §8","currentLocationCorpusLemmaBuckwalters":bws,
            "corpusLemmaBuckwalter":ownbw,"corpusRootBuckwalter":None,"corpusPos":None,
            "arabicMappingEvidence":None})
        cnt["keep"]+=1
        ledger.append({"problemClass":"uncertain","location":loc,"decision":"keep","reason":"ambiguous/unmapped own evidence"})

# no-map (46/48): all valid-null / keep
NM = json.load(open(TMP/"cat-H-nomap.json", encoding="utf-8"))
for x in NM:
    loc=x["location"]; rec=CORPUS.get(loc,{})
    entries.append({"id":nid_next(),"location":loc,"operationKind":"_keep",
        "expectedCurrentLemmaArabic":None,"correctedLemmaArabic":None,
        "wordTextUthmani":x["currentWord"],"decisionStatus":"accepted-exception",
        "confidence":"medium","problemClass":"uncertain","relatedLocation":None,
        "isMultiStem":bool([s for s in rec.get('segments',[]) if s.get('kind')=='STEM']),
        "reason":"QUL lemma absent and Corpus Buckwalter has no reliable Arabic mapping (below threshold); valid null; absence accepted.",
        "sourceReportRef":"audit-report.md §9 (no-reliable-mapping)","currentLocationCorpusLemmaBuckwalters":x["corpusBw"],
        "corpusLemmaBuckwalter":None,"corpusRootBuckwalter":None,"corpusPos":None,
        "arabicMappingEvidence":None})
    cnt["keep"]+=1
    ledger.append({"problemClass":"uncertain","location":loc,"decision":"valid-null",
                   "reason":"no reliable mapping; valid null"})


# ===================== PHASE 0E — multi-STEM compound allow-list =====================
# Known compound divergences -> exception. 28:50:11 already handled in 0A as replace.
G = json.load(open(TMP/"cat-G-multistem.json", encoding="utf-8"))
compound_locs = {
    "8:28:2","11:14:5","18:110:8","21:108:5","38:70:5","41:6:8",  # أنّما
    "8:73:6",  # إلا
}
for x in G:
    loc=x["location"]; rec=CORPUS.get(loc,{}); raw=QUL.get(loc)
    if loc in compound_locs:
        entries.append({"id":nid_next(),"location":loc,"operationKind":"_exception",
            "expectedCurrentLemmaArabic":raw,"correctedLemmaArabic":raw,
            "wordTextUthmani":rec.get("qpcUthmani"),"decisionStatus":"accepted-exception",
            "confidence":"high","problemClass":"multi-stem","relatedLocation":None,"isMultiStem":True,
            "reason":"Known compound particle (multi-STEM) with legitimate QUL-vs-Corpus modeling divergence; accepted exception, not auto-corrected.",
            "sourceReportRef":"audit-report.md §10","currentLocationCorpusLemmaBuckwalters":x.get("corpusBw",[]),
            "corpusLemmaBuckwalter":None,"corpusRootBuckwalter":None,"corpusPos":None,
            "arabicMappingEvidence":None})
        cnt["exception"]+=1
        ledger.append({"problemClass":"multi-stem","location":loc,"decision":"accepted-exception",
                       "reason":"known compound divergence"})


# ===================== finalize mapping evidence =====================
me_list=[]
for bw,e in map_ev.items():
    top,total,pct = dominance(bw, e["arabic"])
    me_list.append({
        "buckwalter":bw,"arabicLemma":e["arabic"],
        "supportingCount":e["supporting"],"totalCount":total,"dominancePct":round(pct,1),
        "ambiguityStatus":"reliable","examples":e["examples"],
        "allowAutoAddReplace": (bw in REL),
    })
me_list.sort(key=lambda x:(-x["supportingCount"], x["buckwalter"]))


# ===================== DEDUP: single operation per location =====================
# Priority order (highest wins). Within the same priority, mutating > non-mutating,
# and the first emitted wins. Duplicates are dropped (and logged) because they are the
# SAME correction expressed by two problem classes (e.g. shift-63 target == missing-recovery).
PRIORITY = {
    "shift-63": 0, "shift-63-replace": 0,
    "shift-59": 1,
    "missing-recovery": 2, "missing-multistem-keep": 2,
    "uncertain": 3, "multi-stem": 3,
}
MUTATING = {"add","remove","replace"}
deduped = []
seen = {}   # loc -> (prio, is_mut, idx)
dedup_dropped = []
for e in entries:
    loc = e["location"]
    is_mut = e["operationKind"] in MUTATING
    prio = PRIORITY.get(e["problemClass"], 9)
    if loc in seen:
        p_prio, p_mut, p_idx = seen[loc]
        # keep higher priority; if equal priority, keep mutating over non-mutating, else first
        if prio < p_prio or (prio == p_prio and is_mut and not p_mut):
            # replace the previously-kept one
            dedup_dropped.append((deduped[p_idx]["location"], deduped[p_idx]["problemClass"],
                                  deduped[p_idx]["operationKind"], "superseded by", e["problemClass"], e["operationKind"]))
            deduped[p_idx] = e
            seen[loc] = (prio, is_mut, p_idx)
        else:
            dedup_dropped.append((loc, e["problemClass"], e["operationKind"],
                                  "dropped; existing", deduped[p_idx]["problemClass"], deduped[p_idx]["operationKind"]))
        continue
    seen[loc] = (prio, is_mut, len(deduped))
    deduped.append(e)
entries = deduped
# renumber ids sequentially after dedup
for i, e in enumerate(entries, 1):
    e["id"] = f"WLN-{i:05d}"

# the 33:61:2-style invalid add (prev word already QUL-present): drop shift-59 adds
# whose expectedCurrent is non-null AND not also a remove target -> those are not valid adds.
# (Validated here defensively: any 'add' with non-null expected is invalid and demoted.)
bad_adds = [e for e in entries if e["operationKind"]=="add" and e.get("expectedCurrentLemmaArabic") is not None]
for e in bad_adds:
    e["operationKind"] = "_candidate"
    e["decisionStatus"] = "candidate"
    e["reason"] = (e.get("reason","") +
        " | DEMOTED: add target already has a QUL lemma (shift-59 prev not actually missing); "
        "genuine scholarly review required.")

wj("dedup-dropped.json", dedup_dropped)
print("dedup dropped:", len(dedup_dropped), "| demoted invalid adds:", len(bad_adds))


# ===================== write draft + ledger + evidence + counts =====================
draft = {
    "schemaVersion": 2,
    "artifactId": "word-lemma-normalization",
    "sourcePackage": "resources/import-sources/quran-morphology",
    "sourceAudit": "docs/feature-017-lexical-explorers-polish/full-word-level-lemma-alignment-audit-report.md",
    "generatedFromReports": [
        "docs/feature-017-lexical-explorers-polish/full-word-level-lemma-alignment-audit-report.md",
        "docs/feature-017-lexical-explorers-polish/word-level-lemma-alignment-correction-curation-report.md",
    ],
    "note": "DRAFT/staging artifact. May contain candidate/blocker/_keep/_exception entries. "
            "Only approved add/remove/replace + accepted-exception/keep are promoted to the active artifact.",
    "entries": entries,
}
wj("draft.json", draft)
wj("decisions-ledger.json", ledger)
wj("mapping-evidence.json", me_list)
wj("counts.json", {"byProblemClass":dict(cnt),
                   "byOperationKind":{
                       "add":sum(1 for e in entries if e["operationKind"]=="add"),
                       "remove":sum(1 for e in entries if e["operationKind"]=="remove"),
                       "replace":sum(1 for e in entries if e["operationKind"]=="replace"),
                       "keep":sum(1 for e in entries if e["operationKind"]=="_keep"),
                       "exception":sum(1 for e in entries if e["operationKind"]=="_exception"),
                       "candidate_blocker":sum(1 for e in entries if e["operationKind"]=="_candidate"),
                   },
                   "byDecision":{
                       "approved":sum(1 for e in entries if e["decisionStatus"]=="approved"),
                       "accepted-exception":sum(1 for e in entries if e["decisionStatus"]=="accepted-exception"),
                       "candidate_blocker":sum(1 for e in entries if e["decisionStatus"]=="candidate"),
                   },
                   "blockers":[e["location"] for e in entries if e["decisionStatus"]=="candidate"],
                   "totalEntries":len(entries),
                   "mappingEvidenceRecords":len(me_list),
                   })

print("entries:", len(entries))
print("counts:", json.dumps({"byOperationKind":{
    "add":sum(1 for e in entries if e["operationKind"]=="add"),
    "remove":sum(1 for e in entries if e["operationKind"]=="remove"),
    "replace":sum(1 for e in entries if e["operationKind"]=="replace"),
    "keep":sum(1 for e in entries if e["operationKind"]=="_keep"),
    "exception":sum(1 for e in entries if e["operationKind"]=="_exception"),
    "candidate_blocker":sum(1 for e in entries if e["operationKind"]=="_candidate"),
}}, ensure_ascii=False, indent=2))
print("blockers:", sum(1 for e in entries if e["decisionStatus"]=="candidate"))
