import { ActivatedRoute, ParamMap } from '@angular/router';
import { Subscription } from 'rxjs';
import { distinctUntilChanged, map } from 'rxjs/operators';

export interface RouteDetailController<TUrlState, TView, TWordView, TSurahView> {
  applyUrlState(state: TUrlState | null): void;
  clearSelection(): void;
  setView(view: TView): void;
  setWordView(wordView: TWordView): void;
  setSurahView(surahView: TSurahView): void;
  setDetailPage(page: number): void;
}

export abstract class AbstractRouteDetailFacade<TUrlState, TView, TWordView, TSurahView> {
  protected routeSub?: Subscription;

  protected abstract readonly controller: RouteDetailController<TUrlState, TView, TWordView, TSurahView>;

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();

    this.routeSub = route.queryParamMap
      .pipe(
        map((params) => this.toPanelUrlState(params)),
        distinctUntilChanged((a, b) => this.urlStatesEqual(a, b)),
      )
      .subscribe((state) => this.controller.applyUrlState(state));
  }

  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.routeSub = undefined;
    // Clear the identity, not just cancel in-flight loads: else re-binding the SAME query params
    // short-circuits in applyUrlState() and strands the panel in a cancelled `loading` state.
    this.controller.clearSelection();
  }

  clearSelection(): void {
    this.controller.clearSelection();
  }

  setView(view: TView): void {
    this.controller.setView(view);
  }

  setWordView(wordView: TWordView): void {
    this.controller.setWordView(wordView);
  }

  setSurahView(surahView: TSurahView): void {
    this.controller.setSurahView(surahView);
  }

  setDetailPage(page: number): void {
    this.controller.setDetailPage(page);
  }

  protected abstract urlStatesEqual(a: TUrlState | null, b: TUrlState | null): boolean;

  protected abstract toPanelUrlState(params: ParamMap): TUrlState | null;
}
