import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of } from 'rxjs';

import { AbwabApi } from '../data-access/abwab.api';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabSelectionStore } from './abwab-selection.store';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabNode } from '../models/abwab.models';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { CreateSectionCommand } from '../../../core/api/generated/models/create-section-command';
import { RenameSectionBody } from '../../../core/api/generated/models/rename-section-body';
import { CreateDoorCommand } from '../../../core/api/generated/models/create-door-command';
import { EditDoorBody } from '../../../core/api/generated/models/edit-door-body';
import { MoveDoorBody } from '../../../core/api/generated/models/move-door-body';
import { ReorderDoorBody } from '../../../core/api/generated/models/reorder-door-body';
import { AbwabBulkDoorRef } from '../../../core/api/generated/models/abwab-bulk-door-ref';
import { AbwabDoorDto } from '../../../core/api/generated/models/abwab-door-dto';
import { AbwabSectionDto } from '../../../core/api/generated/models/abwab-section-dto';
import { AbwabRestoredDoorDto } from '../../../core/api/generated/models/abwab-restored-door-dto';

type AbwabWriteFailureOutcome =
  | { readonly kind: 'conflict'; readonly message: string }
  | { readonly kind: 'invalid'; readonly message: string }
  | { readonly kind: 'error'; readonly message: string };

export type AbwabWriteOutcome<T> = { readonly kind: 'success'; readonly data: T } | AbwabWriteFailureOutcome;

/** Counts a door plus every live descendant — the live tree only ever holds live nodes
 * (abwab-tree.builder.ts partitions archived ones out), so no extra `isArchived` guard is
 * needed past the root (plan-slice-b.md §6.4, the archive-count cell). */
function countLiveSubtree(node: AbwabNode): number {
  return 1 + node.children.reduce((sum, child) => sum + countLiveSubtree(child), 0);
}

/**
 * Every door write, plus the section commands (`state/abwab-sections.controller.ts`
 * delegates its `createSection`/`renameSection`/`deleteSection` calls here rather than
 * re-implementing them) — the outcome→message mapping and the §2.7 409 policy live in
 * exactly one place for both aggregates, not scattered per-command. Unsaved input stays
 * with the caller (this controller never owns form state), still-valid context is
 * untouched, an invalidated single selection is cleared, the message is surfaced via
 * `announcement`, and nothing here ever auto-retries — the caller decides whether to retry.
 *
 * Bulk conflicts are deliberately different (§6.2 M17): the backend's bulk-conflict
 * response carries no per-door identification (verified against
 * `AbwabDoorsController.cs` / `ApiMessages.AbwabDoorStaleVersion` — one generic message,
 * no `errors` array), so "X" in the locked bulk-conflict copy names every door in the
 * *attempted* selection, and that selection is preserved rather than cleared — the failure
 * is all-or-nothing, so nothing is known to be individually invalid.
 */
@Injectable({ providedIn: 'root' })
export class AbwabWriteController {
  private readonly api = inject(AbwabApi);
  private readonly facade = inject(AbwabSnapshotFacade);
  private readonly selection = inject(AbwabSelectionStore);

  private readonly announcementState = signal<string | null>(null);
  readonly announcement = this.announcementState.asReadonly();

  readonly liveSubtreeCountFor = (doorId: number): number => {
    const node = this.facade.snapshot()?.byId.get(doorId);
    return node && !node.isArchived ? countLiveSubtree(node) : 0;
  };

  readonly archiveConfirmMessageFor = (doorId: number): string =>
    ABWAB_LABELS.archiveConfirm(this.liveSubtreeCountFor(doorId));

  /** Union, not sum (T504): if one selected door is an ancestor of another selected
   * door, summing `liveSubtreeCountFor` per id double-counts their shared descendants.
   * Walking every selected subtree into one id set and counting the set is correct
   * regardless of ancestor/descendant overlap in the selection. */
  readonly bulkArchiveConfirmMessage = (doorIds: readonly number[]): string =>
    ABWAB_LABELS.archiveConfirm(this.bulkLiveSubtreeCount(doorIds));

  private bulkLiveSubtreeCount(doorIds: readonly number[]): number {
    const snapshot = this.facade.snapshot();
    if (!snapshot) {
      return 0;
    }
    const counted = new Set<number>();
    const walk = (node: AbwabNode): void => {
      if (counted.has(node.id)) {
        return;
      }
      counted.add(node.id);
      node.children.forEach(walk);
    };
    for (const id of doorIds) {
      const node = snapshot.byId.get(id);
      if (node && !node.isArchived) {
        walk(node);
      }
    }
    return counted.size;
  }

  createSection(command: CreateSectionCommand): Observable<AbwabWriteOutcome<AbwabSectionDto>> {
    return this.dispatch(this.api.createSection(command));
  }

  renameSection(id: number, body: RenameSectionBody): Observable<AbwabWriteOutcome<AbwabSectionDto>> {
    return this.dispatch(this.api.renameSection(id, body));
  }

  deleteSection(id: number): Observable<AbwabWriteOutcome<unknown>> {
    return this.dispatch(this.api.deleteSection(id));
  }

  createDoor(command: CreateDoorCommand): Observable<AbwabWriteOutcome<AbwabDoorDto>> {
    return this.dispatch(this.api.createDoor(command));
  }

  updateDoor(id: number, body: EditDoorBody): Observable<AbwabWriteOutcome<AbwabDoorDto>> {
    return this.dispatch(this.api.updateDoor(id, body), id);
  }

  moveDoor(id: number, body: MoveDoorBody): Observable<AbwabWriteOutcome<AbwabDoorDto>> {
    return this.dispatch(this.api.moveDoor(id, body), id);
  }

