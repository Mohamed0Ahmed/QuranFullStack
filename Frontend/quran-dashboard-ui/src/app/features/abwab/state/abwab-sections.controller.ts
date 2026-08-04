import { Injectable, computed, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AbwabWriteController, AbwabWriteOutcome } from './abwab-write.controller';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabTreeSectionDto } from '../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabSectionDto } from '../../../core/api/generated/models/abwab-section-dto';

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

  reorderSection(id: number, position: number, version: number): Observable<AbwabWriteOutcome<AbwabSectionDto>> {
    return this.writeController.reorderSection(id, { position, version });
  }
}
