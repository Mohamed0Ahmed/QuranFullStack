/**
 * Splits a CSS `grid-template-columns` value into its individual track
 * strings, so a skeleton row can render one placeholder cell per real
 * column. Splits on top-level whitespace only — a function call like
 * `minmax(0, 1fr)` counts as a single track because its interior comma/space
 * are inside parentheses.
 *
 * Contract note: `repeat(n, ...)` collapses to a single track (the repeat
 * count is not expanded). Every current explorer table/list column template
 * in `_explorer-tables.scss` / `_explorer-detail-lists.scss` is an explicit
 * space-separated track list, so this covers the real call-sites `qd-skeleton-rows`
 * targets; a `repeat()`-based template is a known limitation, not a silent bug.
 */
export function splitGridTemplateColumns(template: string): string[] {
  const trimmed = template.trim();
  if (!trimmed) {
    return [];
  }

  const tracks: string[] = [];
  let current = '';
  let depth = 0;

  for (const char of trimmed) {
    if (char === '(') {
      depth++;
    } else if (char === ')') {
      depth--;
    }

    if (/\s/.test(char) && depth === 0) {
      if (current) {
        tracks.push(current);
        current = '';
      }
      continue;
    }

    current += char;
  }

  if (current) {
    tracks.push(current);
  }

  return tracks;
}
