import { Injectable, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { EMPTY, Observable, Subscription, forkJoin, map, of, switchMap } from 'rxjs';
import { catchError, distinctUntilChanged, tap } from 'rxjs/operators';

import { PhraseOccurrencePageResponse } from '../../../../core/api/generated/models/phrase-occurrence-page-response';
import { PhraseRepetitionsPageResponse } from '../../../../core/api/generated/models/phrase-repetitions-page-response';
import { PhraseSearchCapabilitiesResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response';
import { lastPageNumber } from '../../../../shared/ui/pagination/pagination-range';
import { PhraseRepetitionsApi } from '../data-access/phrase-repetitions.api';
import {
  DEFAULT_PHRASE_REPETITIONS_URL_STATE,
  PHRASE_REPETITIONS_PAGE_SIZE,
  ParsedPhraseRepetitionsUrlState,
  PhraseLoadStatus,
  PhraseRepetitionSort,
  PhraseRepetitionsState,
  PhraseRepetitionsUrlState,
  PhraseTextMode,
} from '../models/phrase-repetitions.models';
import { phraseEnvelopeFailure, phraseRequestFailure } from './phrase-request-failure';
import {
  clearPhraseRoute,
  defaultPhraseRepetitionsRoute,
  selectPhraseRoute,
  updatePhraseListRoute,
  updatePhraseOccurrencePageRoute,
} from './phrase-repetitions-route-updates';
import {
  parsePhraseRepetitionsUrlState,
  phraseRepetitionsUrlStateKey,
  phraseRepetitionsRouteStateKey,
  serializePhraseRepetitionsUrlState,
} from './phrase-repetitions-url-sync';
import { encodePhraseQuery } from './phrase-query-encoding';

const INVALID_URL_MESSAGE = 'رابط البحث غير صالح أو يحتوي على خيارات غير متاحة.';
const INDEX_CHANGED_MESSAGE = 'تغير فهرس البحث، أعد اختيار النتيجة';
const INDEX_UNAVAILABLE_MESSAGE = 'فهرس البحث غير متاح الآن. أعد المحاولة بعد اكتمال بنائه.';
const PHRASE_REFERENCE_CHANGED_MESSAGE =
  'تعذر استعادة العبارة المحددة بهذه الخيارات، أعد اختيارها من القائمة.';

@Injectable()
export class PhraseRepetitionsFacade {
  private readonly api = inject(PhraseRepetitionsApi);
  private readonly router = inject(Router);

  private readonly _route = signal<PhraseRepetitionsUrlState>(
    DEFAULT_PHRASE_REPETITIONS_URL_STATE,
  );
  private readonly _routeInvalid = signal(false);
  private readonly _capabilitiesStatus = signal<PhraseLoadStatus>('idle');
  private readonly _capabilities = signal<PhraseSearchCapabilitiesResponse | null>(null);
  private readonly _listStatus = signal<PhraseLoadStatus>('idle');
  private readonly _list = signal<PhraseRepetitionsPageResponse | null>(null);
  private readonly _occurrencesStatus = signal<PhraseLoadStatus>('idle');
  private readonly _occurrences = signal<PhraseOccurrencePageResponse | null>(null);
  private readonly _errorMessage = signal('');
  private readonly _occurrencesErrorMessage = signal('');
  private readonly _indexNotice = signal('');

  private route?: ActivatedRoute;
  private routeSub?: Subscription;
  private manualLoadSub?: Subscription;
  private listRequestKey: string | null = null;
  private occurrencesRequestKey: string | null = null;

  readonly state = computed<PhraseRepetitionsState>(() => ({
    route: this._route(),
    routeInvalid: this._routeInvalid(),
    capabilitiesStatus: this._capabilitiesStatus(),
    capabilities: this._capabilities(),
    listStatus: this._listStatus(),
    list: this._list(),
    occurrencesStatus: this._occurrencesStatus(),
    occurrences: this._occurrences(),
    errorMessage: this._errorMessage(),
    occurrencesErrorMessage: this._occurrencesErrorMessage(),
    indexNotice: this._indexNotice(),
  }));

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();
    this.route = route;
    this.routeSub = route.queryParamMap
      .pipe(
        tap(() => {
          this.manualLoadSub?.unsubscribe();
          this.manualLoadSub = undefined;
        }),
        map(parsePhraseRepetitionsUrlState),
        distinctUntilChanged(
          (previous, current) =>
            phraseRepetitionsUrlStateKey(previous) === phraseRepetitionsUrlStateKey(current),
        ),
        switchMap((parsed) => this.runRoute(parsed)),
      )
      .subscribe();
  }

  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.manualLoadSub?.unsubscribe();
    this.routeSub = undefined;
    this.manualLoadSub = undefined;
    this.route = undefined;
  }

  setMode(mode: PhraseTextMode): void {
    const modeCapabilities = this._capabilities()?.modes.find((item) => item.mode === mode);
    if (!modeCapabilities) {
      return;
    }
    const current = this._route();
    const length = modeCapabilities.repeatedLengths.includes(current.length)
      ? current.length
      : (modeCapabilities.repeatedLengths[0] ?? current.length);
    if (current.mode === mode && current.length === length) {
      return;
    }
    this.startListTransition();
    this.navigate(updatePhraseListRoute(current, this.currentBuildId(), { mode, length }));
  }

  setLength(length: number): void {
    if (length === this._route().length) {
      return;
    }
    this.startListTransition();
    this.navigate(updatePhraseListRoute(this._route(), this.currentBuildId(), { length }));
  }

  setSort(sort: PhraseRepetitionSort): void {
    if (sort === this._route().sort) {
      return;
    }
    this.startListTransition();
    this.navigate(updatePhraseListRoute(this._route(), this.currentBuildId(), { sort }));
  }

  setQuery(rawQuery: string): void {
    const query = normalizeSearchQuery(rawQuery);
    if (query === this._route().query) {
      return;
    }
    this.startListTransition();
    this.navigate(updatePhraseListRoute(this._route(), this.currentBuildId(), { query }));
  }

  setPage(page: number): void {
    if (page === this._route().page) {
      return;
    }
    this.startListTransition();
    this.navigate(updatePhraseListRoute(this._route(), this.currentBuildId(), { page }));
  }

  selectPhrase(variantId: number): void {
    if (variantId === this._route().phrase) {
      return;
    }
    this.navigate(selectPhraseRoute(this._route(), this.currentBuildId(), variantId));
  }

  clearPhrase(): void {
    this.navigate(clearPhraseRoute(this._route()));
  }

  setOccurrencePage(page: number): void {
    if (this._route().phrase === null) {
      return;
    }
    this.navigate(updatePhraseOccurrencePageRoute(this._route(), page));
  }

  retry(): void {
    const route = this._route();
    this.listRequestKey = null;
    this.occurrencesRequestKey = null;
    this.manualLoadSub?.unsubscribe();
    this.manualLoadSub = this.runRoute({ state: route, invalid: false }).subscribe();
  }

  resetInvalidState(): void {
    this.navigate(defaultPhraseRepetitionsRoute(this._capabilities()), true);
  }

  dismissIndexNotice(): void {
    this._indexNotice.set('');
  }

  private runRoute(parsed: ParsedPhraseRepetitionsUrlState): Observable<void> {
    this.setRouteState(parsed.state);
    this._routeInvalid.set(parsed.invalid);
    this._errorMessage.set('');
    this._occurrencesErrorMessage.set('');

    if (parsed.invalid) {
      this._listStatus.set('invalid');
      this._occurrencesStatus.set('idle');
      this._errorMessage.set(INVALID_URL_MESSAGE);
      return EMPTY;
    }

    const cachedCapabilities = this._capabilities();
    if (
      cachedCapabilities &&
      this._capabilitiesStatus() !== 'error' &&
      this._capabilitiesStatus() !== 'unavailable'
    ) {
      this._capabilitiesStatus.set('success');
      return this.loadForCapabilities(parsed.state, cachedCapabilities);
    }

    this._capabilitiesStatus.set(this._capabilities() ? 'refreshing' : 'loading');
    return this.api.getCapabilities().pipe(
      switchMap((response) => {
        if (!this.isCurrentRoute(parsed.state)) {
          return EMPTY;
        }
        if (!response.isSuccess || !response.data) {
          const failure = phraseEnvelopeFailure(response.errors, response.message);
          this._capabilitiesStatus.set(failure.status);
          this._errorMessage.set(failure.message);
          return EMPTY;
        }

        const capabilities = response.data;
        this._capabilities.set(capabilities);
        this._capabilitiesStatus.set('success');
        return this.loadForCapabilities(parsed.state, capabilities);
      }),
      catchError((error: unknown) => {
        if (!this.isCurrentRoute(parsed.state)) {
          return of(undefined);
        }
        const failure = phraseRequestFailure(error);
        this._capabilitiesStatus.set(failure.status);
        this._errorMessage.set(failure.message);
        return of(undefined);
      }),
    );
  }

  private loadForCapabilities(
    route: PhraseRepetitionsUrlState,
    capabilities: PhraseSearchCapabilitiesResponse,
  ): Observable<void> {
    if (!capabilities.exactReady) {
      this._capabilitiesStatus.set('unavailable');
      this._errorMessage.set(INDEX_UNAVAILABLE_MESSAGE);
      return EMPTY;
    }
    if (!this.supportsRoute(capabilities, route)) {
      this._routeInvalid.set(true);
      this._listStatus.set('invalid');
      this._occurrencesStatus.set('idle');
      this._errorMessage.set(INVALID_URL_MESSAGE);
      return EMPTY;
    }
    if (route.build === null) {
      this.navigate({ ...route, build: capabilities.activeBuildId }, true);
      return EMPTY;
    }
    if (!sameBuild(route.build, capabilities.activeBuildId)) {
      this.resetForBuildChange(capabilities.activeBuildId, true);
      return EMPTY;
    }
    return forkJoin([
      this.loadRepetitions(route),
      this.loadOccurrences(route, capabilities.maximumRepetitionOccurrencePageSize),
    ]).pipe(map(() => undefined));
  }

  private loadRepetitions(route: PhraseRepetitionsUrlState): Observable<void> {
    const requestKey = [
      route.build,
      route.mode,
      route.length,
      route.query,
      route.sort,
      route.page,
    ].join('|');
    if (
      requestKey === this.listRequestKey &&
      (this._listStatus() === 'success' || this._listStatus() === 'empty')
    ) {
      return of(undefined);
    }

    this._listStatus.set(this._list() ? 'refreshing' : 'loading');
    return this.api
      .getRepetitions(
        route.mode,
        route.length,
        route.query ? encodePhraseQuery(route.query) : null,
        route.sort,
        route.page,
        PHRASE_REPETITIONS_PAGE_SIZE,
      )
      .pipe(
        tap((response) => {
          if (!this.isCurrentRoute(route)) {
            return;
          }
          if (!response.isSuccess || !response.data) {
            const failure = phraseEnvelopeFailure(response.errors, response.message);
            this._listStatus.set(failure.status);
            this._errorMessage.set(failure.message);
            return;
          }
          if (!sameBuild(route.build, response.data.activeBuildId)) {
            this.resetForBuildChange(response.data.activeBuildId);
            return;
          }
          const lastPage = lastPageNumber(response.data.pageSize, response.data.totalCount);
          if (route.page > lastPage) {
            this.listRequestKey = null;
            this.navigate(
              updatePhraseListRoute(route, route.build, { page: lastPage }),
              true,
            );
            return;
          }
          this.listRequestKey = requestKey;
          this._list.set(response.data);
          this._listStatus.set(response.data.totalCount === 0 ? 'empty' : 'success');
          this._errorMessage.set('');
        }),
        catchError((error: unknown) => {
          if (!this.isCurrentRoute(route)) {
            return of(undefined);
          }
          const failure = phraseRequestFailure(error);
          if (failure.status === 'stale') {
            this.resetForBuildChange(null);
          } else {
            this._listStatus.set(failure.status);
            this._errorMessage.set(failure.message);
          }
          return of(undefined);
        }),
        map(() => undefined),
      );
  }

  private loadOccurrences(
    route: PhraseRepetitionsUrlState,
    pageSize: number,
  ): Observable<void> {
    if (route.phrase === null || route.build === null) {
      this.occurrencesRequestKey = null;
      this._occurrences.set(null);
      this._occurrencesStatus.set('idle');
      return of(undefined);
    }

    const requestKey = [route.build, route.phrase, route.occPage, pageSize].join('|');
    if (
      requestKey === this.occurrencesRequestKey &&
      (this._occurrencesStatus() === 'success' || this._occurrencesStatus() === 'empty')
    ) {
      return of(undefined);
    }

    this._occurrencesStatus.set(this._occurrences() ? 'refreshing' : 'loading');
    return this.api
      .getOccurrences(
        route.build,
        route.phrase,
        route.occPage,
        pageSize,
      )
      .pipe(
        tap((response) => {
          if (!this.isCurrentRoute(route)) {
            return;
          }
          if (!response.isSuccess || !response.data) {
            const failure = phraseEnvelopeFailure(response.errors, response.message);
            if (failure.status === 'stale') {
              this.resetForBuildChange(null);
            } else {
              this._occurrencesStatus.set(failure.status);
              this._occurrencesErrorMessage.set(failure.message);
            }
            return;
          }
          if (!sameBuild(route.build, response.data.activeBuildId)) {
            this.resetForBuildChange(response.data.activeBuildId);
            return;
          }
          if (
            response.data.phrase.variantId !== route.phrase ||
            response.data.phrase.mode !== route.mode ||
            response.data.phrase.wordCount !== route.length
          ) {
            this.clearMismatchedPhraseReference(route);
            return;
          }
          const lastPage = lastPageNumber(response.data.pageSize, response.data.totalCount);
          if (route.occPage > lastPage) {
            this.occurrencesRequestKey = null;
            this.navigate(updatePhraseOccurrencePageRoute(route, lastPage), true);
            return;
          }
          this.occurrencesRequestKey = requestKey;
          this._occurrences.set(response.data);
          this._occurrencesStatus.set(response.data.totalCount === 0 ? 'empty' : 'success');
          this._occurrencesErrorMessage.set('');
        }),
        catchError((error: unknown) => {
          if (!this.isCurrentRoute(route)) {
            return of(undefined);
          }
          const failure = phraseRequestFailure(error);
          if (failure.status === 'stale') {
            this.resetForBuildChange(null);
          } else {
            this._occurrencesStatus.set(failure.status);
            this._occurrencesErrorMessage.set(failure.message);
          }
          return of(undefined);
        }),
        map(() => undefined),
      );
  }

  private supportsRoute(
    capabilities: PhraseSearchCapabilitiesResponse,
    route: PhraseRepetitionsUrlState,
  ): boolean {
    const mode = capabilities.modes.find((item) => item.mode === route.mode);
    return mode !== undefined && mode.repeatedLengths.includes(route.length);
  }

  private resetForBuildChange(
    activeBuildId: string | null,
    preserveCapabilities = false,
  ): void {
    this.listRequestKey = null;
    this.occurrencesRequestKey = null;
    this._list.set(null);
    this._occurrences.set(null);
    if (!preserveCapabilities) {
      this._capabilities.set(null);
      this._capabilitiesStatus.set('idle');
    }
    this._listStatus.set('stale');
    this._occurrencesStatus.set('stale');
    this._indexNotice.set(INDEX_CHANGED_MESSAGE);
    this.navigate(
      {
        ...this._route(),
        build: activeBuildId,
        page: 1,
        phrase: null,
        occPage: 1,
      },
      true,
    );
  }

  private clearMismatchedPhraseReference(route: PhraseRepetitionsUrlState): void {
    this.occurrencesRequestKey = null;
    this._occurrences.set(null);
    this._occurrencesStatus.set('invalid');
    this._indexNotice.set(PHRASE_REFERENCE_CHANGED_MESSAGE);
    this.navigate(clearPhraseRoute(route), true);
  }

  private isCurrentRoute(route: PhraseRepetitionsUrlState): boolean {
    return phraseRepetitionsRouteStateKey(route) === phraseRepetitionsRouteStateKey(this._route());
  }

  private startListTransition(): void {
    this._listStatus.set(this._list() ? 'refreshing' : 'loading');
  }

  private setRouteState(route: PhraseRepetitionsUrlState): void {
    if (!sameOccurrenceIdentity(this._route(), route)) {
      this.occurrencesRequestKey = null;
      this._occurrences.set(null);
      this._occurrencesStatus.set(route.phrase === null ? 'idle' : 'loading');
    }
    this._route.set(route);
  }

  private currentBuildId(): string | null {
    return this._capabilities()?.activeBuildId ?? this._route().build;
  }

  private navigate(state: PhraseRepetitionsUrlState, replaceUrl = false): void {
    if (!this.route) {
      return;
    }
    this.setRouteState(state);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: serializePhraseRepetitionsUrlState(state),
      replaceUrl,
    });
  }
}

function sameBuild(expected: string | null, actual: string): boolean {
  return expected !== null && expected.toLowerCase() === actual.toLowerCase();
}

function sameOccurrenceIdentity(
  previous: PhraseRepetitionsUrlState,
  current: PhraseRepetitionsUrlState,
): boolean {
  return (
    previous.build === current.build &&
    previous.phrase === current.phrase &&
    previous.mode === current.mode &&
    previous.length === current.length &&
    previous.query === current.query
  );
}

function normalizeSearchQuery(value: string): string {
  return value.trim().replace(/\s+/g, ' ');
}
