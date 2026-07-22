// Splits on top-level whitespace (parens keep `minmax(0, 1fr)` one track); `repeat(n, …)` is NOT expanded.
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
