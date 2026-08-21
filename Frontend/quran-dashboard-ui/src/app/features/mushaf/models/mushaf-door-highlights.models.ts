export const MUSHAF_DOOR_COLOR_SLOTS = [
  1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
] as const;

export type MushafDoorColorSlot = (typeof MUSHAF_DOOR_COLOR_SLOTS)[number];
export type MushafDoorResolvedColorSlot = MushafDoorColorSlot | 'multi';

export interface MushafAppliedDoorViewModel {
  readonly id: number;
  readonly name: string;
  readonly colorSlot: MushafDoorColorSlot;
}

export interface MushafDoorResolvedHighlight {
  readonly doors: readonly { readonly id: number; readonly name: string }[];
  readonly colorSlot: MushafDoorResolvedColorSlot;
}

export interface MushafDoorDetailsRequest {
  readonly position: { readonly x: number; readonly y: number };
  readonly highlight: MushafDoorResolvedHighlight;
}
