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
import { ReorderSectionBody } from '../../../core/api/generated/models/reorder-section-body';
import { CreateDoorCommand } from '../../../core/api/generated/models/create-door-command';
import { EditDoorBody } from '../../../core/api/generated/models/edit-door-body';
import { MoveDoorBody } from '../../../core/api/generated/models/move-door-body';
import { ReorderDoorBody } from '../../../core/api/generated/models/reorder-door-body';
import { AbwabBulkDoorRef } from '../../../core/api/generated/models/abwab-bulk-door-ref';
import { AbwabDoorDto } from '../../../core/api/generated/models/abwab-door-dto';
import { AbwabSectionDto } from '../../../core/api/generated/models/abwab-section-dto';
import { AbwabDoorRelationDto } from '../../../core/api/generated/models/abwab-door-relation-dto';
import { AddDoorRelationsBody } from '../../../core/api/generated/models/add-door-relations-body';

export type AbwabWriteFailureOutcome =
  | { readonly kind: 'conflict'; readonly message: string }
  | { readonly kind: 'invalid'; readonly message: string }
  | { readonly kind: 'error'; readonly message: string };

export type AbwabWriteOutcome<T> = { readonly kind: 'success'; readonly data: T } | AbwabWriteFailureOutcome;

/** The 409 policy, at module scope so the templates controller — which cannot reuse this
 * controller, having no snapshot to refresh and no version tokens to rebind — still shares one
 * status→outcome mapping instead of forking a second one that could drift. */
export function toAbwabWriteFailure(err: unknown): AbwabWriteFailureOutcome {
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

/** Counts a door plus every live descendant — the live tree only ever holds live nodes
 * (abwab-tree.builder.ts partitions archived ones out), so no extra `isArchived` guard is
 * needed past the root (plan-slice-b.md §6.4, the archive-count cell). */
function countLiveSubtree(node: AbwabNode): number {
  return 1 + node.children.reduce((sum, child) => sum + countLiveSubtree(child), 0);
}

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

  // No conflictClearsSelectionId, matching renameSection: a section write invalidates no
  // door selection.
  reorderSection(id: number, body: ReorderSectionBody): Observable<AbwabWriteOutcome<AbwabSectionDto>> {
    return this.dispatch(this.api.reorderSection(id, body));
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

  // sectionId is the restore destination and is omitted for the ordinary "back where it came from"
  // case; a root whose section was retired meanwhile has no such place and the backend 400s without
  // one, which is what the restore modal asks for.
  restoreDoor(id: number, options: { sectionId?: number; version: number }): Observable<AbwabWriteOutcome<AbwabDoorDto>> {
    return this.api.restoreDoor(id, options).pipe(
      map((response) => this.handleSuccess(response, () => {
        this.announcementState.set(ABWAB_LABELS.restoreAnnouncement);
      })),
      catchError((err: unknown) => of(this.handleFailure<AbwabDoorDto>(err, id))),
    );
  }

  bulkMoveDoors(
    targetParentId: number | null,
    targetSectionId: number | null,
  ): Observable<AbwabWriteOutcome<AbwabDoorDto[]>> {
    const doors = this.currentBulkRefs();
    return this.api.bulkMoveDoors({ doors, targetParentId, targetSectionId }).pipe(
      map((response) => this.handleSuccess(response)),
      catchError((err: unknown) => this.handleBulkFailure<AbwabDoorDto[]>(err, doors)),
    );
  }

  bulkArchiveDoors(): Observable<AbwabWriteOutcome<number[]>> {
    const doors = this.currentBulkRefs();
    return this.api.bulkArchiveDoors({ doors }).pipe(
      map((response) => this.handleSuccess(response)),
      catchError((err: unknown) => this.handleBulkFailure<number[]>(err, doors)),
    );
  }

  // Relation writes carry no version token — they move no door's xmin, so no stale-token 409 is
  // reachable — but they still go through `dispatch`, because they change relation counts on two
  // rows of the snapshot and the refresh-after-write invariant is what keeps those honest.
  addDoorRelations(doorId: number, body: AddDoorRelationsBody): Observable<AbwabWriteOutcome<AbwabDoorRelationDto[]>> {
    return this.dispatch(this.api.addDoorRelations(doorId, body));
  }

  deleteRelation(relationId: number): Observable<AbwabWriteOutcome<unknown>> {
    return this.dispatch(this.api.deleteRelation(relationId));
  }

  private currentBulkRefs(): AbwabBulkDoorRef[] {
    const byId = this.facade.snapshot()?.byId;
    return Array.from(this.selection.bulkSet(), ([doorId, version]) => ({ doorId, version })).filter(
      (ref) => {
        const node = byId?.get(ref.doorId);
        return byId === undefined || (node !== undefined && !node.isArchived);
      },
    );
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
      this.refreshAndRebind();
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

  private handleBulkFailure<T>(
    err: unknown,
    attempted: readonly AbwabBulkDoorRef[],
  ): Observable<AbwabWriteOutcome<T>> {
    const outcome = this.toFailureOutcome(err);
    if (outcome.kind === 'conflict') {
      // Bulk selection is preserved on conflict (M17) — see the class doc for why.
      const message = this.bulkConflictMessage();
      this.announcementState.set(message);
      return of({ kind: 'conflict', message });
    }
    if (outcome.kind !== 'invalid') {
      this.announcementState.set(outcome.message);
      return of(outcome);
    }
    // The backend's bulk 404 is «الباب غير موجود» and identifies no door, so refetch, rebind
    // (which drops whatever is now archived), and name the offenders from what came back.
    return this.facade.refresh().pipe(
      map((snapshot) => {
        if (snapshot) {
          this.selection.rebindTo(snapshot);
        }
        const message = this.bulkVanishedMessage(attempted, snapshot?.byId) ?? outcome.message;
        this.announcementState.set(message);
        return { kind: 'invalid', message };
      }),
    );
  }

  private bulkConflictMessage(): string {
    const snapshot = this.facade.snapshot();
    const names = Array.from(this.selection.bulkSet().keys()).map(
      (doorId) => snapshot?.byId.get(doorId)?.name ?? String(doorId),
    );
    return ABWAB_LABELS.bulkConflictMessage(names.join('، '));
  }

  private bulkVanishedMessage(
    attempted: readonly AbwabBulkDoorRef[],
    byId: ReadonlyMap<number, AbwabNode> | undefined,
  ): string | null {
    if (!byId) {
      return null;
    }
    const names: string[] = [];
    for (const ref of attempted) {
      const node = byId.get(ref.doorId);
      if (node === undefined || node.isArchived) {
        names.push(node?.name ?? String(ref.doorId));
      }
    }
    return names.length === 0 ? null : ABWAB_LABELS.bulkVanishedMessage(names.length, names.join('، '));
  }

  private refreshAndRebind(): void {
    this.facade.refresh().subscribe((snapshot) => {
      if (snapshot) {
        this.selection.rebindTo(snapshot);
      }
    });
  }

  private toFailureOutcome(err: unknown): AbwabWriteFailureOutcome {
    return toAbwabWriteFailure(err);
  }
}
