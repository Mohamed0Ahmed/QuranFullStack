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
import { parseDetailOverlayParams, serializeDetailFrame, serializeDetailOverlayState } from './detail-overlay-url-codec';

/**
 * History-state provenance for app-created overlay navigations. Dialog Back
 * calls browser Back only when this record proves the immediately previous
 * entry is the parent card on the same base; anything else (shared URL,
 * intervening base navigation) uses the deterministic replace fallback.
 */
interface DetailOverlayProvenance {
  readonly baseSignature: string;
  readonly parentStackHash: string;
  readonly stackHash: string;
  readonly kind: 'push' | 'restore' | 'replace' | 'seed';
}

const PROVENANCE_STATE_KEY = 'qdDetailNav';

type OverlayQueryParams = {
  [DETAIL_OVERLAY_QUERY_KEYS.frame]: readonly string[] | null;
  [DETAIL_OVERLAY_QUERY_KEYS.open]: '1' | null;
};

/**
 * URL-authoritative overlay coordinator (Feature 029, Change B). The URL is the
 * single source of truth: every mutation is a router navigation, and the state
 * signal is re-parsed from every NavigationEnd (covering browser Back/Forward).
 */
@Injectable({ providedIn: 'root' })
export class DetailOverlayHistoryService {
  private readonly router = inject(Router);
  private readonly location = inject(Location);

  private readonly _state = signal<DetailOverlayUrlState>(CLOSED_DETAIL_OVERLAY_STATE);
  private readonly _urlEpoch = signal(0);
  private readonly _capRejectionCount = signal(0);
  private started = false;

  /** Parsed overlay state of the current URL. */
  readonly state: Signal<DetailOverlayUrlState> = this._state.asReadonly();

  /** Bumps on every NavigationEnd; hrefs derived from the current URL depend on it. */
  readonly urlEpoch: Signal<number> = this._urlEpoch.asReadonly();

  /** Incremented whenever an append is refused by the eight-frame cap. */
  readonly capRejectionCount: Signal<number> = this._capRejectionCount.asReadonly();

  readonly isOpen = computed(() => this.state().visibility === 'open' && this.state().stack.length > 0);
  readonly isRetainedClosed = computed(() => this.state().visibility === 'closed' && this.state().stack.length > 0);
  readonly topFrame = computed<DetailFrame | null>(() => this.state().stack.at(-1) ?? null);

  /**
   * Starts URL synchronization. Idempotent; called by the persistent host. The
   * current URL is parsed immediately so a fresh deep link hydrates before the
   * first NavigationEnd.
   */
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

  /** Opens a new one-frame stack (entity link outside an open overlay). */
  startStack(frame: DetailFrame): void {
    this.navigate({ visibility: 'open', stack: [frame] }, { push: true, kind: 'push' });
  }

  /**
   * Appends a frame to the open stack (cross-entity link inside a detail).
   * Returns false without touching the URL when the append is a no-op push of
   * the identical top frame, or when the eight-frame cap refuses a ninth frame.
   */
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

  /** Replaces the top frame in place (tab/sub-view/page change; no new history entry). */
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

  /**
   * Dialog Back: browser Back only when owned provenance proves the immediately
   * previous history entry is the parent card on the same base; otherwise a
   * deterministic replace with the top frame removed. Never exits the app.
   */
  back(): void {
    const current = this.state();
    if (current.stack.length <= 1 || current.visibility !== 'open') {
      return;
    }

    const provenance = this.readProvenance();
    const parentHash = this.hashStack(current.stack.slice(0, -1));
    const provesParent =
      provenance !== null &&
      (provenance.kind === 'push' || provenance.kind === 'seed') &&
      provenance.parentStackHash === parentHash &&
      provenance.baseSignature === this.currentBaseSignature();

    if (provesParent) {
      this.location.back();
      return;
    }

    this.navigate({ visibility: 'open', stack: current.stack.slice(0, -1) }, { push: false, kind: 'replace' });
  }

  /** Close/Escape/backdrop: the stack stays in the URL in a closed, restorable state. */
  close(): void {
    const current = this.state();
    if (current.stack.length === 0 || current.visibility !== 'open') {
      return;
    }
    this.navigate({ visibility: 'closed', stack: current.stack }, { push: false, kind: 'replace' });
  }

  /** Restore reopens the retained stack as a history push (Back returns to the closed state). */
  restore(): void {
    const current = this.state();
    if (current.stack.length === 0 || current.visibility !== 'closed') {
      return;
    }
    this.navigate({ visibility: 'open', stack: current.stack }, { push: true, kind: 'restore' });
  }

  /**
   * Real, copyable href for an entity link. `start` links (side panels, Mushaf)
   * open a new one-frame stack; `append` links (inside an open overlay) target
   * the current stack plus the frame. The base route and its existing query
   * state are always retained.
   */
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

