/**
 * Keeps a table header aligned with body rows when the body shows a vertical
 * scrollbar. Measures `offsetWidth - clientWidth` on the scroll container and
 * writes the size to a CSS variable consumed by the header gutter spacer.
 *
 * The body element is swapped on every loading transition (the loading skeleton
 * is replaced by the scroll viewport, and back), so the sync re-resolves the
 * body each run and re-points the resize observer at the current element. A
 * mutation observer on the stable scope detects those swaps; both observers are
 * coalesced through one `requestAnimationFrame` to avoid layout thrashing while
 * virtual-scroll rows recycle.
 */
export function syncTableScrollbarGutter(
  host: HTMLElement,
  cssVariable: string,
  bodySelector: string,
  scopeSelector: string,
): () => void {
  const scope = host.querySelector(scopeSelector) as HTMLElement | null;
  if (!scope) {
    return () => undefined;
  }

  let rafId: number | null = null;
  let observedBody: HTMLElement | null = null;
  let resizeObserver: ResizeObserver | undefined;

  const syncNow = (): void => {
    const body = host.querySelector(bodySelector) as HTMLElement | null;

    // Re-point the resize observer when the body element is swapped (loading
    // skeleton ⇄ scroll viewport) so we keep measuring the live scroll container.
    if (resizeObserver && body !== observedBody) {
      if (observedBody) {
        resizeObserver.unobserve(observedBody);
      }
      if (body) {
        resizeObserver.observe(body);
      }
      observedBody = body;
    }

    const gutter = body ? Math.max(0, body.offsetWidth - body.clientWidth) : 0;
    scope.style.setProperty(cssVariable, `${gutter}px`);
  };

  const scheduleSync = (): void => {
    if (rafId !== null) {
      return;
    }
    if (typeof requestAnimationFrame !== 'function') {
      syncNow();
      return;
    }
    rafId = requestAnimationFrame(() => {
      rafId = null;
      syncNow();
    });
  };

  const cancelScheduled = (): void => {
    if (rafId !== null && typeof cancelAnimationFrame === 'function') {
      cancelAnimationFrame(rafId);
    }
    rafId = null;
  };

  // jsdom / SSR lack ResizeObserver and have no layout to measure: set the
  // initial value once and stop (mirrors the tables' virtual-scroll guard).
  if (typeof ResizeObserver === 'undefined') {
    syncNow();
    return cancelScheduled;
  }

  resizeObserver = new ResizeObserver(scheduleSync);
  syncNow(); // measures and points the resize observer at the current body

  // Watch the stable scope so a body swap (loading → loaded) re-triggers a sync.
  const mutationObserver = new MutationObserver(scheduleSync);
  mutationObserver.observe(scope, { childList: true, subtree: true });

  return () => {
    cancelScheduled();
    resizeObserver?.disconnect();
    mutationObserver.disconnect();
  };
}
