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

  if (typeof ResizeObserver === 'undefined') {
    syncNow();
    return cancelScheduled;
  }

  resizeObserver = new ResizeObserver(scheduleSync);
  syncNow();

  const mutationObserver = new MutationObserver(scheduleSync);
  mutationObserver.observe(scope, { childList: true });

  return () => {
    cancelScheduled();
    resizeObserver?.disconnect();
    mutationObserver.disconnect();
  };
}