  /**
   * Navigates the base route underneath the overlay (ayah continuity, B7):
   *
   * - Overlay OPEN: the new base replaces the current history entry while the
   *   full stack and `qdDetailOpen=1` are kept, so the continuity step never
   *   inserts a non-entity history step between modal frames. The provenance
   *   record is re-written with kind `replace` and the new base signature —
   *   dialog Back then uses the deterministic replace fallback on the new base.
   * - Overlay CLOSED with `promoteFrame`: the source detail context is promoted
   *   to a fresh one-frame stack over the new base as a history push, so
   *   browser Back returns to the originating side panel/base entry.
   * - Overlay CLOSED without `promoteFrame`: a plain push to the base with all
   *   overlay keys stripped.
   */
  navigateBaseWithOverlay(basePath: string, baseQueryParams: Params, opts?: { promoteFrame?: DetailFrame }): void {
    const current = this.state();

    if (current.visibility === 'open' && current.stack.length > 0) {
      const target: DetailOverlayUrlState = { visibility: 'open', stack: current.stack };
      const existing = this.readProvenance();
      const provenance: DetailOverlayProvenance = {
        baseSignature: this.baseSignatureFor(basePath, baseQueryParams),
        parentStackHash: existing?.parentStackHash ?? this.hashStack(current.stack.slice(0, -1)),
        stackHash: this.hashStack(current.stack),
        kind: 'replace',
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

  /**
   * Real, copyable href with the same semantics an unmodified click on
   * {@link navigateBaseWithOverlay} would apply over the current URL: open
   * overlay → new base plus the current stack; closed with a promotable frame →
   * new base plus that one-frame stack; otherwise the bare base.
   */
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
      // Invalid/corrupted overlay params canonicalize exactly once, with
      // replace semantics so the malformed URL does not linger in history.
      this.navigate(state, { push: false, preserveProvenance: true });
      return;
    }

    this.seedDeepLinkHistoryOnce(state);
  }

  /**
   * A freshly loaded shared URL has no owned history provenance, so browser
   * Back would leave the application instead of popping the stack. Materialize
   * its prefixes once: replace the current entry with the bare base, then add
   * each stack prefix with `Location.go`, ending at the original URL. Entries
   * are marked in `history.state`, and the seeded URL is remembered per session
   * because the router's initial navigation rewrites `history.state` on reload —
   * the prefix entries themselves survive the reload, so seeding again would
   * duplicate them.
   */
  private seedDeepLinkHistoryOnce(state: DetailOverlayUrlState): void {
    if (state.stack.length === 0 || state.visibility !== 'open' || this.readProvenance() !== null) {
      return;
    }

    const baseSignature = this.currentBaseSignature();
    const originalUrl = this.location.path();
    if (this.wasSeededThisSession(originalUrl)) {
      // Reload of an already-seeded entry: restore the provenance marker the
      // router's initial navigation clobbered, without adding history entries.
      const provenance: DetailOverlayProvenance = {
        baseSignature,
        parentStackHash: this.hashStack(state.stack.slice(0, -1)),
        stackHash: this.hashStack(state.stack),
        kind: 'seed',
      };
      this.location.replaceState(originalUrl, '', { [PROVENANCE_STATE_KEY]: provenance });
      return;
    }
    this.markSeededThisSession(originalUrl);
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
      ? this.withUpdatedStackHash(this.readProvenance(), target)
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

  /**
   * URL tree for an explicit base (path + its complete own query params) plus
   * the overlay keys of `state`. Unlike {@link buildUrlTree} this does not merge
   * with the current URL: an ayah destination fully defines its base query.
   */
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

  /** Base signature of an explicit destination: its URL with no overlay keys. */
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

  private wasSeededThisSession(url: string): boolean {
    try {
      return sessionStorage.getItem(`${PROVENANCE_STATE_KEY}:seeded:${url}`) === '1';
    } catch {
      return false;
    }
  }

  private markSeededThisSession(url: string): void {
    try {
      sessionStorage.setItem(`${PROVENANCE_STATE_KEY}:seeded:${url}`, '1');
    } catch {
      // Session storage unavailable: seeding stays correct, only reload dedup is lost.
    }
  }

  private readProvenance(): DetailOverlayProvenance | null {
    const historyState = this.location.getState();
    if (historyState === null || typeof historyState !== 'object') {
      return null;
    }
    const record = (historyState as Record<string, unknown>)[PROVENANCE_STATE_KEY];
    if (record === null || typeof record !== 'object') {
      return null;
    }
    const candidate = record as Partial<DetailOverlayProvenance>;
    return typeof candidate.baseSignature === 'string' &&
      typeof candidate.parentStackHash === 'string' &&
      typeof candidate.stackHash === 'string' &&
      (candidate.kind === 'push' || candidate.kind === 'restore' || candidate.kind === 'replace' || candidate.kind === 'seed')
      ? (candidate as DetailOverlayProvenance)
      : null;
  }

  private hashStack(stack: readonly DetailFrame[]): string {
    return stack.map(serializeDetailFrame).join('|');
  }

  /** The current URL with overlay-owned keys removed: the base an overlay sits on. */
  private currentBaseSignature(): string {
    return this.router.serializeUrl(this.buildUrlTree(CLOSED_DETAIL_OVERLAY_STATE));
  }
}
