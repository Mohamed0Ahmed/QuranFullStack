import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Observable, Subscription, catchError, map, of, shareReplay, tap } from 'rxjs';

import { AbwabTemplatesApi } from '../data-access/abwab-templates.api';
import { AbwabTemplateSummaryDto } from '../../../core/api/generated/models/abwab-template-summary-dto';
import { AbwabTemplateDto } from '../../../core/api/generated/models/abwab-template-dto';
import { AbwabTemplateVm, buildAbwabTemplateTree } from '../models/abwab-templates.models';
import { ABWAB_LABELS } from '../models/abwab.labels';

// HttpClient routes a 304 to the error channel because only 2xx counts as ok. It is not a failure:
// the held value and its validator are current, so both stay and no error is set.
function isNotModified(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotModified;
}

@Injectable({ providedIn: 'root' })
export class AbwabTemplatesFacade {
  private readonly api = inject(AbwabTemplatesApi);

  private readonly rawList = signal<readonly AbwabTemplateSummaryDto[] | null>(null);
  private listEtagState: string | null = null;
  private readonly rawSelected = signal<AbwabTemplateDto | null>(null);
  // Id-keyed so a validator can never travel across templates: it is sent only for the id it was
  // issued for, and selecting a different template simply finds no validator to send.
  private selectedEtagState: { id: number; etag: string } | null = null;
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
  readonly selectedLoading = this.selectedLoadingState.asReadonly();
  readonly selectedTemplateId = this.selectedIdState.asReadonly();

  readonly templates = computed<readonly AbwabTemplateSummaryDto[]>(() => this.rawList() ?? []);
  readonly isEmpty = computed(() => (this.rawList()?.length ?? -1) === 0);

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
    // Dropped with the value it validates: keeping it would let a re-select of the same template be
    // answered 304 with nothing left to render against.
    this.selectedEtagState = null;
    this.selectedErrorState.set(null);
    this.selectedLoadingState.set(false);
  }

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

    const request$ = this.api.getTemplates(this.listEtagState).pipe(
      tap((response) => {
        this.listLoadingState.set(false);
        const envelope = response.body;
        if (envelope?.isSuccess && envelope.data) {
          this.rawList.set(envelope.data);
          this.listEtagState = response.headers.get('ETag');
          this.listErrorState.set(null);
        } else {
          this.listErrorState.set(envelope?.message ?? ABWAB_LABELS.templatesLoadError);
        }
      }),
      map(() => this.templates()),
      catchError((error: unknown) => {
        this.listLoadingState.set(false);

        // 304: the held list and its validator are current. Keep both, report nothing.
        if (isNotModified(error)) {
          return of(this.templates());
        }

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

    const heldEtag = this.selectedEtagState?.id === templateId ? this.selectedEtagState.etag : null;

    const request$ = this.api.getTemplate(templateId, heldEtag).pipe(
      tap((response) => {
        this.selectedLoadingState.set(false);
        const envelope = response.body;
        if (envelope?.isSuccess && envelope.data) {
          this.rawSelected.set(envelope.data);
          const etag = response.headers.get('ETag');
          this.selectedEtagState = etag ? { id: templateId, etag } : null;
          this.selectedErrorState.set(null);
        } else {
          this.selectedErrorState.set(envelope?.message ?? ABWAB_LABELS.templateLoadError);
        }
      }),
      map(() => this.selectedTemplate()),
      catchError((error: unknown) => {
        this.selectedLoadingState.set(false);

        if (isNotModified(error)) {
          return of(this.selectedTemplate());
        }

        this.selectedErrorState.set(ABWAB_LABELS.templateLoadError);
        return of(this.selectedTemplate());
      }),
      shareReplay(1),
    );

    this.selectedRequest = request$.subscribe();
    return request$;
  }
}
