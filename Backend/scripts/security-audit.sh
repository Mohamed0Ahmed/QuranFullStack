#!/usr/bin/env bash
# FR-042 dependency/security + secret/license gate (§15.2 gate 8).
#
# Gating policy:
#  - Secret scan .............. BLOCKING + FAIL-CLOSED (a committed secret must never merge; a scan
#    error aborts rather than passing).
#  - License gate ............. BLOCKING (forbidden strong-copyleft prod deps).
#  - Backend vulnerable pkgs .. BLOCKING (backend is currently clean; keep it clean).
#  - Frontend `npm audit` ..... REPORT-ONLY. The frontend tree carries pre-existing transitive
#    advisories whose remediation is dependency upgrades owned by the dependency-audit workflow, not
#    this feature. It still RUNS and surfaces in CI logs; it does not fail this feature's pipeline.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
FRONTEND="$REPO_ROOT/Frontend/quran-dashboard-ui"

failed=0

echo "== Secret scan (blocking, fail-closed) =="
# git grep exit codes: 0 = match, 1 = no match, >1 = error. A prior version ran `git grep` WITHOUT
# `-e`, so a pattern beginning with `-----BEGIN` was parsed as an unknown option (exit 129) and the
# `if git grep; then FAIL else pass` swallowed it as "no secrets" — the control failed OPEN. Every
# pattern is now passed with `-e`, and a git grep error (>1) aborts fail-closed instead of passing.
#
# Strong tokens are unambiguous secrets (no placeholder tolerance needed).
strong_secret='-----BEGIN[A-Z0-9 ]*PRIVATE KEY-----|AKIA[0-9A-Z]{16}|ghp_[0-9A-Za-z]{36}|gho_[0-9A-Za-z]{36}|github_pat_[0-9A-Za-z_]{60,}|xox[baprs]-[0-9A-Za-z-]{10,}|npg_[0-9A-Za-z]{16,}|AIza[0-9A-Za-z_-]{35}'
# `Password=<value>` (>=6 chars), then filtered against the dev-default/placeholder values that pepper
# this repo (SET_VIA_USER_SECRETS, Password=postgres, <your-password>, changeme, example, ${ENV}) so
# the gate catches a REAL password without flagging templates.
password_secret=$'[Pp]assword=[^"\';[:space:],]{6,}'
# Tolerated (case-insensitively) after an optional * / < / ( prefix: dev-defaults, angle-bracket
# templates, env interpolation, and `***REDACTED***` markers.
password_placeholder='[Pp]assword=[*<(]*(SET_VIA_USER_SECRETS|postgres|changeme|example|your[-_]?password|placeholder|dummy|redacted|\$\{|\$\()'

git_secret_grep() {
  # Emit "path:line:text" for one ERE pattern; return 2 (caller aborts) on a git grep error (>1).
  # Excludes the lockfile, this scanner (holds the patterns), and GENERATED bundles (redoc HTML +
  # ng-openapi models) whose minified content false-positives. Human-authored docs are NOT excluded.
  local pattern="$1" out rc=0
  out="$(git -C "$REPO_ROOT" grep -nIE -e "$pattern" -- \
      ':(exclude)Frontend/quran-dashboard-ui/package-lock.json' \
      ':(exclude)Backend/scripts/security-audit.sh' \
      ':(exclude)docs/api-reference/index.html' \
      ':(exclude)Frontend/quran-dashboard-ui/src/app/core/api/generated')" || rc=$?
  if [ "$rc" -gt 1 ]; then
    return 2
  fi
  printf '%s' "$out"
}

if ! strong_hits="$(git_secret_grep "$strong_secret")"; then
  echo "SECRET SCAN ERROR: git grep failed on the strong-token pattern; failing closed." >&2
  exit 2
fi
if ! password_raw="$(git_secret_grep "$password_secret")"; then
  echo "SECRET SCAN ERROR: git grep failed on the password pattern; failing closed." >&2
  exit 2
fi
password_hits="$(printf '%s' "$password_raw" | grep -Evi "$password_placeholder" || true)"
secret_hits="$(printf '%s\n%s\n' "$strong_hits" "$password_hits" | grep -Ev '^[[:space:]]*$' || true)"

if [ -n "$secret_hits" ]; then
  echo "SECRET SCAN FAILED: potential committed secret(s):"
  printf '%s\n' "$secret_hits" | sed 's/^/  /'
  failed=1
else
  echo "Secret scan passed: no committed secrets detected."
fi

echo ""
echo "== License gate (blocking) =="
if [ -d "$FRONTEND/node_modules" ]; then
  ( cd "$FRONTEND" && node scripts/check-licenses.mjs ) || failed=1
else
  echo "SKIPPED: node_modules not installed (run npm ci first)."
fi

echo ""
echo "== Backend dependency audit (blocking) =="
backend_audit="$(dotnet list "$REPO_ROOT/Backend/QuranDashboard.sln" package --vulnerable --include-transitive 2>&1)"
echo "$backend_audit"
if echo "$backend_audit" | grep -qi 'has the following vulnerable'; then
  echo "BACKEND AUDIT FAILED: vulnerable NuGet package(s) detected."
  failed=1
else
  echo "Backend dependency audit passed."
fi

echo ""
echo "== Frontend dependency audit (report-only) =="
if [ -d "$FRONTEND/node_modules" ] || [ -f "$FRONTEND/package-lock.json" ]; then
  ( cd "$FRONTEND" && npm audit ) || echo "(frontend npm audit reported advisories — see dependency-audit workflow; non-blocking here)"
else
  echo "SKIPPED: frontend dependencies not present."
fi

echo ""
if [ "$failed" -ne 0 ]; then
  echo "Security audit FAILED (see blocking gate output above)."
  exit 1
fi
echo "Security audit passed (blocking gates clean; frontend advisories reported)."
