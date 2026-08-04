import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Observable, Subscription, catchError, map, of, shareReplay, tap } from 'rxjs';

import { AbwabTemplatesApi } from '../data-access/abwab-templates.api';
import { AbwabTemplateSummaryDto } from '../../../core/api/generated/models/abwab-template-summary-dto';
import { AbwabTemplateDto } from '../../../core/api/generated/models/abwab-template-dto';
import { AbwabTemplateVm, buildAbwabTemplateTree } from '../models/abwab-templates.models';
import { ABWAB_LABELS } from '../models/abwab.labels';

function isNotModified(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotModified;
}

@Injectable({ providedIn: 'root' })
export class AbwabTemplatesFacade {
  private readonly api = inject(AbwabTemplatesApi);

  private readonly rawList = signal<readonly AbwabTemplateSummaryDto[] | null>(null);
  private listEtagState: string | null = null;
  private readonly rawSelected = signal<AbwabTemplateDto | null>(null);
  private selectedEtagState: { id: number; etag: string } | null = null;
  private readonly listLoadingState = signal(false);
  private readonly listErrorState = signal<string | null>(null);
  private readonly selectedErrorState = signal<string | null>(null);
  private readonly selectedLoadingState = signal(false);
  private readonly selectedIdState = signal<number | null>(null);
  private listRequest: Subscription | null = null;
  private selectedRequest: Subscription | null = null;
  private listGeneration = 0;
  private selectedGeneration = 0;

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
    this.fetchList();
  }

  refreshList(): Observable<readonly AbwabTemplateSummaryDto[]> {
    return this.fetchList();
  }

  select(templateId: number): void {
    this.selectedIdState.set(templateId);
    this.fetchSelected(templateId);
  }

  clearSelection(): void {
    this.selectedRequest?.unsubscribe();
    this.selectedIdState.set(null);
    this.rawSelected.set(null);
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
    const generation = ++this.listGeneration;
    const isSuperseded = () => generation !== this.listGeneration;
    this.listLoadingState.set(true);
    this.listErrorState.set(null);

    const request$ = this.api.getTemplates(this.listEtagState).pipe(
      tap((response) => {
        if (isSuperseded()) {
          return;
        }
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
        if (isSuperseded()) {
          return of(this.templates());
        }
        this.listLoadingState.set(false);

        if (isNotModified(error)) {
          return of(this.templates());
        }

        this.listErrorState.set(ABWAB_LABELS.templatesLoadError);
        return of(this.templates());
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

    this.listRequest = request$.subscribe();
    return request$;
  }

  private fetchSelected(templateId: number): Observable<AbwabTemplateVm | null> {
    this.selectedRequest?.unsubscribe();
    const generation = ++this.selectedGeneration;
    const isSuperseded = () => generation !== this.selectedGeneration;
    this.selectedErrorState.set(null);
    this.selectedLoadingState.set(true);

    const heldEtag = this.selectedEtagState?.id === templateId ? this.selectedEtagState.etag : null;

    const request$ = this.api.getTemplate(templateId, heldEtag).pipe(
      tap((response) => {
        if (isSuperseded()) {
          return;
        }
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
        if (isSuperseded()) {
          return of(this.selectedTemplate());
        }
        this.selectedLoadingState.set(false);

        if (isNotModified(error)) {
          return of(this.selectedTemplate());
        }

        this.selectedErrorState.set(ABWAB_LABELS.templateLoadError);
        return of(this.selectedTemplate());
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

    this.selectedRequest = request$.subscribe();
    return request$;
  }
}
