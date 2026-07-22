#!/usr/bin/env bash
# FR (§Workspace Path Conventions) staged-source-strategy gate.
# `resources/` is gitignored and absent in CI, so importers must read source data ONLY from staged
# packages under `resources/import-sources/`, never a random upstream/absolute folder. This is a
# SOURCE-level check (it inspects importer code, it does NOT run any import) so it is safe in CI.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
IMPORTER="$REPO_ROOT/Backend/tools/QuranDashboard.DataImporter"

python3 - "$IMPORTER" <<'PY'
import os, re, sys

importer = sys.argv[1]
files = []
for root, dirs, names in os.walk(importer):
    dirs[:] = [d for d in dirs if d not in ("obj", "bin")]
    files.extend(os.path.join(root, n) for n in names if n.endswith(".cs"))

texts = {p: open(p, encoding="utf-8").read() for p in files}
all_text = "\n".join(texts.values())

violations = []

# The staged-source strategy must actually be in force.
if "import-sources" not in all_text:
    violations.append("no importer code references resources/import-sources/ (staged-source strategy missing)")

# Every default source-path resolver must route through resources/import-sources/, either directly
# or by delegating to another canonical ResolveDefault*SourcePath resolver.
resolver = re.compile(r"(ResolveDefault\w*SourcePath)\s*\(\s*\)\s*=>(.*?);", re.S)
delegated_call = re.compile(r"\w+\.ResolveDefault\w*SourcePath\s*\(\s*\)")
for path, text in texts.items():
    for match in resolver.finditer(text):
        name, body = match.group(1), match.group(2)
        anchored = "import-sources" in body
        delegated = delegated_call.search(body) is not None
        if not anchored and not delegated:
            rel = os.path.relpath(path, importer)
            violations.append(f"{rel}: {name} does not resolve under resources/import-sources/")

# No importer source may hard-code an absolute filesystem source path.
absolute = re.compile(r'"(?:/[A-Za-z]|[A-Za-z]:\\\\)[^"]*"')
for path, text in texts.items():
    for match in absolute.finditer(text):
        rel = os.path.relpath(path, importer)
        violations.append(f"{rel}: absolute path literal is forbidden for source resolution: {match.group(0)}")

if violations:
    print("Import source-strategy gate FAILED: imports must read only from resources/import-sources/.")
    for v in violations:
        print("  " + v)
    sys.exit(1)

print("Import source-strategy gate passed: importer resolves sources only under resources/import-sources/.")
PY
