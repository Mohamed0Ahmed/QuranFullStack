import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Subscription } from 'rxjs';

import { DoorLinkAyahDto } from '../../../core/api/generated/models/door-link-ayah-dto';
import { DoorLinkAyahsPageDto } from '../../../core/api/generated/models/door-link-ayahs-page-dto';
import { AbwabDoorLinksApi } from '../data-access/abwab-door-links.api';
import { ABWAB_DOOR_LINK_AYAH_PAGE_SIZE } from '../models/abwab-door-links.models';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabDoorLinksStore } from './abwab-door-links.store';

@Injectable({ providedIn: 'root' })
export class AbwabDoorLinkEditController {
  private readonly api = inject(AbwabDoorLinksApi);
  private readonly store = inject(AbwabDoorLinksStore);
  private request: Subscription | null = null;
  private generation = 0;

  start(
    doorId: number,
    expectedDoorVersion: number,
    unitId: number,
    onStale: (message: string) => void,
  ): void {
    this.cancel(false);
    const generation = this.generation;
    this.store.beginEditPreparation(unitId, expectedDoorVersion);
    this.loadPage({
      doorId,
      expectedDoorVersion,
      unitId,
      page: 1,
      expectedLinkingDataRevision: null,
      expectedTotalCount: null,
      ayahs: [],
      generation,
      onStale,
    });
  }

  cancel(resetState = true): void {
    this.generation++;
    this.request?.unsubscribe();
    this.request = null;
    if (resetState) {
      this.store.cancelEdit();
    }
  }

  private loadPage(context: AbwabDoorLinkEditPageContext): void {
    this.request = this.api.getAyahs(context.doorId, context.unitId, {
      page: context.page,
      pageSize: ABWAB_DOOR_LINK_AYAH_PAGE_SIZE,
      expectedDoorVersion: context.expectedDoorVersion,
      expectedLinkingDataRevision: context.expectedLinkingDataRevision,
    }).subscribe({
      next: (response) => {
        if (!this.isCurrent(context)) {
          return;
        }
        if (!response.isSuccess || response.data == null) {
          this.store.failEditPreparation(
            context.unitId,
            response.message ?? ABWAB_LABELS.doorLinkAyahsLoadError,
          );
          return;
        }
        this.receivePage(context, response.data);
      },
      error: (error: unknown) => this.handleError(context, error),
    });
  }

  private receivePage(context: AbwabDoorLinkEditPageContext, page: DoorLinkAyahsPageDto): void {
    if (!this.isValidPage(context, page)) {
      this.store.failEditPreparation(context.unitId, ABWAB_LABELS.doorLinkAyahsLoadError);
      return;
    }
    const ayahs = [...context.ayahs, ...page.items];
    if (ayahs.length === page.totalCount) {
      this.request = null;
      this.store.completeEditPreparation(context.unitId, context.expectedDoorVersion, ayahs);
      return;
    }
    if (page.items.length === 0 || ayahs.length > page.totalCount) {
      this.store.failEditPreparation(context.unitId, ABWAB_LABELS.doorLinkAyahsLoadError);
      return;
    }
    this.loadPage({
      ...context,
      page: context.page + 1,
      expectedLinkingDataRevision: page.linkingDataRevision,
      expectedTotalCount: page.totalCount,
      ayahs,
    });
  }

  private handleError(context: AbwabDoorLinkEditPageContext, error: unknown): void {
    if (!this.isCurrent(context)) {
      return;
    }
    const message = doorLinkResponseMessage(error) ?? ABWAB_LABELS.doorLinkAyahsLoadError;
    this.request = null;
    if (isDoorLinkStaleResponse(error)) {
      this.generation++;
      this.store.markStale(message || ABWAB_LABELS.doorLinksStale);
      context.onStale(message || ABWAB_LABELS.doorLinksStale);
      return;
    }
    this.store.failEditPreparation(context.unitId, message);
  }

  private isCurrent(context: AbwabDoorLinkEditPageContext): boolean {
    const state = this.store.state();
    return context.generation === this.generation
      && state.openDoorId === context.doorId
      && state.edit.unitId === context.unitId
      && state.edit.status === 'preparing';
  }

  private isValidPage(context: AbwabDoorLinkEditPageContext, page: DoorLinkAyahsPageDto): boolean {
    return page.doorId === context.doorId
      && page.doorVersion === context.expectedDoorVersion
      && page.unitId === context.unitId
      && page.page === context.page
      && (
        context.expectedLinkingDataRevision === null
        || page.linkingDataRevision === context.expectedLinkingDataRevision
      )
      && (context.expectedTotalCount === null || page.totalCount === context.expectedTotalCount);
  }
}

interface AbwabDoorLinkEditPageContext {
  readonly doorId: number;
  readonly expectedDoorVersion: number;
  readonly unitId: number;
  readonly page: number;
  readonly expectedLinkingDataRevision: number | null;
  readonly expectedTotalCount: number | null;
  readonly ayahs: readonly DoorLinkAyahDto[];
  readonly generation: number;
  readonly onStale: (message: string) => void;
}

function doorLinkResponseMessage(error: unknown): string | null {
  if (!(error instanceof HttpErrorResponse) || typeof error.error !== 'object' || error.error === null) {
    return null;
  }
  const message = (error.error as Record<string, unknown>)['message'];
  return typeof message === 'string' && message.trim().length > 0 ? message : null;
}

function isDoorLinkStaleResponse(error: unknown): boolean {
  if (!(error instanceof HttpErrorResponse) || error.status !== HttpStatusCode.Conflict) {
    return false;
  }
  const envelope = typeof error.error === 'object' && error.error !== null
    ? error.error as Record<string, unknown>
    : null;
  const data = envelope !== null && typeof envelope['data'] === 'object' && envelope['data'] !== null
    ? envelope['data'] as Record<string, unknown>
    : null;
  return data?.['code'] === 'DOOR_LINKS_STALE' || data?.['code'] === 'LINKING_DATA_STALE';
}
