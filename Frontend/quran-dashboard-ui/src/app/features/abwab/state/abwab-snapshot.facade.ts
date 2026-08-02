import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Observable, Subscription, catchError, map, of, shareReplay, tap } from 'rxjs';

import { AbwabApi } from '../data-access/abwab.api';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import { AbwabTreeSnapshotVm } from '../models/abwab.models';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { buildAbwabTreeSnapshot } from './abwab-tree.builder';

/**
 * Owns the `GET api/abwab/tree` snapshot: loading/error/empty state, and the pure
 * DTO → view-model build (abwab-tree.builder.ts). `load()` and `refresh()` both go through
 * `fetch()`, which always issues a **new** request and cancels any still-pending one —
 * §4.6's refetch-after-every-write invariant means a coalesced or cached read would hand
 * back exactly the stale snapshot the invariant exists to prevent. The returned observable
 * is `shareReplay(1)`-backed so the caller (the write controller) sees the same snapshot the
 * internal subscription produced and can chain `selection.rebindTo(...)` onto it.
 *
 * On failure the previous snapshot is left in place — API_INTEGRATION_GUIDELINES.md:
 * "do not leave pages blank during loading or failure." Only a successful response
 * replaces `rawTree`.
 *
 * The `If-None-Match` validator is stored beside the snapshot and the two are written as one unit,
 * so a validator can never outlive the value it validates. A `304` reuses the failure path's
 * keep-previous-value semantics without the error: nothing changed, so nothing is replaced and
 * nothing is reported.
 */
@Injectable({ providedIn: 'root' })
export class AbwabSnapshotFacade {
  private readonly api = inject(AbwabApi);

  private readonly rawTree = signal<AbwabTreeDto | null>(null);
  private readonly etagState = signal<string | null>(null);
  private readonly loadingState = signal(false);
  private readonly errorState = signal<string | null>(null);
  private pendingRequest: Subscription | null = null;

  readonly isLoading = this.loadingState.asReadonly();
  readonly errorMessage = this.errorState.asReadonly();

  /** `bootId + tree generation` as the server computed it — the one identity every write that can
   * change a door list moves, rename included. Exposed so client-side caches keyed on "the tree I
   * hold" can key on the server's own answer instead of inventing a second, weaker one
   * (`AbwabTreeDto.version` is diagnostics and is blind to relation writes). */
  readonly snapshotValidator = this.etagState.asReadonly();

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

    const request$ = this.api.getTree(this.etagState()).pipe(
      tap((response) => {
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
        this.loadingState.set(false);

        // A 304 arrives on the error channel because HttpClient treats only 2xx as ok. It is the
        // opposite of a failure: the held snapshot and its validator are current, so both stay and no
        // error is reported. Checked before the generic branch, or every cached revisit shows a banner.
        if (error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotModified) {
          return of(this.snapshot());
        }

        this.errorState.set(ABWAB_LABELS.loadErrorFallback);
        return of(this.snapshot());
      }),
      shareReplay(1),
    );

    this.pendingRequest = request$.subscribe();
    return request$;
  }
}
