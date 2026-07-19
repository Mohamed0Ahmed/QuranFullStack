import { Injectable, OnDestroy, Signal, WritableSignal, computed, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subscription, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { DetailRequestLifecycle } from './detail-request-lifecycle';

export interface DetailPanelStateBase {
  status: 'idle' | 'loading' | 'success' | 'empty' | 'notFound' | 'error';
  errorMessage: string;
}

// Shared route-independent detail controller skeleton (Feature 033 DRY),
// extracted from the near-identical Roots/Lemmas/Stems controllers. Every
// complete-identity transition abandons BOTH the summary and the detail request
// and opens a new generation, so a late response from the previously selected
// identity can never overwrite this one (see DetailRequestLifecycle). Entity-
// specific selection methods stay on the concrete controller: hoisting them
// would trade real duplication for accidental coupling.
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

  // initialPanel comes through the constructor, not an abstract getter: TS rejects
  // reading an abstract member during base-class construction (and the subclass's
  // own field initializers have not run yet either), so the concrete controller
  // passes its module-level INITIAL_PANEL up through super().
  protected constructor(protected readonly initialPanel: TPanel) {
    this._panel = signal<TPanel>(initialPanel);
    this.panelState = computed(() => this._panel());
  }

  ngOnDestroy(): void {
    this.cancelPendingLoads();
  }

  // Identical states short-circuit, leaving any in-flight load for that identity
  // alone (they are not cancelled).
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

  // The identity is unchanged, so applyUrlState() would short-circuit it; retry
  // re-enters the load path directly. A failed read is never cached, so this
  // issues a real request, while an intact summary still resolves from cache and
  // only the detail view reloads.
  retryCurrentIdentity(): void {
    const state = this.activeUrlState;
    if (state === null) {
      return;
    }

    this.applyIdentity(state);
  }

  cancelPendingLoads(): void {
    this.requests.cancelAll();
  }

  clearSelection(): void {
    this.requests.cancelAll();
    this.activeUrlState = null;
    this._panel.set(this.initialPanel);
  }

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

  protected applyIfCurrent(token: number, update: (state: TPanel) => TPanel): void {
    if (this.requests.isCurrent(token)) {
      this._panel.update(update);
    }
  }

  protected abstract readonly notFoundLabel: string;
  protected abstract readonly errorLabel: string;

  protected abstract urlStatesEqual(a: TUrlState | null, b: TUrlState | null): boolean;

  // True when `state` re-selects the entity already loaded in `current`, so only
  // the view need reload (weaker than urlStatesEqual's complete-identity check).
  protected abstract sameIdentity(current: TPanel, state: TUrlState): boolean;

  protected abstract applyUrlStateFields(panel: TPanel, state: TUrlState): TPanel;

  protected abstract applySummary(state: TUrlState, data: TSummary): Partial<TPanel>;

  // Reads the summary through its shared cache key, deduping with every other
  // consumer of that cache (side panel + overlay).
  protected abstract loadSummary(state: TUrlState): Observable<ApiResponse<TSummary>>;

  protected abstract notFoundPanel(state: TUrlState, message: string): TPanel;
  protected abstract errorPanel(state: TUrlState, message: string): TPanel;
  protected abstract extractErrorMessage(err: unknown, fallback: string): string;

  protected abstract requestActiveView(state: TUrlState, handlers: TViewHandlers): Subscription | undefined;

  protected abstract buildViewHandlers(token: number): TViewHandlers;
}
