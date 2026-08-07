import { Injectable, Injector, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, from, map, of, switchMap } from 'rxjs';

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
import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { WriteAuthFailureCoordinator } from '../../../core/auth/write-auth-failure.coordinator';
import { PermissionCode } from '../../../core/auth/permission-code';
import { ABWAB_WRITE_PERMISSIONS } from './abwab-permissions.controller';

export type AbwabWriteFailureOutcome =
  | { readonly kind: 'conflict'; readonly message: string }
  | { readonly kind: 'invalid'; readonly message: string }
  | { readonly kind: 'unauthorized'; readonly message: string }
  | { readonly kind: 'forbidden'; readonly message: string }
  | { readonly kind: 'error'; readonly message: string };

export type AbwabWriteOutcome<T> = { readonly kind: 'success'; readonly data: T | null } | AbwabWriteFailureOutcome;

interface AbwabWriteOptions {
  readonly successAnnouncement?: string;
  readonly conflictClearsSelectionId?: number;
  readonly announceFailure?: boolean;
}

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

export function abwabPermissionDenied(): AbwabWriteFailureOutcome {
  return { kind: 'forbidden', message: ABWAB_LABELS.writePermissionDenied };
}

function countLiveSubtree(node: AbwabNode): number {
  return 1 + node.children.reduce((sum, child) => sum + countLiveSubtree(child), 0);
}

