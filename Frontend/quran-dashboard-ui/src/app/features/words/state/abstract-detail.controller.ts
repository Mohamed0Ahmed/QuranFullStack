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

  // initialPanel comes via constructor, not an abstract getter: TS forbids reading an abstract
  // member during base-class construction.
  protected constructor(protected readonly initialPanel: TPanel) {
    this._panel = signal<TPanel>(initialPanel);
    this.panelState = computed(() => this._panel());
  }

  ngOnDestroy(): void {
    this.cancelPendingLoads();
  }

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

  protected abstract sameIdentity(current: TPanel, state: TUrlState): boolean;

  protected abstract applyUrlStateFields(panel: TPanel, state: TUrlState): TPanel;

  protected abstract applySummary(state: TUrlState, data: TSummary): Partial<TPanel>;

  protected abstract loadSummary(state: TUrlState): Observable<ApiResponse<TSummary>>;

  protected abstract notFoundPanel(state: TUrlState, message: string): TPanel;
  protected abstract errorPanel(state: TUrlState, message: string): TPanel;
  protected abstract extractErrorMessage(err: unknown, fallback: string): string;

  protected abstract requestActiveView(state: TUrlState, handlers: TViewHandlers): Subscription | undefined;

  protected abstract buildViewHandlers(token: number): TViewHandlers;
}
