import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';

import { AbwabApi } from '../data-access/abwab.api';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabWriteController, AbwabWriteOutcome } from './abwab-write.controller';
import { ABWAB_LABELS } from '../models/abwab.labels';
import {
  ABWAB_RELATION_DIRECTION_FROM_WIRE,
  ABWAB_RELATION_DIRECTION_TO_WIRE,
  ABWAB_RELATION_KIND_FROM_WIRE,
  ABWAB_RELATION_KIND_TO_WIRE,
  AbwabRelationDirectionKind,
  AbwabRelationKind,
  AbwabRelationVm,
} from '../models/abwab.models';
import { AbwabDoorRelationDto } from '../../../core/api/generated/models/abwab-door-relation-dto';

export type AbwabRelationsLoadResult =
  | { readonly kind: 'success'; readonly relations: readonly AbwabRelationVm[] }
  | { readonly kind: 'error'; readonly message: string };

/**
 * The relations-facing surface, built like `abwab-sections.controller.ts`: it owns only what is
 * relation-specific — the per-door fetch, its cache, and the wire↔domain mapping — and forwards
 * every write to `AbwabWriteController`, which keeps the 409 policy, the outcome→message mapping,
 * and the refresh-after-write invariant in one place for every aggregate.
 *
 * **The cache's identity is the snapshot's ETag validator, and clearing is all-or-nothing.** The
 * server's validator is `bootId + tree generation`, bumped by every write that can alter any
 * relation list — a door rename included, which no count and no per-door key would catch, since a
 * rename changes the partner's list text without changing its length. So when the validator moves,
 * every entry goes. A null validator means no snapshot identity is held at all and nothing may be
 * served from the map.
 */
@Injectable({ providedIn: 'root' })
export class AbwabRelationsController {
  private readonly api = inject(AbwabApi);
  private readonly writeController = inject(AbwabWriteController);
  private readonly snapshot = inject(AbwabSnapshotFacade);

  private readonly cache = new Map<number, readonly AbwabRelationVm[]>();
  private cacheValidator: string | null = null;

  loadFor(doorId: number): Observable<AbwabRelationsLoadResult> {
    const validator = this.adoptCurrentValidator();
    const cached = validator === null ? undefined : this.cache.get(doorId);
    return cached === undefined
      ? this.fetchFor(doorId, validator)
      : of<AbwabRelationsLoadResult>({ kind: 'success', relations: cached });
  }

  /** The post-write read. The write has already moved the tree, so this door's cached list is
   * precisely the answer that must not be reused — and the snapshot refetch that would evict it is
   * still in flight when the write's own callback runs (`abwab-write.controller.ts:238`,
   * fire-and-forget). Racing it would serve the pre-write list back to the user who just changed it. */
  refetchFor(doorId: number): Observable<AbwabRelationsLoadResult> {
    return this.fetchFor(doorId, this.adoptCurrentValidator());
  }

  private adoptCurrentValidator(): string | null {
    const current = this.snapshot.snapshotValidator();
    if (current !== this.cacheValidator) {
      this.cache.clear();
      this.cacheValidator = current;
    }
    return current;
  }

  private fetchFor(doorId: number, requestValidator: string | null): Observable<AbwabRelationsLoadResult> {
    return this.api.getDoorRelations(doorId).pipe(
      map((response): AbwabRelationsLoadResult => {
        if (!response.isSuccess) {
          return { kind: 'error', message: response.message ?? ABWAB_LABELS.relationsLoadError };
        }
        const relations = (response.data ?? []).map(toRelationVm);
        // Re-checked on arrival, not just on departure: a tree that moved while this request was
        // out makes the response itself possibly stale, so it is rendered but not kept.
        if (requestValidator !== null && this.adoptCurrentValidator() === requestValidator) {
          this.cache.set(doorId, relations);
        }
        return { kind: 'success', relations };
      }),
      catchError(() => of<AbwabRelationsLoadResult>({ kind: 'error', message: ABWAB_LABELS.relationsLoadError })),
    );
  }

  addRelations(
    doorId: number,
    kind: AbwabRelationKind,
    direction: AbwabRelationDirectionKind | null,
    targetDoorIds: readonly number[],
  ): Observable<AbwabWriteOutcome<AbwabDoorRelationDto[]>> {
    return this.writeController.addDoorRelations(doorId, {
      type: ABWAB_RELATION_KIND_TO_WIRE[kind],
      // Null for the two mutual types, and the backend refuses a direction sent with one — the
      // asymmetry is the contract's, not a defensive default.
      direction: direction === null ? null : ABWAB_RELATION_DIRECTION_TO_WIRE[direction],
      targetDoorIds: [...targetDoorIds],
    });
  }

  deleteRelation(relationId: number): Observable<AbwabWriteOutcome<unknown>> {
    return this.writeController.deleteRelation(relationId);
  }
}

function toRelationVm(dto: AbwabDoorRelationDto): AbwabRelationVm {
  return {
    id: dto.id,
    otherDoorId: dto.otherDoorId,
    otherDoorName: dto.otherDoorName,
    kind: ABWAB_RELATION_KIND_FROM_WIRE[dto.type],
    direction: dto.direction === null ? null : ABWAB_RELATION_DIRECTION_FROM_WIRE[dto.direction],
  };
}
