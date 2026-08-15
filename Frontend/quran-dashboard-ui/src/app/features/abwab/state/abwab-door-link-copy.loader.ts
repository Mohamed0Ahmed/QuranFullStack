import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { DoorLinkAyahsPageDto } from '../../../core/api/generated/models/door-link-ayahs-page-dto';
import { DoorLinkRecordsPageDto } from '../../../core/api/generated/models/door-link-records-page-dto';
import { AbwabDoorLinksApi } from '../data-access/abwab-door-links.api';
import {
  ABWAB_DOOR_LINK_AYAH_PAGE_SIZE,
  ABWAB_DOOR_LINK_COPY_BATCH_SIZE,
  AbwabDoorLinkCopyRecord,
  AbwabDoorLinkSelectionState,
} from '../models/abwab-door-links.models';
import { ABWAB_LABELS } from '../models/abwab.labels';

@Injectable({ providedIn: 'root' })
export class AbwabDoorLinkCopyLoader {
  private readonly api = inject(AbwabDoorLinksApi);

  async captureDoorVersion(sourceDoorId: number): Promise<number> {
    const page = await this.loadRecordsPage(sourceDoorId, 1, null);
    this.validateRecordsPage(page, sourceDoorId, 1, null);
    return page.doorVersion;
  }

  async enumerateUnitIds(
    sourceDoorId: number,
    expectedDoorVersion: number,
    selection: AbwabDoorLinkSelectionState,
    isCurrent: () => boolean,
  ): Promise<readonly number[]> {
    if (selection.mode === 'only') {
      return [...new Set(selection.unitIds)];
    }
    const selected: number[] = [];
    const excludedIds = new Set(selection.unitIds);
    let page = 1;
    let loadedRecordCount = 0;
    let totalCount: number | null = null;
    do {
      const result = await this.loadRecordsPage(sourceDoorId, page, expectedDoorVersion);
      this.validateRecordsPage(result, sourceDoorId, page, expectedDoorVersion, totalCount);
      if (!isCurrent()) {
        return [];
      }
      totalCount ??= result.totalCount;
      result.items.forEach((record) => {
        if (!excludedIds.has(record.unitId)) {
          selected.push(record.unitId);
        }
      });
      loadedRecordCount += result.items.length;
      page++;
    } while (loadedRecordCount < (totalCount ?? 0));
    return selected;
  }

  async hydrateRecords(
    sourceDoorId: number,
    expectedDoorVersion: number,
    expectedLinkingDataRevision: number | null,
    unitIds: readonly number[],
    isCurrent: () => boolean,
  ): Promise<readonly AbwabDoorLinkCopyRecord[]> {
    const records: AbwabDoorLinkCopyRecord[] = [];
    let linkingDataRevision = expectedLinkingDataRevision;
    for (const unitId of unitIds) {
      const record = await this.hydrateRecord(
        sourceDoorId,
        expectedDoorVersion,
        unitId,
        linkingDataRevision,
        isCurrent,
      );
      if (!isCurrent()) {
        return [];
      }
      linkingDataRevision ??= record.linkingDataRevision;
      records.push(record);
    }
    return records;
  }

  private async hydrateRecord(
    sourceDoorId: number,
    expectedDoorVersion: number,
    unitId: number,
    batchLinkingDataRevision: number | null,
    isCurrent: () => boolean,
  ): Promise<AbwabDoorLinkCopyRecord> {
    const ayahs: DoorLinkAyahsPageDto['items'] = [];
    let page = 1;
    let totalCount: number | null = null;
    let linkingDataRevision = batchLinkingDataRevision;
    let isGrouped: boolean | null = null;
    do {
      const result = await this.loadAyahsPage(
        sourceDoorId,
        unitId,
        page,
        expectedDoorVersion,
        linkingDataRevision,
      );
      this.validateAyahsPage(
        result,
        sourceDoorId,
        unitId,
        page,
        expectedDoorVersion,
        linkingDataRevision,
        totalCount,
        isGrouped,
      );
      if (!isCurrent()) {
        throw new Error(ABWAB_LABELS.doorLinksCopyStopped);
      }
      totalCount ??= result.totalCount;
      linkingDataRevision ??= result.linkingDataRevision;
      isGrouped ??= result.isGrouped;
      ayahs.push(...result.items);
      page++;
    } while (ayahs.length < (totalCount ?? 0));
    if (ayahs.length !== totalCount || linkingDataRevision === null || isGrouped === null) {
      throw new Error(ABWAB_LABELS.doorLinksCopySourceChanged);
    }
    return { unitId, isGrouped, linkingDataRevision, ayahs };
  }

  private async loadRecordsPage(
    doorId: number,
    page: number,
    expectedDoorVersion: number | null,
  ): Promise<DoorLinkRecordsPageDto> {
    const response = await firstValueFrom(this.api.getRecords(doorId, {
      page,
      pageSize: ABWAB_DOOR_LINK_COPY_BATCH_SIZE,
      expectedDoorVersion,
    }));
    if (!response.isSuccess || response.data == null) {
      throw new Error(response.message ?? ABWAB_LABELS.doorLinksCopyLoadError);
    }
    return response.data;
  }

  private async loadAyahsPage(
    doorId: number,
    unitId: number,
    page: number,
    expectedDoorVersion: number,
    expectedLinkingDataRevision: number | null,
  ): Promise<DoorLinkAyahsPageDto> {
    const response = await firstValueFrom(this.api.getAyahs(doorId, unitId, {
      page,
      pageSize: ABWAB_DOOR_LINK_AYAH_PAGE_SIZE,
      expectedDoorVersion,
      expectedLinkingDataRevision,
    }));
    if (!response.isSuccess || response.data == null) {
      throw new Error(response.message ?? ABWAB_LABELS.doorLinksCopyLoadError);
    }
    return response.data;
  }

  private validateRecordsPage(
    page: DoorLinkRecordsPageDto,
    doorId: number,
    expectedPage: number,
    expectedDoorVersion: number | null,
    expectedTotalCount: number | null = null,
  ): void {
    if (
      page.doorId !== doorId
      || page.page !== expectedPage
      || expectedDoorVersion !== null && page.doorVersion !== expectedDoorVersion
      || expectedTotalCount !== null && page.totalCount !== expectedTotalCount
      || page.items.length > page.pageSize
      || page.items.length === 0 && page.totalCount > 0
    ) {
      throw new Error(ABWAB_LABELS.doorLinksCopySourceChanged);
    }
  }

  private validateAyahsPage(
    page: DoorLinkAyahsPageDto,
    doorId: number,
    unitId: number,
    expectedPage: number,
    expectedDoorVersion: number,
    expectedLinkingDataRevision: number | null,
    expectedTotalCount: number | null,
    expectedGrouped: boolean | null,
  ): void {
    if (
      page.doorId !== doorId
      || page.unitId !== unitId
      || page.page !== expectedPage
      || page.doorVersion !== expectedDoorVersion
      || expectedLinkingDataRevision !== null && page.linkingDataRevision !== expectedLinkingDataRevision
      || expectedTotalCount !== null && page.totalCount !== expectedTotalCount
      || expectedGrouped !== null && page.isGrouped !== expectedGrouped
      || page.items.length === 0 && page.totalCount > 0
    ) {
      throw new Error(ABWAB_LABELS.doorLinksCopySourceChanged);
    }
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
