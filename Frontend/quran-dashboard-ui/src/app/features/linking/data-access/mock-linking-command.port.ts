import { Injectable, inject } from '@angular/core';

import { AbwabSnapshotFacade } from '../../abwab/state/abwab-snapshot.facade';
import { LINKING_LABELS } from '../models/linking.labels';
import { LinkingAccessService } from '../state/linking-access.service';
import type { LinkingCommand, LinkingCommandPort } from './linking-command.port';

@Injectable({ providedIn: 'root' })
export class MockLinkingCommandPort implements LinkingCommandPort {
  private readonly access = inject(LinkingAccessService);
  private readonly doors = inject(AbwabSnapshotFacade);

  execute(command: LinkingCommand) {
    if (!this.access.canUseLinking()) {
      throw new Error('لا تملك صلاحية تنفيذ الربط.');
    }
    if (!this.doors.snapshot()?.byId.has(command.doorId)) {
      throw new Error('الباب المحدد لم يعد متاحاً.');
    }
    if (command.selectedVerseKeys.length === 0) {
      throw new Error('اختر آية واحدة على الأقل.');
    }
    return { kind: 'linked' as const, message: LINKING_LABELS.success };
  }
}
