import { Injectable, OnDestroy, Signal, WritableSignal, computed, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subscription, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { DetailRequestLifecycle } from './detail-request-lifecycle';

/**
 * Minimal shape every words detail panel state must expose so
 * `AbstractDetailController` can drive its `status`/`errorMessage` fields
 * generically (Feature 033, decision 5 (DRY)).
 */
export interface DetailPanelStateBase {
  status: 'idle' | 'loading' | 'success' | 'empty' | 'notFound' | 'error';
  errorMessage: string;
}

/**
 * Shared route-independent detail controller skeleton (Feature 033, decision 5
 * (DRY)), extracted from the near-identical `RootsDetailController` /
 * `LemmasDetailController` / `StemsDetailController` trio (Feature 029, Change
 * B4). Every complete-identity transition abandons BOTH the summary and the
 * detail request and opens a new generation, so a late response from the
 * previously selected identity can never populate or overwrite this one — see
 * {@link DetailRequestLifecycle}.
 *
 * Owns the panel signal, the request lifecycle, and the three load-path
 * skeletons (`applyIdentity` / `loadSummaryAndRestore` / `loadActiveView`).
 * Every divergent bit — the initial panel, identity/equality comparisons, the
 * summary read + its cache key, how a summary or a not-found/error result is
 * folded into the panel, and how the active view's request/handlers are built
 * — is a hook the concrete controller implements. Entity-specific selection
 * methods (`selectX`, `selectXWithPanel`, `setView`, `setWordView`,
 * `setSurahView`, `setDetailPage`, and the lemmas/stems-only
 * `setAyahTypeCode`) stay on the concrete controller: their panel-update
 * shapes and the fields participating in each entity's URL-state identity
 * differ enough that hoisting them would trade real duplication for
 * accidental coupling.
 */
@Injectable()
export abstract class AbstractDetailController<
  TPanel extends DetailPanelStateBase,
  TUrlState,
  TSummary,
  TViewHandlers,
> implements OnDestroy
{
  protected readonly _panel: WritableSignal<TPanel>;
  readonly panelState: Signal<TPanel>;

  protected readonly requests = new DetailRequestLifecycle();
  protected activeUrlState: TUrlState | null = null;

  /**
   * `initialPanel` arrives through the constructor rather than an abstract
   * getter: TypeScript rejects reading an abstract member while the base
   * class itself is still constructing (and, at runtime, the subclass's own
   * field initializers — e.g. a class-field `INITIAL_PANEL` — have not run
   * yet at that point either), so the concrete controller passes its module-
   * level `INITIAL_PANEL` constant up through `super(INITIAL_PANEL)`.
   */
  protected constructor(protected readonly initialPanel: TPanel) {
    this._panel = signal<TPanel>(initialPanel);
    this.panelState = computed(() => this._panel());
  }

  ngOnDestroy(): void {
    this.cancelPendingLoads();
  }

  /**
   * Route-free entry point: synchronize the panel to a complete detail state
   * (`null` clears the selection). Identical states short-circuit via complete
   * identity comparison, leaving an in-flight load for that identity alone.
   */
  applyUrlState(state: TUrlState | null): void {
    if (state === null) {
      this.clearSelection();
      return;
    }

    if (this.urlStatesEqual(this.activeUrlState, state)) {
      return;
    }

    this.applyIdentity(state);
  }

  /**
   * Re-drives the current complete identity after a failed load (Feature 030,
   * M3). The identity is unchanged, so {@link applyUrlState} would short-circuit
   * it; retry re-enters the load path directly. A failed read is never cached,
   * so this issues a real request, while an intact summary still resolves from
   * cache and only the detail view reloads.
   */
  retryCurrentIdentity(): void {
    const state = this.activeUrlState;
    if (state === null) {
      return;
    }

    this.applyIdentity(state);
  }

  /** Cancels the pending summary/detail loads without resetting panel state. */
  cancelPendingLoads(): void {
    this.requests.cancelAll();
  }

  clearSelection(): void {
    this.requests.cancelAll();
    this.activeUrlState = null;
    this._panel.set(this.initialPanel);
  }

  /**
   * Drives a complete identity: abandons the previous identity's summary and
   * detail requests, then either reloads only the active view (same entity,
   * loaded summary) or reloads the summary first.
   */
  protected applyIdentity(state: TUrlState): void {
    const token = this.requests.beginTransition();
    this.activeUrlState = state;
    const current = this._panel();

    if (this.sameIdentity(current, state)) {
      this._panel.update(
        (s) =>
          ({
            ...this.applyUrlStateFields(s, state),
            status: 'loading',
            errorMessage: '',
          }) as TPanel,
      );
      this.loadActiveView(state, token);
      return;
    }

    this.loadSummaryAndRestore(state, token);
  }

  protected loadSummaryAndRestore(state: TUrlState, token: number): void {
    this._panel.set(
      ({
        ...this.applyUrlStateFields(this.initialPanel, state),
        status: 'loading',
      }) as TPanel,
    );

    this.requests.trackSummary(
      this.loadSummary(state)
        .pipe(
          tap((response) => {
            if (!this.requests.isCurrent(token)) {
              return;
            }

            if (!response.isSuccess || !response.data) {
              this._panel.set(this.notFoundPanel(state, response.message ?? ''));
              return;
            }

            const summary = response.data;
            this._panel.update(
              (s) =>
                ({
                  ...s,
                  ...this.applySummary(state, summary),
                  status: 'loading',
                }) as TPanel,
            );
            this.loadActiveView(state, token);
          }),
          catchError((err) => {
            if (!this.requests.isCurrent(token)) {
              return of(undefined);
            }

            if (err instanceof HttpErrorResponse && err.status === 404) {
              this._panel.set(this.notFoundPanel(state, this.extractErrorMessage(err, this.notFoundLabel)));
              return of(undefined);
            }

            this._panel.set(this.errorPanel(state, this.extractErrorMessage(err, this.errorLabel)));
            return of(undefined);
          }),
        )
        .subscribe(),
    );
  }

  protected loadActiveView(state: TUrlState, token: number): void {
    this.requests.trackDetail(this.requestActiveView(state, this.buildViewHandlers(token)));
  }

  /** Applies a panel update only while `token` still owns the panel. */
  protected applyIfCurrent(token: number, update: (state: TPanel) => TPanel): void {
    if (this.requests.isCurrent(token)) {
      this._panel.update(update);
    }
  }

  protected abstract readonly notFoundLabel: string;
  protected abstract readonly errorLabel: string;

  /** Complete-identity equality (the entity's `xDetailUrlStatesEqual` free function). */
  protected abstract urlStatesEqual(a: TUrlState | null, b: TUrlState | null): boolean;

  /** True when `state` re-selects the entity already loaded in `current` (so only the view need reload). */
  protected abstract sameIdentity(current: TPanel, state: TUrlState): boolean;

  /** Merges `state`'s identity/view/page fields (and, where applicable, its filter) into `panel`. */
  protected abstract applyUrlStateFields(panel: TPanel, state: TUrlState): TPanel;

  /** Folds a successful summary read into a partial panel update (summary plus any filter it restores). */
  protected abstract applySummary(state: TUrlState, data: TSummary): Partial<TPanel>;

  /** Reads the entity summary through its cache key, sharing reads with every other consumer of that cache. */
  protected abstract loadSummary(state: TUrlState): Observable<ApiResponse<TSummary>>;

  protected abstract notFoundPanel(state: TUrlState, message: string): TPanel;
  protected abstract errorPanel(state: TUrlState, message: string): TPanel;
  protected abstract extractErrorMessage(err: unknown, fallback: string): string;

  /** Issues the active-view read (via the entity's view loader) and returns its subscription, if any. */
  protected abstract requestActiveView(state: TUrlState, handlers: TViewHandlers): Subscription | undefined;

  /** Builds the view-loader handlers that fold each response into the panel through {@link applyIfCurrent}. */
  protected abstract buildViewHandlers(token: number): TViewHandlers;
}
