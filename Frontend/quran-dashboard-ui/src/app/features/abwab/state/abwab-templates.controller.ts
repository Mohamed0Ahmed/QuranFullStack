import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';

import { AbwabTemplatesApi } from '../data-access/abwab-templates.api';
import { AbwabTemplatesFacade } from './abwab-templates.facade';
import { AbwabWriteOutcome, toAbwabWriteFailure } from './abwab-write.controller';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabAuthoringFields } from '../models/abwab-templates.models';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AbwabTemplateDto } from '../../../core/api/generated/models/abwab-template-dto';
import { AbwabTemplateNodeDto } from '../../../core/api/generated/models/abwab-template-node-dto';
import { AbwabDoorDto } from '../../../core/api/generated/models/abwab-door-dto';

type AbwabTemplatesRefresh = 'list' | 'selected' | 'both' | 'none';

interface AbwabTemplatesWriteOptions {
  readonly refresh: AbwabTemplatesRefresh;
  readonly successAnnouncement?: string;
  readonly announceFailure?: boolean;
}

@Injectable({ providedIn: 'root' })
export class AbwabTemplatesController {
  private readonly api = inject(AbwabTemplatesApi);
  private readonly facade = inject(AbwabTemplatesFacade);

  private readonly announcementState = signal<string | null>(null);
  readonly announcement = this.announcementState.asReadonly();

  createTemplate(name: string): Observable<AbwabWriteOutcome<AbwabTemplateDto | null>> {
    return this.dispatch(this.api.createTemplate({ name, description: null, representativeAyahText: null, aliases: [] }), {
      refresh: 'list',
      successAnnouncement: ABWAB_LABELS.templateCreatedAnnouncement,
      announceFailure: true,
    });
  }

  deleteTemplate(templateId: number): Observable<AbwabWriteOutcome<unknown>> {
    return this.dispatch(this.api.deleteTemplate(templateId), {
      refresh: 'list',
      successAnnouncement: ABWAB_LABELS.templateDeletedAnnouncement,
      announceFailure: true,
    });
  }

  addNode(
    templateId: number,
    parentNodeId: number | null,
    fields: AbwabAuthoringFields,
  ): Observable<AbwabWriteOutcome<AbwabTemplateNodeDto | null>> {
    return this.dispatch(this.api.addNode(templateId, { parentNodeId, ...toWireFields(fields) }), {
      refresh: 'both',
      announceFailure: true,
    });
  }

  editNode(nodeId: number, fields: AbwabAuthoringFields): Observable<AbwabWriteOutcome<AbwabTemplateNodeDto | null>> {
    return this.dispatch(this.api.editNode(nodeId, toWireFields(fields)), { refresh: 'both', announceFailure: true });
  }

  reorderNode(nodeId: number, position: number): Observable<AbwabWriteOutcome<AbwabTemplateNodeDto | null>> {
    return this.dispatch(this.api.reorderNode(nodeId, { position }), { refresh: 'selected', announceFailure: true });
  }

  deleteNode(nodeId: number): Observable<AbwabWriteOutcome<unknown>> {
    return this.dispatch(this.api.deleteNode(nodeId), { refresh: 'both', announceFailure: true });
  }

  applyTemplate(
    templateId: number,
    targetDoorIds: readonly number[],
  ): Observable<AbwabWriteOutcome<AbwabDoorDto[] | null>> {
    return this.dispatch(this.api.applyTemplate(templateId, { targetDoorIds: [...targetDoorIds] }), {
      refresh: 'none',
      successAnnouncement: ABWAB_LABELS.templateAppliedAnnouncement(targetDoorIds.length),
    });
  }

  private dispatch<T>(
    request$: Observable<ApiResponse<T> | null>,
    options: AbwabTemplatesWriteOptions,
  ): Observable<AbwabWriteOutcome<T | null>> {
    return request$.pipe(
      map((response) => this.handleSuccess(response, options)),
      catchError((err: unknown) => of(this.handleFailure<T | null>(err, options))),
    );
  }

  private handleSuccess<T>(
    response: ApiResponse<T> | null,
    options: AbwabTemplatesWriteOptions,
  ): AbwabWriteOutcome<T | null> {
    const data = response?.data ?? null;
    if (response === null || response.isSuccess) {
      this.announcementState.set(options.successAnnouncement ?? null);
      this.applyRefresh(options.refresh);
      return { kind: 'success', data };
    }
    const message = response.message ?? ABWAB_LABELS.writeInvalidFallback;
    this.announcementState.set(options.announceFailure ? message : null);
    return { kind: 'invalid', message };
  }

  private handleFailure<T>(err: unknown, options: AbwabTemplatesWriteOptions): AbwabWriteOutcome<T> {
    const outcome = toAbwabWriteFailure(err);
    this.announcementState.set(options.announceFailure ? outcome.message : null);
    return outcome;
  }

  private applyRefresh(refresh: AbwabTemplatesRefresh): void {
    if (refresh === 'list' || refresh === 'both') {
      this.facade.refreshList().subscribe();
    }
    if (refresh === 'selected' || refresh === 'both') {
      this.facade.refreshSelected().subscribe();
    }
  }
}

function toWireFields(fields: AbwabAuthoringFields): {
  name: string;
  description: string | null;
  representativeAyahText: string | null;
  aliases: string[];
} {
  return {
    name: fields.name.trim(),
    description: fields.description.trim() || null,
    representativeAyahText: fields.representativeAyahText.trim() || null,
    aliases: [...fields.aliases],
  };
}
