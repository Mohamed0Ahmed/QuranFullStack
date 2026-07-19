// Splits on top-level whitespace only, so `minmax(0, 1fr)` stays one track (its
// interior comma/space are inside parens). Known limitation: `repeat(n, …)` collapses
// to a single track rather than expanding — every current explorer column template is
// an explicit space-separated track list, so this covers the real call-sites.
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
