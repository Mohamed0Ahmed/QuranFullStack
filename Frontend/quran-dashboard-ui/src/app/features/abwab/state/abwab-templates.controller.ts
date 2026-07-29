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

/** What the workshop refetches after a write. Every node write moves the selected template's tree,
 * and most move the list's name or «N عناصر» chip too, so `both` is the ordinary case. */
type AbwabTemplatesRefresh = 'list' | 'selected' | 'both' | 'none';

/**
 * The templates-facing write surface. It deliberately does **not** reuse
 * `AbwabWriteController`: that controller's core invariant is refresh-the-doors-snapshot-and-
 * rebind-every-version-token, and templates carry no version tokens and are not in that
 * snapshot. What the two must not fork is the 409 policy, so both call
 * `toAbwabWriteFailure` — one status→outcome mapping, two refresh targets.
 *
 * Apply refreshes nothing: it writes doors, and `AbwabPageComponent.ngOnInit` calls
 * `facade.load()` on every entry, so returning to `/abwab` is what makes the copies visible.
 * Refreshing the doors snapshot from here would buy a fetch nobody sees.
 */
@Injectable({ providedIn: 'root' })
export class AbwabTemplatesController {
  private readonly api = inject(AbwabTemplatesApi);
  private readonly facade = inject(AbwabTemplatesFacade);

  private readonly announcementState = signal<string | null>(null);
  readonly announcement = this.announcementState.asReadonly();

  createTemplate(name: string): Observable<AbwabWriteOutcome<AbwabTemplateDto>> {
    // The other three authoring fields are `null` at birth: creation is a name-only prompt, and
    // the root is then edited through the full authoring modal like every other node (plan T704).
    return this.dispatch(
      this.api.createTemplate({ name, description: null, representativeAyahText: null, aliases: [] }),
      'list',
      ABWAB_LABELS.templateCreatedAnnouncement,
    );
  }

  deleteTemplate(templateId: number): Observable<AbwabWriteOutcome<unknown>> {
    return this.dispatch(this.api.deleteTemplate(templateId), 'list', ABWAB_LABELS.templateDeletedAnnouncement);
  }

  addNode(
    templateId: number,
    parentNodeId: number | null,
    fields: AbwabAuthoringFields,
  ): Observable<AbwabWriteOutcome<AbwabTemplateNodeDto>> {
    return this.dispatch(this.api.addNode(templateId, { parentNodeId, ...toWireFields(fields) }), 'both');
  }

  editNode(nodeId: number, fields: AbwabAuthoringFields): Observable<AbwabWriteOutcome<AbwabTemplateNodeDto>> {
    return this.dispatch(this.api.editNode(nodeId, toWireFields(fields)), 'both');
  }

  reorderNode(nodeId: number, position: number): Observable<AbwabWriteOutcome<AbwabTemplateNodeDto>> {
    return this.dispatch(this.api.reorderNode(nodeId, { position }), 'selected');
  }

  deleteNode(nodeId: number): Observable<AbwabWriteOutcome<unknown>> {
    return this.dispatch(this.api.deleteNode(nodeId), 'both');
  }

  applyTemplate(templateId: number, targetDoorIds: readonly number[]): Observable<AbwabWriteOutcome<AbwabDoorDto[]>> {
    return this.dispatch(
      this.api.applyTemplate(templateId, { targetDoorIds: [...targetDoorIds] }),
      'none',
      ABWAB_LABELS.templateAppliedAnnouncement(targetDoorIds.length),
    );
  }

  private dispatch<T>(
    request$: Observable<ApiResponse<T> | null>,
    refresh: AbwabTemplatesRefresh,
    successMessage?: string,
  ): Observable<AbwabWriteOutcome<T>> {
    return request$.pipe(
      map((response) => this.handleSuccess(response, refresh, successMessage)),
      catchError((err: unknown) => of(this.handleFailure<T>(err))),
    );
  }

  private handleSuccess<T>(
    response: ApiResponse<T> | null,
    refresh: AbwabTemplatesRefresh,
    successMessage?: string,
  ): AbwabWriteOutcome<T> {
    // Both delete routes answer `204 No Content`, and HttpClient parses an empty body as `null`.
    // Only a success is ever a 204 — every failure arrives as a 4xx through `catchError` — so a
    // null response is a payload-less success. Dereferencing `isSuccess` first throws, gets
    // swallowed as a transport error, and reports failure on a committed write.
    const data = (response?.data ?? null) as T;
    if (response === null || response.isSuccess) {
      this.announcementState.set(successMessage ?? null);
      this.applyRefresh(refresh);
      return { kind: 'success', data };
    }
    const message = response.message ?? ABWAB_LABELS.writeInvalidFallback;
    this.announcementState.set(message);
    return { kind: 'invalid', message };
  }

  private handleFailure<T>(err: unknown): AbwabWriteOutcome<T> {
    const outcome = toAbwabWriteFailure(err);
    this.announcementState.set(outcome.message);
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

/** Aliases are sent as a real array, never `null`: the backend normalizes either, but an empty
 * list is what "no aliases" means and it keeps the create/edit bodies one shape. */
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
