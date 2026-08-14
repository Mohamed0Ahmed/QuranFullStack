import { Injectable, Signal, WritableSignal, inject, signal } from '@angular/core';
import { Observable, forkJoin, map, of, tap } from 'rxjs';

import { LinkingPreparedDetailPageDto } from '../../../core/api/generated/models/linking-prepared-detail-page-dto';
import { LinkingPreparedPreflightApi } from '../data-access/linking-prepared-preflight.api';
import {
  LINKING_PREPARED_PAGE_CACHE_BUDGET,
  LINKING_PREPARED_PAGE_CACHE_TTL_MS,
} from '../linking.policy';
import {
  LinkingPageRange,
  LinkingPreparedDetailPage,
  LinkingPreparedDetailRequest,
} from '../models/linking-page.models';
import { LinkingPageCache } from './linking-page-cache';
import { LinkingPageRequestScheduler } from './linking-page-request.scheduler';
import { LinkingQuranEntityStore } from './linking-quran-entity.store';

export interface LinkingPreflightDetailState {
  status: 'idle' | 'loading' | 'ready' | 'error';
  errorMessage: string | null;
}

@Injectable({ providedIn: 'root' })
export class LinkingPreflightDetailsFacade {
  private readonly api = inject(LinkingPreparedPreflightApi);
  private readonly entities = inject(LinkingQuranEntityStore);
  private readonly scheduler = inject(LinkingPageRequestScheduler);
  private readonly states = new Map<string, WritableSignal<LinkingPreflightDetailState>>();
  private readonly activeRanges = new Map<string, LinkingPageRange<LinkingPreparedDetailPage>>();
  private readonly transientLeases = new WeakMap<LinkingPreparedDetailPage, string>();
  private readonly cache = new LinkingPageCache<LinkingPreparedDetailPage>(
    LINKING_PREPARED_PAGE_CACHE_BUDGET,
    LINKING_PREPARED_PAGE_CACHE_TTL_MS,
    (key) => this.entities.release(this.cacheLease(key)),
  );

  stateFor(preflightId: string): Signal<LinkingPreflightDetailState> {
    return this.stateSignal(preflightId).asReadonly();
  }

  loadRange(
    request: Omit<LinkingPreparedDetailRequest, 'page'>,
    startIndex: number,
    endIndex: number,
  ): Observable<LinkingPageRange<LinkingPreparedDetailPage>> {
    const scope = `prepared:${request.preflightId}`;
    const state = this.stateSignal(request.preflightId);
    state.set({ status: 'loading', errorMessage: null });
    this.scheduler.cancelOlder(scope, request.generation);
    return forkJoin(
      pagesForRange(startIndex, endIndex, request.pageSize).map((page) =>
        this.loadPage(scope, { ...request, page }),
      ),
    ).pipe(
      map((pages) => this.acquireRange(scope, pages)),
      tap({
        next: () => state.set({ status: 'ready', errorMessage: null }),
        error: (error: unknown) =>
          state.set({
            status: 'error',
            errorMessage: error instanceof Error ? error.message : 'تعذر تحميل تفاصيل المراجعة.',
          }),
      }),
    );
  }

  cancel(preflightId: string): void {
    const scope = `prepared:${preflightId}`;
    this.scheduler.cancelScope(scope);
    this.activeRanges.get(scope)?.release();
    this.activeRanges.delete(scope);
  }

  evict(preflightId: string): void {
    this.cancel(preflightId);
    this.cache.deleteWhere((_key, page) => page.preflightId === preflightId);
  }

  private loadPage(
    scope: string,
    request: LinkingPreparedDetailRequest,
  ): Observable<LinkingPreparedDetailPage> {
    const key = preparedDetailCacheKey(request);
    const cached = this.cache.get(key);
    if (cached !== null) {
      return of(cached);
    }
    return this.scheduler.schedule(scope, key, request.generation, () =>
      this.api.loadDetails(request).pipe(map((dto) => this.acceptFreshPage(request, key, dto))),
    );
  }

  private acceptFreshPage(
    request: LinkingPreparedDetailRequest,
    key: string,
    dto: LinkingPreparedDetailPageDto,
  ): LinkingPreparedDetailPage {
    validatePage(request, dto);
    const page = toPage(dto);
    const existing = this.cache.get(key);
    if (existing !== null) {
      const verificationLease = `prepared-verify:${crypto.randomUUID()}`;
      this.entities.insertPage(
        dto.linkingDataRevision,
        dto.items.map((item) => item.ayah),
        verificationLease,
      );
      this.entities.release(verificationLease);
      return existing;
    }
    const lease = this.cacheLease(key);
    this.entities.insertPage(dto.linkingDataRevision, dto.items.map((item) => item.ayah), lease);
    if (!this.cache.set(key, page, page.weight)) {
      this.transientLeases.set(page, lease);
      queueMicrotask(() => {
        const transientLease = this.transientLeases.get(page);
        if (transientLease !== undefined) {
          this.transientLeases.delete(page);
          this.entities.release(transientLease);
        }
      });
    }
    return page;
  }

