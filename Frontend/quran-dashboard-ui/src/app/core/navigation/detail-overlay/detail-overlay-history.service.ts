import { Location } from '@angular/common';
import { Injectable, Signal, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Params, Router, UrlTree } from '@angular/router';

import {
  CLOSED_DETAIL_OVERLAY_STATE,
  DETAIL_OVERLAY_MAX_FRAMES,
  DETAIL_OVERLAY_QUERY_KEYS,
  DetailFrame,
  DetailOverlayUrlState,
  detailFramesEqual,
} from './detail-overlay.models';
import { parseDetailOverlayParams, serializeDetailOverlayState } from './detail-overlay-url-codec';
import {
  DetailOverlayProvenance,
  PROVENANCE_STATE_KEY,
  hashDetailStack,
  readDetailOverlayProvenance,
} from './detail-overlay-provenance';

type OverlayQueryParams = {
  [DETAIL_OVERLAY_QUERY_KEYS.frame]: readonly string[] | null;
  [DETAIL_OVERLAY_QUERY_KEYS.open]: '1' | null;
};

@Injectable({ providedIn: 'root' })
export class DetailOverlayHistoryService {
  private readonly router = inject(Router);
  private readonly location = inject(Location);

  private readonly _state = signal<DetailOverlayUrlState>(CLOSED_DETAIL_OVERLAY_STATE);
  private readonly _urlEpoch = signal(0);
  private readonly _capRejectionCount = signal(0);
  private started = false;

  readonly state: Signal<DetailOverlayUrlState> = this._state.asReadonly();

  // href computeds read this to recompute after each NavigationEnd.
  readonly urlEpoch: Signal<number> = this._urlEpoch.asReadonly();

  readonly capRejectionCount: Signal<number> = this._capRejectionCount.asReadonly();

  readonly isOpen = computed(() => this.state().visibility === 'open' && this.state().stack.length > 0);
  readonly isRetainedClosed = computed(() => this.state().visibility === 'closed' && this.state().stack.length > 0);
  readonly topFrame = computed<DetailFrame | null>(() => this.state().stack.at(-1) ?? null);

