/** Visual-linking palette for segment color slots (not POS-semantic). */
export const SEGMENT_COLOR_PALETTE = [
  '#3d6b8e',
  '#9a6b3c',
  '#3a7d56',
  '#7d4a6b',
  '#5a5a9e',
  '#8b6914',
] as const;

export function segmentSlotToColor(slot: number): string {
  const index = (slot - 1) % SEGMENT_COLOR_PALETTE.length;
  return SEGMENT_COLOR_PALETTE[index >= 0 ? index : 0];
}
