/** Visual-linking palette for segment color slots (not POS-semantic). */
export const SEGMENT_COLOR_PALETTE = [
  '#c2410c',
  '#15803d',
  '#be123c',
  '#2563eb',
  '#7e22ce',
  '#0f766e',
] as const;

export function segmentSlotToColor(slot: number): string {
  const index = (slot - 1) % SEGMENT_COLOR_PALETTE.length;
  return SEGMENT_COLOR_PALETTE[index >= 0 ? index : 0];
}
