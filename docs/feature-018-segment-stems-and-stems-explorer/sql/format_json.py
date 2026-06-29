"""One-off formatter: pretty-print the psql-emitted raw candidate JSON.

Reads the single-line JSON produced by build_json.sql (via psql \\o) and rewrites it
order-preserving + UTF-8 (Arabic kept readable). No DB access, no runtime app code.
"""
import json
import os

SRC = "/tmp/f018_candidates_raw.json"
DST = (
    "/projects/Dashboard/App/docs/feature-018-segment-stems-and-stems-explorer/"
    "segment-stem-curation-candidates.json"
)

with open(SRC, encoding="utf-8") as fh:
    doc = json.load(fh)

with open(DST, "w", encoding="utf-8") as fh:
    json.dump(doc, fh, ensure_ascii=False, indent=2)
    fh.write("\n")

print("candidates:", len(doc["candidates"]))
print("counts.secondary:", doc["counts"]["secondary_stem_candidates_generated"])
print("byStatus:", doc["riskSummary"]["byStatus"])
print("byFlag:", doc["riskSummary"]["byFlag"])
print("top keys:", list(doc.keys()))
print("bytes:", os.path.getsize(DST))
