import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, Subscription, catchError, map, of, shareReplay, tap } from 'rxjs';

import { AbwabApi } from '../data-access/abwab.api';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import { AbwabTreeSnapshotVm } from '../models/abwab.models';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { buildAbwabTreeSnapshot } from './abwab-tree.builder';

/**
 * Owns the `GET api/abwab/tree` snapshot: loading/error/empty state, and the pure
 * DTO → view-model build (abwab-tree.builder.ts). `load()` and `refresh()` share one
 * `fetch()` — `refresh()` returns the in-flight observable (rather than firing a second
 * request) so the write controller can chain `selection.rebindTo(...)` onto the exact
 * snapshot this call produced (§4.6).
 *
 * On failure the previous snapshot is left in place — API_INTEGRATION_GUIDELINES.md:
 * "do not leave pages blank during loading or failure." Only a successful response
 * replaces `rawTree`.
 */
@Injectable({ providedIn: 'root' })
export class AbwabSnapshotFacade {
  private readonly api = inject(AbwabApi);

  private readonly rawTree = signal<AbwabTreeDto | null>(null);
  private readonly loadingState = signal(false);
  private readonly errorState = signal<string | null>(null);
  private pendingRequest: Subscription | null = null;

  readonly isLoading = this.loadingState.asReadonly();
  readonly errorMessage = this.errorState.asReadonly();

  readonly snapshot = computed<AbwabTreeSnapshotVm | null>(() => {
    const dto = this.rawTree();
    return dto === null ? null : buildAbwabTreeSnapshot(dto);
  });

  readonly isEmpty = computed(() => (this.rawTree()?.doors.length ?? -1) === 0);

  load(): void {
    this.fetch().subscribe();
  }

  refresh(): Observable<AbwabTreeSnapshotVm | null> {
    return this.fetch();
  }

  private fetch(): Observable<AbwabTreeSnapshotVm | null> {
    this.pendingRequest?.unsubscribe();
    this.loadingState.set(true);
    this.errorState.set(null);

    const request$ = this.api.getTree().pipe(
      tap((response) => {
        this.loadingState.set(false);
        if (response.isSuccess && response.data) {
          this.rawTree.set(response.data);
          this.errorState.set(null);
        } else {
          this.errorState.set(response.message ?? ABWAB_LABELS.loadErrorFallback);
        }
      }),
      map(() => this.snapshot()),
      catchError(() => {
        this.loadingState.set(false);
        this.errorState.set(ABWAB_LABELS.loadErrorFallback);
        return of(this.snapshot());
      }),
      shareReplay(1),
    );

    this.pendingRequest = request$.subscribe();
    return request$;
  }
}