  reorderDoor(id: number, body: ReorderDoorBody): Observable<AbwabWriteOutcome<AbwabDoorDto>> {
    return this.dispatch(this.api.reorderDoor(id, body), id);
  }

  archiveDoor(id: number, version: number): Observable<AbwabWriteOutcome<unknown>> {
    return this.dispatch(this.api.archiveDoor(id, { version }), id);
  }

  restoreDoor(id: number, version: number): Observable<AbwabWriteOutcome<AbwabRestoredDoorDto>> {
    return this.api.restoreDoor(id, { version }).pipe(
      map((response) => this.handleSuccess(response, (data) => {
        this.announcementState.set(data.detachedFromArchivedSection ? ABWAB_LABELS.restoreDetachedAnnouncement : null);
      })),
      catchError((err: unknown) => of(this.handleFailure<AbwabRestoredDoorDto>(err, id))),
    );
  }

  bulkMoveDoors(
    targetParentId: number | null,
    targetSectionId: number | null,
  ): Observable<AbwabWriteOutcome<AbwabDoorDto[]>> {
    const doors = this.currentBulkRefs();
    return this.api.bulkMoveDoors({ doors, targetParentId, targetSectionId }).pipe(
      map((response) => this.handleSuccess(response)),
      catchError((err: unknown) => of(this.handleBulkFailure<AbwabDoorDto[]>(err))),
    );
  }

  bulkArchiveDoors(): Observable<AbwabWriteOutcome<number[]>> {
    const doors = this.currentBulkRefs();
    return this.api.bulkArchiveDoors({ doors }).pipe(
      map((response) => this.handleSuccess(response)),
      catchError((err: unknown) => of(this.handleBulkFailure<number[]>(err))),
    );
  }

  private currentBulkRefs(): AbwabBulkDoorRef[] {
    return Array.from(this.selection.bulkSet(), ([doorId, version]) => ({ doorId, version }));
  }

  private dispatch<T>(request$: Observable<ApiResponse<T> | null>, conflictClearsSelectionId?: number): Observable<AbwabWriteOutcome<T>> {
    return request$.pipe(
      map((response) => this.handleSuccess(response)),
      catchError((err: unknown) => of(this.handleFailure<T>(err, conflictClearsSelectionId))),
    );
  }

  private handleSuccess<T>(response: ApiResponse<T> | null, onData?: (data: T) => void): AbwabWriteOutcome<T> {
    // `isSuccess` alone is authoritative here — unlike a read, a void write's success
    // payload (archive, section delete) is legitimately `null`; requiring non-null data
    // would silently treat those successes as failures.
    //
    // The whole envelope is `null` too on those two routes: they answer `204 No Content`
    // (AbwabDoorsController.cs:176, AbwabSectionsController.cs:67) and HttpClient parses an
    // empty body as `null`. Only a success is ever a 204 — every failure arrives as a 4xx
    // through `catchError` — so a null response is a payload-less success. Reading
    // `response.isSuccess` first threw here, and the throw was swallowed as a transport
    // error while the write had in fact committed.
    const data = (response?.data ?? null) as T;
    if (response === null || response.isSuccess) {
      if (onData && data !== null) {
        onData(data);
      } else {
        this.announcementState.set(null);
      }
      this.facade.refresh().subscribe((snapshot) => {
        if (snapshot) {
          this.selection.rebindTo(snapshot);
        }
      });
      return { kind: 'success', data };
    }
    const message = response.message ?? ABWAB_LABELS.writeInvalidFallback;
    this.announcementState.set(message);
    return { kind: 'invalid', message };
  }

  private handleFailure<T>(err: unknown, conflictClearsSelectionId?: number): AbwabWriteOutcome<T> {
    const outcome = this.toFailureOutcome(err);
    if (outcome.kind === 'conflict' && conflictClearsSelectionId !== undefined) {
      if (this.selection.selectedDoorId() === conflictClearsSelectionId) {
        this.selection.clearSelection();
      }
    }
    this.announcementState.set(outcome.message);
    return outcome;
  }

  private handleBulkFailure<T>(err: unknown): AbwabWriteOutcome<T> {
    const outcome = this.toFailureOutcome(err);
    if (outcome.kind !== 'conflict') {
      this.announcementState.set(outcome.message);
      return outcome;
    }
    // Bulk selection is preserved on conflict (M17) — see the class doc for why.
    const message = this.bulkConflictMessage();
    this.announcementState.set(message);
    return { kind: 'conflict', message };
  }

  private bulkConflictMessage(): string {
    const snapshot = this.facade.snapshot();
    const names = Array.from(this.selection.bulkSet().keys()).map(
      (doorId) => snapshot?.byId.get(doorId)?.name ?? String(doorId),
    );
    return ABWAB_LABELS.bulkConflictMessage(names.join('، '));
  }

  private toFailureOutcome(err: unknown): AbwabWriteFailureOutcome {
    if (err instanceof HttpErrorResponse) {
      const body = err.error as ApiResponse<unknown> | null | undefined;
      const backendMessage = typeof body?.message === 'string' && body.message.length > 0 ? body.message : null;

      if (err.status === 409) {
        return { kind: 'conflict', message: backendMessage ?? ABWAB_LABELS.writeConflictFallback };
      }
      if (err.status === 400 || err.status === 404) {
        return { kind: 'invalid', message: backendMessage ?? ABWAB_LABELS.writeInvalidFallback };
      }
    }
    return { kind: 'error', message: ABWAB_LABELS.writeTransportFallback };
  }
}
