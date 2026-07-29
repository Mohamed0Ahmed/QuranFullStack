import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';

import { AbwabApi } from '../data-access/abwab.api';
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
 * relation-specific — the per-door fetch and the wire↔domain mapping — and forwards every write to
 * `AbwabWriteController`, which keeps the 409 policy, the outcome→message mapping, and the
 * refresh-after-write invariant in one place for every aggregate.
 */
@Injectable({ providedIn: 'root' })
export class AbwabRelationsController {
  private readonly api = inject(AbwabApi);
  private readonly writeController = inject(AbwabWriteController);

  loadFor(doorId: number): Observable<AbwabRelationsLoadResult> {
    return this.api.getDoorRelations(doorId).pipe(
      map((response): AbwabRelationsLoadResult =>
        response.isSuccess
          ? { kind: 'success', relations: (response.data ?? []).map(toRelationVm) }
          : { kind: 'error', message: response.message ?? ABWAB_LABELS.relationsLoadError },
      ),
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
