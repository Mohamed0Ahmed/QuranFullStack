import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import {
  MushafAppliedDoorViewModel,
  MushafDoorColorSlot,
} from '../../models/mushaf-door-highlights.models';

@Component({
  selector: 'qd-mushaf-door-palette',
  standalone: true,
  templateUrl: './mushaf-door-palette.component.html',
  styleUrl: './mushaf-door-palette.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MushafDoorPaletteComponent {
  readonly door = input.required<MushafAppliedDoorViewModel>();
  readonly palette = input.required<readonly MushafDoorColorSlot[]>();
  readonly reservedColorSlots = input.required<ReadonlySet<MushafDoorColorSlot>>();

  readonly colorSelected = output<MushafDoorColorSlot>();

  protected colorLabel(colorSlot: MushafDoorColorSlot): string {
    return this.isReserved(colorSlot)
      ? `اللون ${colorSlot}، مستخدم لباب آخر`
      : `اللون ${colorSlot}`;
  }

  protected isReserved(colorSlot: MushafDoorColorSlot): boolean {
    return this.door().colorSlot !== colorSlot && this.reservedColorSlots().has(colorSlot);
  }
}
