export function encodePhraseQuery(rawQuery: string): string {
  const bytes = new TextEncoder().encode(rawQuery);
  let binary = '';
  for (const byte of bytes) {
    binary += String.fromCharCode(byte);
  }
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}

export function phraseQueryByteLength(rawQuery: string): number {
  return new TextEncoder().encode(rawQuery).byteLength;
}
