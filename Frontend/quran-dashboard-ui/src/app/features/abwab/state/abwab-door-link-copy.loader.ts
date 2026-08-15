import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { DoorLinkSnapshotDto } from '../../../core/api/generated/models/door-link-snapshot-dto';
import { AbwabDoorLinksApi } from '../data-access/abwab-door-links.api';
import { ABWAB_LABELS } from '../models/abwab.labels';

@Injectable({ providedIn: 'root' })
export class AbwabDoorLinkCopyLoader {
  private readonly api = inject(AbwabDoorLinksApi);

  async loadSnapshot(sourceDoorId: number): Promise<DoorLinkSnapshotDto> {
    const response = await firstValueFrom(this.api.getSnapshot(sourceDoorId));
    if (!response.isSuccess || response.data == null || response.data.doorId !== sourceDoorId) {
      throw new Error(response.message ?? ABWAB_LABELS.doorLinksCopyLoadError);
    }
    return response.data;
  }
}

export function copyFailureMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const envelope = typeof error.error === 'object' && error.error !== null
      ? error.error as Record<string, unknown>
      : null;
    const message = envelope?.['message'];
    if (typeof message === 'string' && message.trim().length > 0) {
      return message;
    }
    if (error.status === HttpStatusCode.Conflict) {
      return ABWAB_LABELS.doorLinksCopySourceChanged;
    }
  }
  return error instanceof Error ? error.message : ABWAB_LABELS.doorLinksCopyLoadError;
}