  private acquireRange(
    scope: string,
    pages: readonly LinkingPreparedDetailPage[],
  ): LinkingPageRange<LinkingPreparedDetailPage> {
    this.activeRanges.get(scope)?.release();
    const leases = pages.map((page) => {
      const lease = `prepared-range:${crypto.randomUUID()}`;
      this.entities.retainPage(page.linkingDataRevision, page.ayahIds, lease);
      const transientLease = this.transientLeases.get(page);
      if (transientLease !== undefined) {
        this.transientLeases.delete(page);
        this.entities.release(transientLease);
      }
      return lease;
    });
    let released = false;
    const range: LinkingPageRange<LinkingPreparedDetailPage> = {
      pages,
      release: () => {
        if (released) {
          return;
        }
        released = true;
        leases.forEach((lease) => this.entities.release(lease));
        if (this.activeRanges.get(scope) === range) {
          this.activeRanges.delete(scope);
        }
      },
    };
    this.activeRanges.set(scope, range);
    return range;
  }

  private stateSignal(preflightId: string): WritableSignal<LinkingPreflightDetailState> {
    let state = this.states.get(preflightId);
    if (state === undefined) {
      state = signal<LinkingPreflightDetailState>({ status: 'idle', errorMessage: null });
      this.states.set(preflightId, state);
    }
    return state;
  }

  private cacheLease(key: string): string {
    return `prepared-cache:${key}`;
  }
}

export function preparedDetailCacheKey(request: LinkingPreparedDetailRequest): string {
  return JSON.stringify([
    request.linkingDataRevision,
    request.preflightId,
    request.detailKind,
    request.preparedSourceId,
    request.filter,
    request.pageSize,
    request.page,
  ]);
}

function pagesForRange(startIndex: number, endIndex: number, pageSize: number): readonly number[] {
  if (pageSize <= 0 || startIndex < 0 || endIndex < startIndex) {
    throw new Error('نطاق تفاصيل المراجعة غير صالح.');
  }
  const first = Math.floor(startIndex / pageSize) + 1;
  const last = Math.floor(endIndex / pageSize) + 1;
  return Array.from({ length: last - first + 1 }, (_, index) => first + index);
}

function validatePage(
  request: LinkingPreparedDetailRequest,
  dto: LinkingPreparedDetailPageDto,
): void {
  if (
    dto.linkingDataRevision !== request.linkingDataRevision ||
    dto.preflightId !== request.preflightId ||
    dto.page !== request.page ||
    dto.pageSize !== request.pageSize ||
    dto.items.some((item) => item.ayah.ayahId <= 0 || item.ayah.words.length === 0)
  ) {
    throw new Error('استجابة تفاصيل المراجعة غير صالحة.');
  }
}

function toPage(dto: LinkingPreparedDetailPageDto): LinkingPreparedDetailPage {
  const overlays = dto.items.flatMap((item) => item.sourceOverlays);
  const wordCount = dto.items.reduce((sum, item) => sum + item.ayah.words.length, 0);
  const overlayReferences = overlays.reduce(
    (sum, overlay) =>
      sum +
      overlay.matchedQuranWordIds.length +
      overlay.requestedQuranWordIds.length +
      overlay.overlappingSources.length +
      overlay.descriptions.length +
      4,
    0,
  );
  const textUnits = dto.items.reduce(
    (sum, item) =>
      sum + item.ayah.words.reduce((wordSum, word) => wordSum + word.textUthmani.length, 0),
    0,
  );
  return Object.freeze({
    linkingDataRevision: dto.linkingDataRevision,
    preflightId: dto.preflightId,
    detailKind: dto.detailKind,
    preparedSourceId: dto.preparedSourceId,
    filter: dto.filter,
    page: dto.page,
    pageSize: dto.pageSize,
    totalItems: dto.totalItems,
    totalPages: dto.totalPages,
    ayahIds: Object.freeze(dto.items.map((item) => item.ayah.ayahId)),
    overlaysByAyahId: Object.freeze(
      Object.fromEntries(dto.items.map((item) => [item.ayah.ayahId, Object.freeze(item.sourceOverlays)])),
    ),
    weight: dto.items.length + wordCount + overlayReferences + Math.ceil(textUnits / 64),
  });
}