  start(): void {
    if (this.started) {
      return;
    }
    this.started = true;

    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.syncFromUrl();
      }
    });
    this.syncFromUrl();
  }

  startStack(frame: DetailFrame): void {
    this.navigate({ visibility: 'open', stack: [frame] }, { push: true, kind: 'push' });
  }

  appendFrame(frame: DetailFrame): boolean {
    const current = this.state();
    if (current.stack.length === 0 || current.visibility !== 'open') {
      this.startStack(frame);
      return true;
    }

    const top = current.stack[current.stack.length - 1];
    if (detailFramesEqual(top, frame)) {
      return false;
    }
    if (current.stack.length >= DETAIL_OVERLAY_MAX_FRAMES) {
      this._capRejectionCount.update((count) => count + 1);
      return false;
    }

    this.navigate({ visibility: 'open', stack: [...current.stack, frame] }, { push: true, kind: 'push' });
    return true;
  }

  replaceTopFrame(frame: DetailFrame): void {
    const current = this.state();
    if (current.stack.length === 0 || current.visibility !== 'open') {
      return;
    }
    const top = current.stack[current.stack.length - 1];
    if (detailFramesEqual(top, frame)) {
      return;
    }

    const stack = [...current.stack.slice(0, -1), frame];
    this.navigate({ visibility: 'open', stack }, { push: false, preserveProvenance: true });
  }

  back(): void {
    const current = this.state();
    if (current.stack.length <= 1 || current.visibility !== 'open') {
      return;
    }

    const provenance = this.currentEntryProvenance(current);
    if (this.provenanceProvesParent(provenance, current)) {
      this.location.back();
      return;
    }

    this.navigate({ visibility: 'open', stack: current.stack.slice(0, -1) }, { push: false, kind: 'replace' });
  }

  close(): void {
    const current = this.state();
    if (current.stack.length === 0 || current.visibility !== 'open') {
      return;
    }
    this.navigate({ visibility: 'closed', stack: current.stack }, { push: false, kind: 'replace' });
  }

  restore(): void {
    const current = this.state();
    if (current.stack.length === 0 || current.visibility !== 'closed') {
      return;
    }
    this.navigate({ visibility: 'open', stack: current.stack }, { push: true, kind: 'restore' });
  }

  buildFrameHref(frame: DetailFrame, mode: 'start' | 'append'): string {
    const current = this.state();
    const stack =
      mode === 'append' && current.visibility === 'open' && current.stack.length > 0
        ? current.stack.length >= DETAIL_OVERLAY_MAX_FRAMES || detailFramesEqual(current.stack[current.stack.length - 1], frame)
          ? current.stack
          : [...current.stack, frame]
        : [frame];
    return this.router.serializeUrl(this.buildUrlTree({ visibility: 'open', stack }));
  }

  navigateBaseWithOverlay(basePath: string, baseQueryParams: Params, opts?: { promoteFrame?: DetailFrame }): void {
    const current = this.state();

    if (current.visibility === 'open' && current.stack.length > 0) {
      const target: DetailOverlayUrlState = { visibility: 'open', stack: current.stack };
      const existing = this.ensureBaseTransitionProvenance(current);
      const provenance: DetailOverlayProvenance = {
        baseSignature: this.baseSignatureFor(basePath, baseQueryParams),
        parentStackHash: existing?.parentStackHash ?? this.hashStack(current.stack.slice(0, -1)),
        stackHash: this.hashStack(current.stack),
        kind: existing?.kind ?? 'replace',
      };
      void this.router.navigateByUrl(this.buildBaseUrlTree(basePath, baseQueryParams, target), {
        replaceUrl: true,
        state: { [PROVENANCE_STATE_KEY]: provenance },
      });
      return;
    }

    const promoteFrame = opts?.promoteFrame;
    if (promoteFrame !== undefined) {
      const target: DetailOverlayUrlState = { visibility: 'open', stack: [promoteFrame] };
      const provenance: DetailOverlayProvenance = {
        baseSignature: this.baseSignatureFor(basePath, baseQueryParams),
        parentStackHash: this.hashStack([]),
        stackHash: this.hashStack(target.stack),
        kind: 'push',
      };
      void this.router.navigateByUrl(this.buildBaseUrlTree(basePath, baseQueryParams, target), {
        state: { [PROVENANCE_STATE_KEY]: provenance },
      });
      return;
    }

    void this.router.navigateByUrl(this.buildBaseUrlTree(basePath, baseQueryParams, CLOSED_DETAIL_OVERLAY_STATE));
  }

  buildBaseWithOverlayHref(basePath: string, baseQueryParams: Params, opts?: { promoteFrame?: DetailFrame }): string {
    const current = this.state();
    const stack =
      current.visibility === 'open' && current.stack.length > 0
        ? current.stack
        : opts?.promoteFrame !== undefined
          ? [opts.promoteFrame]
          : [];
    const target: DetailOverlayUrlState = stack.length > 0 ? { visibility: 'open', stack } : CLOSED_DETAIL_OVERLAY_STATE;
    return this.router.serializeUrl(this.buildBaseUrlTree(basePath, baseQueryParams, target));
  }

  private syncFromUrl(): void {
    const paramMap = this.router.routerState.snapshot.root.queryParamMap;
    const { state, isCanonical } = parseDetailOverlayParams(
      paramMap.getAll(DETAIL_OVERLAY_QUERY_KEYS.frame),
      paramMap.get(DETAIL_OVERLAY_QUERY_KEYS.open),
    );

    this._state.set(state);
    this._urlEpoch.update((epoch) => epoch + 1);

    if (!isCanonical) {
      // Canonicalize corrupted params once with replace semantics so the bad URL doesn't linger in history.
      this.navigate(state, { push: false, preserveProvenance: true });
      return;
    }

    this.reconcileHistoryOwnership(state);
  }

  // Provenance lives in history.state (persists across reload/popstate) and IS the entry identity;
  // missing/mismatched proof fails closed and re-seeds.
  private reconcileHistoryOwnership(state: DetailOverlayUrlState): void {
    if (state.stack.length === 0 || state.visibility !== 'open') {
      return;
    }

    if (this.currentEntryProvenance(state) !== null) {
      return;
    }

    this.seedChain(state);
  }

  private seedChain(state: DetailOverlayUrlState): void {
    const baseSignature = this.currentBaseSignature();
    const baseUrl = this.router.serializeUrl(this.buildUrlTree(CLOSED_DETAIL_OVERLAY_STATE));
    this.location.replaceState(baseUrl);

    for (let depth = 1; depth <= state.stack.length; depth += 1) {
      const prefix = state.stack.slice(0, depth);
      const url = this.router.serializeUrl(this.buildUrlTree({ visibility: 'open', stack: prefix }));
      const provenance: DetailOverlayProvenance = {
        baseSignature,
        parentStackHash: this.hashStack(prefix.slice(0, -1)),
        stackHash: this.hashStack(prefix),
        kind: 'seed',
      };
      this.location.go(url, '', { [PROVENANCE_STATE_KEY]: provenance });
    }
  }

  private ensureBaseTransitionProvenance(state: DetailOverlayUrlState): DetailOverlayProvenance | null {
    let provenance = this.currentEntryProvenance(state);
    if (provenance !== null && (state.stack.length === 1 || this.provenanceProvesParent(provenance, state))) {
      return provenance;
    }

    this.seedChain(state);
    provenance = this.currentEntryProvenance(state);
    return provenance;
  }

  private provenanceProvesParent(
    provenance: DetailOverlayProvenance | null,
    state: DetailOverlayUrlState,
  ): boolean {
    return (
      provenance !== null &&
      (provenance.kind === 'push' || provenance.kind === 'seed') &&
      provenance.parentStackHash === this.hashStack(state.stack.slice(0, -1))
    );
  }

  private navigate(
    target: DetailOverlayUrlState,
    options: { push: boolean; kind?: DetailOverlayProvenance['kind']; preserveProvenance?: boolean },
  ): void {
    const current = this.state();
    const serialized = serializeDetailOverlayState(target);
    const queryParams: OverlayQueryParams = {
      [DETAIL_OVERLAY_QUERY_KEYS.frame]: serialized.frames.length > 0 ? serialized.frames : null,
      [DETAIL_OVERLAY_QUERY_KEYS.open]: serialized.open,
    };

    const provenance: DetailOverlayProvenance | null = options.preserveProvenance
      ? this.withUpdatedStackHash(this.currentEntryProvenance(current), target)
      : options.kind
        ? {
            baseSignature: this.currentBaseSignature(),
            parentStackHash: this.hashStack(current.stack),
            stackHash: this.hashStack(target.stack),
            kind: options.kind,
          }
        : null;

    void this.router.navigate([], {
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: !options.push,
      state: provenance === null ? undefined : { [PROVENANCE_STATE_KEY]: provenance },
    });
  }

  private withUpdatedStackHash(
    provenance: DetailOverlayProvenance | null,
    target: DetailOverlayUrlState,
  ): DetailOverlayProvenance | null {
    if (provenance === null) {
      return null;
    }
    return { ...provenance, stackHash: this.hashStack(target.stack) };
  }

  // Unlike buildUrlTree, this does NOT merge current-URL query: the destination fully defines its own base query.
  private buildBaseUrlTree(basePath: string, baseQueryParams: Params, state: DetailOverlayUrlState): UrlTree {
    const serialized = serializeDetailOverlayState(state);
    const queryParams: Params = { ...baseQueryParams };
    if (serialized.frames.length > 0) {
      queryParams[DETAIL_OVERLAY_QUERY_KEYS.frame] = [...serialized.frames];
    }
    if (serialized.open !== null) {
      queryParams[DETAIL_OVERLAY_QUERY_KEYS.open] = serialized.open;
    }
    return this.router.createUrlTree([basePath], { queryParams });
  }

  private baseSignatureFor(basePath: string, baseQueryParams: Params): string {
    return this.router.serializeUrl(this.buildBaseUrlTree(basePath, baseQueryParams, CLOSED_DETAIL_OVERLAY_STATE));
  }

  private buildUrlTree(state: DetailOverlayUrlState): UrlTree {
    const serialized = serializeDetailOverlayState(state);
    return this.router.createUrlTree([], {
      queryParams: {
        [DETAIL_OVERLAY_QUERY_KEYS.frame]: serialized.frames.length > 0 ? serialized.frames : null,
        [DETAIL_OVERLAY_QUERY_KEYS.open]: serialized.open,
      },
      queryParamsHandling: 'merge',
    });
  }

  private readProvenance(): DetailOverlayProvenance | null {
    return readDetailOverlayProvenance(this.location);
  }

  private currentEntryProvenance(state: DetailOverlayUrlState): DetailOverlayProvenance | null {
    const provenance = this.readProvenance();
    return provenance !== null &&
      provenance.baseSignature === this.currentBaseSignature() &&
      provenance.stackHash === this.hashStack(state.stack)
      ? provenance
      : null;
  }

  private hashStack(stack: readonly DetailFrame[]): string {
    return hashDetailStack(stack);
  }

  private currentBaseSignature(): string {
    return this.router.serializeUrl(this.buildUrlTree(CLOSED_DETAIL_OVERLAY_STATE));
  }
}
