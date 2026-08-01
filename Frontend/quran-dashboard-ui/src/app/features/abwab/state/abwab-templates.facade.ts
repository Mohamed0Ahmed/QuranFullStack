import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, Subscription, catchError, map, of, shareReplay, tap } from 'rxjs';

import { AbwabTemplatesApi } from '../data-access/abwab-templates.api';
import { AbwabTemplateSummaryDto } from '../../../core/api/generated/models/abwab-template-summary-dto';
import { AbwabTemplateDto } from '../../../core/api/generated/models/abwab-template-dto';
import { AbwabTemplateVm, buildAbwabTemplateTree } from '../models/abwab-templates.models';
import { ABWAB_LABELS } from '../models/abwab.labels';

/**
 * Owns the template list and the selected template's tree, on the `AbwabSnapshotFacade`
 * contract: `refresh()` always issues a new request rather than replaying a cached one, and on
 * failure the previous list/tree is left in place instead of blanking the page.
 *
 * Root-scoped like the doors snapshot — it is a cache, not overlay state
 * (`features/abwab/README.md`, "Overlay state is page-scoped; caches are app-scoped").
 */
@Injectable({ providedIn: 'root' })
export class AbwabTemplatesFacade {
  private readonly api = inject(AbwabTemplatesApi);

  private readonly rawList = signal<readonly AbwabTemplateSummaryDto[] | null>(null);
  private readonly rawSelected = signal<AbwabTemplateDto | null>(null);
  private readonly listLoadingState = signal(false);
  private readonly listErrorState = signal<string | null>(null);
  private readonly selectedErrorState = signal<string | null>(null);
  private readonly selectedLoadingState = signal(false);
  private readonly selectedIdState = signal<number | null>(null);
  private listRequest: Subscription | null = null;
  private selectedRequest: Subscription | null = null;

  readonly isLoading = this.listLoadingState.asReadonly();
  readonly errorMessage = this.listErrorState.asReadonly();
  readonly selectedErrorMessage = this.selectedErrorState.asReadonly();
  /** True only while a per-template fetch is in flight. `selectedTemplate` is null for that whole
   * window — `select()` writes the id before the request resolves — so without this the page
   * cannot tell "nothing chosen" from "still loading" and says «اختر قالبًا» to a user who just
   * chose one. */
  readonly selectedLoading = this.selectedLoadingState.asReadonly();
  readonly selectedTemplateId = this.selectedIdState.asReadonly();

  /** Each row's `nodeCount` counts the root's live descendants, excluding the root itself — the
   * contract's «N عناصر» chip. */
  readonly templates = computed<readonly AbwabTemplateSummaryDto[]>(() => this.rawList() ?? []);
  readonly isEmpty = computed(() => (this.rawList()?.length ?? -1) === 0);

  /**
   * Null unless the loaded template *is* the selected one. Leaving a previous value in place
   * across a failed load is the `AbwabSnapshotFacade` contract, but that facade owns a single
   * resource; this one changes resource on every `select()`, so the same rule would let the page
   * name one template while every write sent another id — and template copies are detached by
   * design, so there is no provenance to undo a wrong copy by.
   *
   * A refresh of the *same* template does not trip this, so writes never blink the editor.
   */
  readonly selectedTemplate = computed<AbwabTemplateVm | null>(() => {
    const dto = this.rawSelected();
    if (dto === null || dto.id !== this.selectedIdState()) {
      return null;
    }
    return buildAbwabTemplateTree(dto);
  });

  loadList(): void {
    this.fetchList().subscribe();
  }

  refreshList(): Observable<readonly AbwabTemplateSummaryDto[]> {
    return this.fetchList();
  }

  select(templateId: number): void {
    this.selectedIdState.set(templateId);
    this.fetchSelected(templateId).subscribe();
  }

  clearSelection(): void {
    this.selectedRequest?.unsubscribe();
    this.selectedIdState.set(null);
    this.rawSelected.set(null);
    this.selectedErrorState.set(null);
    this.selectedLoadingState.set(false);
  }

  /** Re-reads whatever is selected. Every write in the workshop changes the selected template's
   * own tree, and most change the list's «N عناصر» chip or its name too, so both are refetched. */
  refreshSelected(): Observable<AbwabTemplateVm | null> {
    const templateId = this.selectedIdState();
    if (templateId === null) {
      return of(null);
    }
    return this.fetchSelected(templateId);
  }

  private fetchList(): Observable<readonly AbwabTemplateSummaryDto[]> {
    this.listRequest?.unsubscribe();
    this.listLoadingState.set(true);
    this.listErrorState.set(null);

    const request$ = this.api.getTemplates().pipe(
      tap((response) => {
        this.listLoadingState.set(false);
        if (response.isSuccess && response.data) {
          this.rawList.set(response.data);
          this.listErrorState.set(null);
        } else {
          this.listErrorState.set(response.message ?? ABWAB_LABELS.templatesLoadError);
        }
      }),
      map(() => this.templates()),
      catchError(() => {
        this.listLoadingState.set(false);
        this.listErrorState.set(ABWAB_LABELS.templatesLoadError);
        return of(this.templates());
      }),
      shareReplay(1),
    );

    this.listRequest = request$.subscribe();
    return request$;
  }

  private fetchSelected(templateId: number): Observable<AbwabTemplateVm | null> {
    this.selectedRequest?.unsubscribe();
    this.selectedErrorState.set(null);
    this.selectedLoadingState.set(true);

    const request$ = this.api.getTemplate(templateId).pipe(
      tap((response) => {
        this.selectedLoadingState.set(false);
        if (response.isSuccess && response.data) {
          this.rawSelected.set(response.data);
          this.selectedErrorState.set(null);
        } else {
          this.selectedErrorState.set(response.message ?? ABWAB_LABELS.templateLoadError);
        }
      }),
      map(() => this.selectedTemplate()),
      catchError(() => {
        this.selectedLoadingState.set(false);
        this.selectedErrorState.set(ABWAB_LABELS.templateLoadError);
        return of(this.selectedTemplate());
      }),
      shareReplay(1),
    );

    this.selectedRequest = request$.subscribe();
    return request$;
  }
}
