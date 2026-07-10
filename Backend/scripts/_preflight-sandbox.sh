preflight_check_stale_sandbox_assets() {
  local backend_root="$1"

  if grep -rl '/tmp/cursor-sandbox-cache' --include='project.assets.json' "${backend_root}" >/dev/null 2>&1; then
    echo "ERROR: Generated build assets contain stale Cursor sandbox NuGet paths." >&2
    echo "       One or more obj/project.assets.json files reference /tmp/cursor-sandbox-cache/..." >&2
    echo "       This usually happens after restore/build inside a Cursor sandbox." >&2
    echo "Run: ./scripts/clean-local-build" >&2
    exit 1
  fi
}
