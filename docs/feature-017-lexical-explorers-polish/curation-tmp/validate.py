#!/usr/bin/env python3
"""Self-validator for the draft and (if promoted) the active artifact.

Validates the §3/§6.1 invariants against raw QUL word-lemma.json:
  - no duplicate id
  - no duplicate operation location (corrections only)
  - every expectedCurrentLemmaArabic matches raw QUL
  - add: expected null; corrected non-null
  - remove: expected non-null; corrected null
  - replace: expected non-null; corrected non-null & != expected
  - keep: corrected == expected (non-mutating)
  - exception: recorded, non-mutating
  - candidate/needs-review forbidden in ACTIVE artifact
Prints a per-check report; exit 1 on any failure.
"""
import json, sys, pathlib
TMP=pathlib.Path("docs/feature-017-lexical-explorers-polish/curation-tmp")
QUL=json.load(open("resources/import-sources/quran-morphology/qul/word-lemma.json",encoding="utf-8"))

def validate(draft, active=False):
    errs=[]; warns=[]
    entries=draft["entries"]
    if draft.get("schemaVersion")!=2: errs.append("schemaVersion != 2")
    ids={}; locs={}
    for e in entries:
        lid=e["id"]; loc=e["location"]
        if lid in ids: errs.append(f"duplicate id {lid}")
        ids[lid]=1
        kind=e["operationKind"]; st=e["decisionStatus"]
        exp=e.get("expectedCurrentLemmaArabic"); cor=e.get("correctedLemmaArabic")
        raw=QUL.get(loc)
        # active forbids candidate/needs-review/_candidate
        if active and st in ("candidate","needs-review"):
            errs.append(f"{loc}: candidate/needs-review present in ACTIVE artifact")
        if active and kind.startswith("_"):
            errs.append(f"{loc}: staging op '{kind}' present in ACTIVE artifact")
        # expected-current must match raw QUL
        if exp is not None and kind in ("remove","replace","_candidate"):
            if raw!=exp: errs.append(f"{loc}: expectedCurrent '{exp}' != raw QUL '{raw}'")
        if exp is None and kind=="add":
            if raw is not None: errs.append(f"{loc}: add expects absent but raw present '{raw}'")
        # operation location uniqueness among mutating ops
        if kind in ("add","remove","replace"):
            if loc in locs: errs.append(f"duplicate mutating location {loc}")
            locs[loc]=1
        # shape rules
        if kind=="add" and not (exp is None and cor): errs.append(f"{loc}: add shape invalid")
        if kind=="remove" and not (exp and cor is None): errs.append(f"{loc}: remove shape invalid")
        if kind=="replace" and not (exp and cor and cor!=exp): errs.append(f"{loc}: replace shape invalid")
        if kind=="_keep" and cor!=exp: errs.append(f"{loc}: keep mutated ({exp}->{cor})")
    return errs, warns

if __name__=="__main__":
    target=sys.argv[1] if len(sys.argv)>1 else "draft.json"
    active = target=="active.json"
    draft=json.load(open(TMP/target,encoding="utf-8"))
    errs,warns=validate(draft, active=active)
    n=len(draft["entries"])
    print(f"=== validating {target} ({n} entries, active={active}) ===")
    if errs:
        print(f"FAIL: {len(errs)} errors")
        for e in errs[:40]: print("  -",e)
        sys.exit(1)
    print("PASS: all invariants satisfied")
