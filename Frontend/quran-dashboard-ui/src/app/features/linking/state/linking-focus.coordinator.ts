import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';

import { LinkingFocusOrigin, LinkingFocusOriginKind } from '../models/linking-focus-origin.models';

@Injectable({ providedIn: 'root' })
export class LinkingFocusCoordinator {
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly originSignal = signal<LinkingFocusOrigin | null>(null);

  readonly origin = this.originSignal.asReadonly();

  capture(kind: LinkingFocusOriginKind): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    const active = this.document.activeElement;
    const invoker = active instanceof HTMLElement && active !== this.document.body ? active : null;
    this.originSignal.set({
      kind: originKindFor(invoker, kind),
      invoker,
      fallbackSelector: fallbackSelectorFor(invoker),
    });
  }

  focusAfterRender(target: () => HTMLElement | null, fallback: () => HTMLElement | null = () => null): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    setTimeout(() => {
      const preferred = target();
      if (preferred?.isConnected) {
        preferred.focus();
        return;
      }
      fallback()?.focus();
    }, 0);
  }

  restore(fallback: () => HTMLElement | null = () => null): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    const origin = this.originSignal();
    this.originSignal.set(null);
    setTimeout(() => {
      if (origin?.invoker?.isConnected) {
        origin.invoker.focus();
        return;
      }
      const selectorTarget = origin?.fallbackSelector
        ? this.document.querySelector<HTMLElement>(origin.fallbackSelector)
        : null;
      if (selectorTarget?.isConnected) {
        selectorTarget.focus();
        return;
      }
      fallback()?.focus();
    }, 0);
  }
}

function fallbackSelectorFor(invoker: HTMLElement | null): string | null {
  const sourceKey = invoker?.dataset['linkingSourceAction'];
  if (sourceKey) {
    return `[data-linking-source-action="${CSS.escape(sourceKey)}"]`;
  }
  const editorSourceKey = invoker?.dataset['linkingEditorSourceKey'];
  if (editorSourceKey) {
    return `[data-linking-editor-source-key="${CSS.escape(editorSourceKey)}"]`;
  }
  return invoker?.classList.contains('qd-navbar__linking-trigger') ? '.qd-navbar__linking-trigger' : null;
}

function originKindFor(invoker: HTMLElement | null, fallback: LinkingFocusOriginKind): LinkingFocusOriginKind {
  if (invoker?.closest('[data-testid^="detail-"]') !== null) {
    return 'retained-entity-overlay';
  }
  if (invoker?.closest('.quran-source-linking-actions') !== null) {
    return 'inline-source-action';
  }
  return fallback;
}
