import { Injectable, Signal, WritableSignal, inject, signal } from '@angular/core';
import { Observable, forkJoin, map, of, tap } from 'rxjs';

import { LinkingResolvedSourcePageDto } from '../../../core/api/generated/models/linking-resolved-source-page-dto';
import { LinkingSourcePagesApi } from '../data-access/linking-source-pages.api';
import {
  LINKING_SOURCE_PAGE_CACHE_BUDGET,
  LINKING_SOURCE_PAGE_CACHE_TTL_MS,
} from '../linking.policy';
import {
  LinkingPageRange,
  LinkingSourcePage,
  LinkingSourcePageRequest,
} from '../models/linking-page.models';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingAyah } from '../models/linking-ayah.models';
import { linkingSourceKey } from '../utils/linking-source-key';
import { LinkingPageCache } from './linking-page-cache';
import { LinkingPageRequestScheduler } from './linking-page-request.scheduler';
import { LinkingQuranEntityStore } from './linking-quran-entity.store';

export interface LinkingSourcePageState {
  status: 'idle' | 'loading' | 'ready' | 'error';
  errorMessage: string | null;
}

interface SourceGenerationIdentity {
  revision: number;
  resolutionIdentity: string;
  sourceViewIdentity: string;
}

@Injectable({ providedIn: 'root' })
export class LinkingSourcePagesFacade {
  private readonly api = inject(LinkingSourcePagesApi);
  private readonly entities = inject(LinkingQuranEntityStore);
  private readonly scheduler = inject(LinkingPageRequestScheduler);
  private readonly states = new Map<string, WritableSignal<LinkingSourcePageState>>();
  private readonly identities = new Map<string, SourceGenerationIdentity>();
  private readonly activeRanges = new Map<string, LinkingPageRange<LinkingSourcePage>>();
  private readonly transientLeases = new WeakMap<LinkingSourcePage, string>();
  private readonly cache = new LinkingPageCache<LinkingSourcePage>(
    LINKING_SOURCE_PAGE_CACHE_BUDGET,
    LINKING_SOURCE_PAGE_CACHE_TTL_MS,
    (key) => this.entities.release(this.cacheLease(key)),
  );

  stateFor(scope: string): Signal<LinkingSourcePageState> {
    return this.stateSignal(scope).asReadonly();
  }

  loadRange(
    scope: string,
    request: Omit<LinkingSourcePageRequest, 'page'>,
    startIndex: number,
    endIndex: number,
  ): Observable<LinkingPageRange<LinkingSourcePage>> {
    const pageNumbers = pagesForRange(startIndex, endIndex, request.pageSize);
    const state = this.stateSignal(scope);
    state.set({ status: 'loading', errorMessage: null });
    this.scheduler.cancelOlder(scope, request.draftGeneration);
    return forkJoin(pageNumbers.map((page) => this.loadPage(scope, { ...request, page }))).pipe(
      map((pages) => this.acquireRange(scope, pages)),
      tap({
        next: () => state.set({ status: 'ready', errorMessage: null }),
        error: (error: unknown) =>
          state.set({
            status: 'error',
            errorMessage: error instanceof Error ? error.message : 'تعذر تحميل صفحات المصدر.',
          }),
      }),
    );
  }

  prefetchNext(
    scope: string,
    request: Omit<LinkingSourcePageRequest, 'page'>,
    visibleEndIndex: number,
    loadedPage: LinkingSourcePage,
  ): void {
    const pageStart = (loadedPage.page - 1) * loadedPage.pageSize;
    const threshold = pageStart + Math.floor(loadedPage.pageSize * 0.75);
    if (visibleEndIndex < threshold || loadedPage.page >= loadedPage.totalPages) {
      return;
    }
    this.loadPage(scope, { ...request, page: loadedPage.page + 1 }).subscribe({
      next: (page) => this.releaseTransient(page),
      error: () => undefined,
    });
  }

  cancel(scope: string): void {
    this.scheduler.cancelScope(scope);
    this.activeRanges.get(scope)?.release();
    this.activeRanges.delete(scope);
    for (const key of [...this.identities.keys()]) {
      if (key.startsWith(`${scope}:`)) {
        this.identities.delete(key);
      }
    }
  }

  evictRevision(linkingDataRevision: number): void {
    this.cache.deleteWhere((_key, page) => page.linkingDataRevision === linkingDataRevision);
  }