@Injectable({ providedIn: 'root' })
export class AbwabWriteController {
  private readonly api = inject(AbwabApi);
  private readonly facade = inject(AbwabSnapshotFacade);
  private readonly selection = inject(AbwabSelectionStore);
  private readonly currentUserStore = inject(CurrentUserStore);
  private readonly injector = inject(Injector);

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
    return this.dispatch(ABWAB_WRITE_PERMISSIONS.createSection, () => this.api.createSection(command), {
      successAnnouncement: ABWAB_LABELS.sectionCreatedAnnouncement,
    });
  }

  renameSection(id: number, body: RenameSectionBody): Observable<AbwabWriteOutcome<AbwabSectionDto>> {
    return this.dispatch(ABWAB_WRITE_PERMISSIONS.editSection, () => this.api.renameSection(id, body), {
      successAnnouncement: ABWAB_LABELS.sectionRenamedAnnouncement,
    });
  }

  deleteSection(id: number): Observable<AbwabWriteOutcome<unknown>> {
    return this.dispatch(ABWAB_WRITE_PERMISSIONS.deleteSection, () => this.api.deleteSection(id), {
      successAnnouncement: ABWAB_LABELS.sectionDeletedAnnouncement,
      announceFailure: true,
    });
  }

  reorderSection(id: number, body: ReorderSectionBody): Observable<AbwabWriteOutcome<AbwabSectionDto>> {
    return this.dispatch(ABWAB_WRITE_PERMISSIONS.reorderSection, () => this.api.reorderSection(id, body), {
      successAnnouncement: ABWAB_LABELS.sectionReorderedAnnouncement,
    });
  }

  createDoor(command: CreateDoorCommand): Observable<AbwabWriteOutcome<AbwabDoorDto>> {
    return this.dispatch(ABWAB_WRITE_PERMISSIONS.createDoor, () => this.api.createDoor(command), {
      successAnnouncement: ABWAB_LABELS.doorCreatedAnnouncement,
    });
  }

  updateDoor(id: number, body: EditDoorBody): Observable<AbwabWriteOutcome<AbwabDoorDto>> {
    return this.dispatch(ABWAB_WRITE_PERMISSIONS.editDoor, () => this.api.updateDoor(id, body), {
      successAnnouncement: ABWAB_LABELS.doorUpdatedAnnouncement,
      conflictClearsSelectionId: id,
    });
  }

  moveDoor(id: number, body: MoveDoorBody): Observable<AbwabWriteOutcome<AbwabDoorDto>> {
    return this.dispatch(ABWAB_WRITE_PERMISSIONS.moveDoor, () => this.api.moveDoor(id, body), {
      successAnnouncement: ABWAB_LABELS.doorMovedAnnouncement,
      conflictClearsSelectionId: id,
      announceFailure: true,
    });
  }

  reorderDoor(id: number, body: ReorderDoorBody): Observable<AbwabWriteOutcome<AbwabDoorDto>> {
    return this.dispatch(ABWAB_WRITE_PERMISSIONS.reorderDoor, () => this.api.reorderDoor(id, body), {
      successAnnouncement: ABWAB_LABELS.doorReorderedAnnouncement,
      conflictClearsSelectionId: id,
      announceFailure: true,
    });
  }

  archiveDoor(id: number, version: number): Observable<AbwabWriteOutcome<unknown>> {
    return this.dispatch(ABWAB_WRITE_PERMISSIONS.archiveDoor, () => this.api.archiveDoor(id, { version }), {
      successAnnouncement: ABWAB_LABELS.doorArchivedAnnouncement,
      conflictClearsSelectionId: id,
      announceFailure: true,
    });
  }

  restoreDoor(id: number, options: { sectionId?: number; version: number }): Observable<AbwabWriteOutcome<AbwabDoorDto>> {
    return this.dispatch(ABWAB_WRITE_PERMISSIONS.restoreDoor, () => this.api.restoreDoor(id, options), {
      successAnnouncement: ABWAB_LABELS.restoreAnnouncement,
      conflictClearsSelectionId: id,
      announceFailure: true,
    });
  }

  bulkMoveDoors(
    targetParentId: number | null,
    targetSectionId: number | null,
  ): Observable<AbwabWriteOutcome<AbwabDoorDto[]>> {
    if (!this.currentUserStore.can(ABWAB_WRITE_PERMISSIONS.moveDoor)) {
      return of(this.handlePermissionDenied<AbwabDoorDto[]>({ announceFailure: true }));
    }
    const doors = this.currentBulkRefs();
    const options: AbwabWriteOptions = {
      successAnnouncement: ABWAB_LABELS.bulkMoveAnnouncement(doors.length),
      announceFailure: true,
    };
    return this.api.bulkMoveDoors({ doors, targetParentId, targetSectionId }).pipe(
      map((response) => this.handleSuccess(response, options)),
      catchError((err: unknown) => this.handleBulkFailure<AbwabDoorDto[]>(err, doors, options)),
    );
  }

  bulkArchiveDoors(): Observable<AbwabWriteOutcome<number[]>> {
    if (!this.currentUserStore.can(ABWAB_WRITE_PERMISSIONS.archiveDoor)) {
      return of(this.handlePermissionDenied<number[]>({ announceFailure: true }));
    }
    const doors = this.currentBulkRefs();
    const options: AbwabWriteOptions = {
      successAnnouncement: ABWAB_LABELS.bulkArchiveAnnouncement(doors.length),
      announceFailure: true,
    };
    return this.api.bulkArchiveDoors({ doors }).pipe(
      map((response) => this.handleSuccess(response, options)),
      catchError((err: unknown) => this.handleBulkFailure<number[]>(err, doors, options)),
    );
  }

  addDoorRelations(doorId: number, body: AddDoorRelationsBody): Observable<AbwabWriteOutcome<AbwabDoorRelationDto[]>> {
    return this.dispatch(ABWAB_WRITE_PERMISSIONS.createRelation, () => this.api.addDoorRelations(doorId, body), {
      successAnnouncement: ABWAB_LABELS.relationsAddedAnnouncement(body.targetDoorIds.length),
    });
  }

  deleteRelation(relationId: number): Observable<AbwabWriteOutcome<unknown>> {
    return this.dispatch(ABWAB_WRITE_PERMISSIONS.deleteRelation, () => this.api.deleteRelation(relationId), {
      successAnnouncement: ABWAB_LABELS.relationDeletedAnnouncement,
      announceFailure: true,
    });
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

  private dispatch<T>(
    permission: PermissionCode,
    request: () => Observable<ApiResponse<T> | null>,
    options: AbwabWriteOptions = {},
  ): Observable<AbwabWriteOutcome<T>> {
    if (!this.currentUserStore.can(permission)) {
      return of(this.handlePermissionDenied<T>(options));
    }

    return request().pipe(
      map((response) => this.handleSuccess(response, options)),
      catchError((err: unknown) => this.toFailureOutcome(err).pipe(map((outcome) => this.handleFailure<T>(outcome, options)))),
    );
  }

  private handleSuccess<T>(response: ApiResponse<T> | null, options: AbwabWriteOptions = {}): AbwabWriteOutcome<T> {
    if (response === null || response.isSuccess) {
      this.announcementState.set(options.successAnnouncement ?? null);
      this.refreshAndRebind();
      return { kind: 'success', data: response?.data ?? null };
    }
    const message = response.message ?? ABWAB_LABELS.writeInvalidFallback;
    this.announcementState.set(options.announceFailure ? message : null);
    return { kind: 'invalid', message };
  }

  private handleFailure<T>(outcome: AbwabWriteFailureOutcome, options: AbwabWriteOptions): AbwabWriteOutcome<T> {
    if (outcome.kind === 'conflict' && options.conflictClearsSelectionId !== undefined) {
      if (this.selection.selectedDoorId() === options.conflictClearsSelectionId) {
        this.selection.clearSelection();
      }
    }
    this.announcementState.set(options.announceFailure ? outcome.message : null);
    return outcome;
  }

  private handleBulkFailure<T>(
    err: unknown,
    attempted: readonly AbwabBulkDoorRef[],
    options: AbwabWriteOptions = {},
  ): Observable<AbwabWriteOutcome<T>> {
    return this.toFailureOutcome(err).pipe(
      switchMap((outcome) => {
        if (outcome.kind === 'conflict') {
          const message = this.bulkConflictMessage(attempted);
          this.announcementState.set(options.announceFailure ? message : null);
          return of<AbwabWriteOutcome<T>>({ kind: 'conflict', message });
        }
        if (outcome.kind !== 'invalid') {
          this.announcementState.set(options.announceFailure ? outcome.message : null);
          return of<AbwabWriteOutcome<T>>(outcome);
        }
        return this.facade.refresh().pipe(
          map((snapshot): AbwabWriteOutcome<T> => {
            if (snapshot) {
              this.selection.rebindTo(snapshot);
            }
            const message = this.bulkVanishedMessage(attempted, snapshot?.byId) ?? outcome.message;
            this.announcementState.set(options.announceFailure ? message : null);
            return { kind: 'invalid' as const, message };
          }),
        );
      }),
    );
  }

  private bulkConflictMessage(attempted: readonly AbwabBulkDoorRef[]): string {
    const snapshot = this.facade.snapshot();
    const names = attempted.map((ref) => snapshot?.byId.get(ref.doorId)?.name ?? String(ref.doorId));
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

  private handlePermissionDenied<T>(options: AbwabWriteOptions): AbwabWriteOutcome<T> {
    const outcome = abwabPermissionDenied();
    this.announcementState.set(options.announceFailure ? outcome.message : null);
    return outcome;
  }

  private toFailureOutcome(err: unknown): Observable<AbwabWriteFailureOutcome> {
    if (!(err instanceof HttpErrorResponse) || (err.status !== 401 && err.status !== 403)) {
      return of(toAbwabWriteFailure(err));
    }
    return from(this.injector.get(WriteAuthFailureCoordinator).handle(err)).pipe(
      map((authFailure) =>
        authFailure === null
          ? toAbwabWriteFailure(err)
          : {
              kind: authFailure.kind,
              message: authFailure.message ?? ABWAB_LABELS.writePermissionDenied,
            },
      ),
    );
  }
}
