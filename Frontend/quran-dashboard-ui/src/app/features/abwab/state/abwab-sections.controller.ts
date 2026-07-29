import { Injectable, computed, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AbwabWriteController, AbwabWriteOutcome } from './abwab-write.controller';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabTreeSectionDto } from '../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabSectionDto } from '../../../core/api/generated/models/abwab-section-dto';

/**
 * The section-facing write surface (plan-slice-b.md T510, §5's "one controller per
 * aggregate" split). It does not duplicate `AbwabWriteController`'s dispatch/outcome
 * mapping/409 policy — that stays shared, one policy for both aggregates — it only
 * owns what is section-specific: reading the current section list, and the
 * id/name/version shape the sections modal needs. `sections()` reads the facade's
 * snapshot live on every call, never a value cached at modal-open time, so a rename
 * always carries the section's *current* `version` rather than a stale one that would
 * 409 the very next write.
 */
@Injectable({ providedIn: 'root' })
export class AbwabSectionsController {
  private readonly writeController = inject(AbwabWriteController);
  private readonly facade = inject(AbwabSnapshotFacade);

  readonly sections = computed<readonly AbwabTreeSectionDto[]>(() => this.facade.snapshot()?.sections ?? []);

  createSection(name: string): Observable<AbwabWriteOutcome<AbwabSectionDto>> {
    return this.writeController.createSection({ name });
  }

  renameSection(id: number, name: string, version: number): Observable<AbwabWriteOutcome<AbwabSectionDto>> {
    return this.writeController.renameSection(id, { name, version });
  }

  deleteSection(id: number): Observable<AbwabWriteOutcome<unknown>> {
    return this.writeController.deleteSection(id);
  }
}