  displayAyah(page: LinkingSourcePage, ayahId: number): LinkingAyah | null {
    const ayah = this.entities.ayah(page.linkingDataRevision, ayahId);
    if (ayah === null) {
      return null;
    }
    const matches = new Set(page.matchedWordIdsByAyahId[ayahId] ?? []);
    const words = (page.wordIdsByAyahId[ayahId] ?? [])
      .map((wordId) => this.entities.word(page.linkingDataRevision, wordId))
      .filter((word) => word !== null)
      .map((word) => ({
        renderPosition: word.wordNumber,
        canonicalQuranWordId: word.id,
        textUthmani: word.textUthmani,
        isAyahMarker: word.isAyahMarker,
        isSourceMatch: matches.has(word.id),
      }));
    return {
      verseKey: ayah.verseKey,
      ayahId: ayah.id,
      surahNumber: ayah.surahNumber,
      surahNameArabic: ayah.surahNameArabic,
      ayahNumber: ayah.ayahNumber,
      pageNumber: ayah.pageFrom,
      words,
    };
  }

  private loadPage(scope: string, request: LinkingSourcePageRequest): Observable<LinkingSourcePage> {
    const normalized = this.withKnownIdentity(scope, request);
    const known = this.identities.get(this.generationKey(scope, request.draftGeneration));
    const cacheKey = known === undefined
      ? null
      : sourcePageCacheKey(
          known.revision,
          known.resolutionIdentity,
          known.sourceViewIdentity,
          request.pageSize,
          request.page,
        );
    const cached = cacheKey === null ? null : this.cache.get(cacheKey);
    if (cached !== null) {
      return of(cached);
    }
    const requestKey = sourceRequestKey(normalized);
    return this.scheduler.schedule(scope, requestKey, request.draftGeneration, () =>
      this.api.load(normalized).pipe(map((dto) => this.acceptFreshPage(scope, normalized, dto))),
    );
  }

  private acceptFreshPage(
    scope: string,
    request: LinkingSourcePageRequest,
    dto: LinkingResolvedSourcePageDto,
  ): LinkingSourcePage {
    validatePage(dto, request);
    const identityKey = this.generationKey(scope, request.draftGeneration);
    const known = this.identities.get(identityKey);
    if (known !== undefined && !sameIdentity(known, dto)) {
      throw new Error('تغيّرت هوية صفحة المصدر أثناء التحميل.');
    }
    this.identities.set(identityKey, {
      revision: dto.linkingDataRevision,
      resolutionIdentity: dto.resolutionIdentity,
      sourceViewIdentity: dto.sourceViewIdentity,
    });
    const page = toPage(dto);
    const key = sourcePageCacheKey(
      page.linkingDataRevision,
      page.resolutionIdentity,
      page.sourceViewIdentity,
      page.pageSize,
      page.page,
    );
    const existing = this.cache.get(key);
    if (existing !== null) {
      const verificationLease = `source-verify:${crypto.randomUUID()}`;
      this.entities.insertPage(dto.linkingDataRevision, dto.items, verificationLease);
      this.entities.release(verificationLease);
      return existing;
    }
    const lease = this.cacheLease(key);
    this.entities.insertPage(dto.linkingDataRevision, dto.items, lease);
    if (!this.cache.set(key, page, page.weight)) {
      this.transientLeases.set(page, lease);
      queueMicrotask(() => this.releaseTransient(page));
    }
    return page;
  }

  private acquireRange(
    scope: string,
    pages: readonly LinkingSourcePage[],
  ): LinkingPageRange<LinkingSourcePage> {
    this.activeRanges.get(scope)?.release();
    const leaseIds = pages.map((page) => {
      const lease = `range:${crypto.randomUUID()}`;
      this.entities.retainPage(
        page.linkingDataRevision,
        page.ayahIds,
        page.wordIdsByAyahId,
        lease,
      );
      this.releaseTransient(page);
      return lease;
    });
    let released = false;
    const range: LinkingPageRange<LinkingSourcePage> = {
      pages,
      release: () => {
        if (released) {
          return;
        }
        released = true;
        leaseIds.forEach((lease) => this.entities.release(lease));
        if (this.activeRanges.get(scope) === range) {
          this.activeRanges.delete(scope);
        }
      },
    };
    this.activeRanges.set(scope, range);
    return range;
  }

  private releaseTransient(page: LinkingSourcePage): void {
    const lease = this.transientLeases.get(page);
    if (lease !== undefined) {
      this.transientLeases.delete(page);
      this.entities.release(lease);
    }
  }

