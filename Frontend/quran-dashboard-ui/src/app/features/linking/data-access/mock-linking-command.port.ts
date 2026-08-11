import { Injectable, inject } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';

import { AbwabSnapshotFacade } from '../../abwab/state/abwab-snapshot.facade';
import { AbwabNode } from '../../abwab/models/abwab.models';
import { LINKING_LABELS } from '../models/linking.labels';
import { LinkingAccessService } from '../state/linking-access.service';
import type { LinkingCommand, LinkingCommandPort } from './linking-command.port';

@Injectable({ providedIn: 'root' })
export class MockLinkingCommandPort implements LinkingCommandPort {
  private readonly access = inject(LinkingAccessService);
  private readonly doors = inject(AbwabSnapshotFacade);

  execute(command: LinkingCommand): Observable<{ kind: 'linked'; message: string }> {
    if (!this.access.canUseLinking()) {
      return throwError(() => new Error('لا تملك صلاحية تنفيذ الربط.'));
    }
    if (!this.doors.snapshot()?.liveRoots.some((node) => containsDoor(node, command.doorId))) {
      return throwError(() => new Error('الباب المحدد لم يعد متاحاً.'));
    }
    if (command.operation.mergedSelection.ayahs.length === 0 || command.operation.sourceIntents.length === 0) {
      return throwError(() => new Error('اختر آية واحدة على الأقل.'));
    }
    return of({ kind: 'linked' as const, message: LINKING_LABELS.success });
  }
}

function containsDoor(node: AbwabNode, doorId: number): boolean {
  return node.id === doorId || node.children.some((child) => containsDoor(child, doorId));
}
