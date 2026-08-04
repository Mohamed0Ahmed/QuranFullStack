import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Observable, Subscription, catchError, map, of, shareReplay, tap } from 'rxjs';

import { AbwabApi } from '../data-access/abwab.api';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import { AbwabTreeSnapshotVm } from '../models/abwab.models';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { buildAbwabTreeSnapshot } from './abwab-tree.builder';

@Injectable({ providedIn: 'root' })
export class AbwabSnapshotFacade {
  private readonly api = inject(AbwabApi);

  private readonly rawTree = signal<AbwabTreeDto | null>(null);
  private readonly etagState = signal<string | null>(null);
  private readonly loadingState = signal(false);
  private readonly errorState = signal<string | null>(null);
  private pendingRequest: Subscription | null = null;
  private fetchGeneration = 0;

  readonly isLoading = this.loadingState.asReadonly();
  readonly errorMessage = this.errorState.asReadonly();

  readonly snapshotValidator = this.etagState.asReadonly();

  readonly snapshot = computed<AbwabTreeSnapshotVm | null>(() => {
    const dto = this.rawTree();
    return dto === null ? null : buildAbwabTreeSnapshot(dto);
  });

  readonly isEmpty = computed(() => (this.rawTree()?.doors.length ?? -1) === 0);

  load(): void {
    this.fetch();
  }

  refresh(): Observable<AbwabTreeSnapshotVm | null> {
    return this.fetch();
  }

  private fetch(): Observable<AbwabTreeSnapshotVm | null> {
    this.pendingRequest?.unsubscribe();
    const generation = ++this.fetchGeneration;
    const isSuperseded = () => generation !== this.fetchGeneration;
    this.loadingState.set(true);
    this.errorState.set(null);

    const request$ = this.api.getTree(this.etagState()).pipe(
      tap((response) => {
        if (isSuperseded()) {
          return;
        }
        this.loadingState.set(false);
        const envelope = response.body;
        if (envelope?.isSuccess && envelope.data) {
          this.rawTree.set(envelope.data);
          this.etagState.set(response.headers.get('ETag'));
          this.errorState.set(null);
        } else {
          this.errorState.set(envelope?.message ?? ABWAB_LABELS.loadErrorFallback);
        }
      }),
      map(() => this.snapshot()),
      catchError((error: unknown) => {
        if (isSuperseded()) {
          return of(this.snapshot());
        }
        this.loadingState.set(false);

        if (error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotModified) {
          return of(this.snapshot());
        }

        this.errorState.set(ABWAB_LABELS.loadErrorFallback);
        return of(this.snapshot());
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

    this.pendingRequest = request$.subscribe();
    return request$;
  }
}
