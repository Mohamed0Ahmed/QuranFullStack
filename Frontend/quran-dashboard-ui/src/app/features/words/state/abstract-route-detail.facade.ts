import { ActivatedRoute, ParamMap } from '@angular/router';
import { Subscription } from 'rxjs';
import { distinctUntilChanged, map } from 'rxjs/operators';

// The subset of a concrete detail controller that AbstractRouteDetailFacade drives
// directly. The concrete facade's own `controller` field keeps its full concrete
// type, so entity-only members (e.g. setAyahTypeCode) stay reachable there even
// though this base only calls the shared subset.
export interface RouteDetailController<TUrlState, TView, TWordView, TSurahView> {
  applyUrlState(state: TUrlState | null): void;
  clearSelection(): void;
  setView(view: TView): void;
  setWordView(wordView: TWordView): void;
  setSurahView(surahView: TSurahView): void;
  setDetailPage(page: number): void;
}

// Shared route-adapter skeleton (Feature 033 DRY) for the near-identical
// Roots/Lemmas/Stems detail facades: binds the page's ActivatedRoute query state
// to the entity's controller and delegates the shared view/page/clear methods.
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
    // Reset the active identity as well as cancelling in-flight loads: a bare
    // cancel leaves the controller's last identity set, so re-binding to the
    // SAME query params would short-circuit in applyUrlState() and strand the
    // panel in its cancelled `loading` state. Clearing forces a real reload on
    // re-entry.
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
