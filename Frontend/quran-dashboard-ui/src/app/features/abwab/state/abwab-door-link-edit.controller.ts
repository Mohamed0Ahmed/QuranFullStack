import { Injectable, inject } from '@angular/core';

import { DoorLinkAyahDto } from '../../../core/api/generated/models/door-link-ayah-dto';
import { AbwabDoorLinksStore } from './abwab-door-links.store';

@Injectable({ providedIn: 'root' })
export class AbwabDoorLinkEditController {
  private readonly store = inject(AbwabDoorLinksStore);

  start(
    expectedDoorVersion: number,
    unitId: number,
    ayahs: readonly DoorLinkAyahDto[],
  ): void {
    this.store.beginEditPreparation(unitId, expectedDoorVersion);
    this.store.completeEditPreparation(unitId, expectedDoorVersion, ayahs);
  }

  cancel(): void {
    this.store.cancelEdit();
  }
}
