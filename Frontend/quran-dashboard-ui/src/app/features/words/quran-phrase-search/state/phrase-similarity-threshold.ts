export function minimumMatchedWords(length: number, minimumPercent: number): number {
  return Math.ceil((length * minimumPercent) / 100);
}

export function maximumDifferenceCount(length: number, minimumPercent: number): number {
  return length - minimumMatchedWords(length, minimumPercent);
}

export function percentForMaximumDifferences(
  length: number,
  maximumDifferences: number,
): number {
  const bounded = Math.max(0, Math.min(Math.floor(length / 2), maximumDifferences));
  const matched = length - bounded;
  const exactBandMidpoint = ((matched - 0.5) * 100) / length;
  return Number(Math.max(50, exactBandMidpoint).toFixed(6));
}

export function manualDifferenceOptions(length: number): readonly number[] {
  return Array.from({ length: Math.max(1, Math.floor(length / 2)) }, (_, index) => index + 1);
}