  private withKnownIdentity(
    scope: string,
    request: LinkingSourcePageRequest,
  ): LinkingSourcePageRequest {
    const known = this.identities.get(this.generationKey(scope, request.draftGeneration));
    return known === undefined
      ? request
      : {
          ...request,
          expectedLinkingDataRevision: known.revision,
          expectedSourceViewIdentity: known.sourceViewIdentity,
        };
  }

  private stateSignal(scope: string): WritableSignal<LinkingSourcePageState> {
    let state = this.states.get(scope);
    if (state === undefined) {
      state = signal<LinkingSourcePageState>({ status: 'idle', errorMessage: null });
      this.states.set(scope, state);
    }
    return state;
  }

  private generationKey(scope: string, generation: number): string {
    return `${scope}:${generation}`;
  }

  private cacheLease(key: string): string {
    return `source-cache:${key}`;
  }
}

export function sourcePageCacheKey(
  revision: number,
  resolutionIdentity: string,
  sourceViewIdentity: string,
  pageSize: number,
  page: number,
): string {
  return `${revision}:${resolutionIdentity}:${sourceViewIdentity}:${pageSize}:${page}`;
}

function sourceRequestKey(request: LinkingSourcePageRequest): string {
  return JSON.stringify([
    request.expectedLinkingDataRevision,
    request.expectedSourceViewIdentity,
    linkingSourceKey(request.source),
    request.view.segment,
    request.view.inclusionMode,
    request.view.ayahOverrideIds,
    request.pageSize,
    request.page,
  ]);
}

function pagesForRange(startIndex: number, endIndex: number, pageSize: number): readonly number[] {
  if (pageSize <= 0 || startIndex < 0 || endIndex < startIndex) {
    throw new Error('نطاق صفحة الربط غير صالح.');
  }
  const first = Math.floor(startIndex / pageSize) + 1;
  const last = Math.floor(endIndex / pageSize) + 1;
  return Array.from({ length: last - first + 1 }, (_, index) => first + index);
}

function validatePage(dto: LinkingResolvedSourcePageDto, request: LinkingSourcePageRequest): void {
  if (
    dto.page !== request.page ||
    dto.pageSize !== request.pageSize ||
    dto.linkingDataRevision <= 0 ||
    dto.totalPages < 0 ||
    dto.totalAyahCount < 0 ||
    dto.items.some((ayah) => ayah.ayahId <= 0 || !ayah.verseKey || ayah.words.length === 0)
  ) {
    throw new Error('استجابة صفحة المصدر غير صالحة.');
  }
  if (
    request.expectedLinkingDataRevision !== null &&
    dto.linkingDataRevision !== request.expectedLinkingDataRevision
  ) {
    throw new Error('تغيّرت مراجعة بيانات الربط أثناء التحميل.');
  }
}

function toPage(dto: LinkingResolvedSourcePageDto): LinkingSourcePage {
  const textUnits = dto.items.reduce(
    (sum, ayah) =>
      sum + ayah.verseKey.length + ayah.surahNameArabic.length + ayah.words.reduce((n, word) => n + word.textUthmani.length, 0),
    0,
  );
  const wordCount = dto.items.reduce((sum, ayah) => sum + ayah.words.length, 0);
  const matchCount = dto.items.reduce((sum, ayah) => sum + ayah.matchedQuranWordIds.length, 0);
  return Object.freeze({
    linkingDataRevision: dto.linkingDataRevision,
    resolutionIdentity: dto.resolutionIdentity,
    sourceViewIdentity: dto.sourceViewIdentity,
    page: dto.page,
    pageSize: dto.pageSize,
    totalAyahCount: dto.totalAyahCount,
    totalPages: dto.totalPages,
    ayahIds: Object.freeze(dto.items.map((ayah) => ayah.ayahId)),
    wordIdsByAyahId: Object.freeze(
      Object.fromEntries(
        dto.items.map((ayah) => [
          ayah.ayahId,
          Object.freeze(ayah.words.map((word) => word.quranWordId)),
        ]),
      ),
    ),
    matchedWordIdsByAyahId: Object.freeze(
      Object.fromEntries(dto.items.map((ayah) => [ayah.ayahId, Object.freeze([...ayah.matchedQuranWordIds])])),
    ),
    weight: dto.items.length + wordCount + matchCount + Math.ceil(textUnits / 64),
  });
}

function sameIdentity(identity: SourceGenerationIdentity, dto: LinkingResolvedSourcePageDto): boolean {
  return (
    identity.revision === dto.linkingDataRevision &&
    identity.resolutionIdentity === dto.resolutionIdentity &&
    identity.sourceViewIdentity === dto.sourceViewIdentity
  );
}
