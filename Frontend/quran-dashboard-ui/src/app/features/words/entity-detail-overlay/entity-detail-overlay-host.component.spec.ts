import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { App } from '../../../app';
import { DetailOverlayHistoryService } from '../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { RootDetailFrame } from '../../../core/navigation/detail-overlay/detail-overlay.models';
import { ENTITY_DETAIL_KIND_TITLES } from './entity-detail-overlay.labels';

const rootFrame: RootDetailFrame = {
  kind: 'root',
  id: 999,
  view: 'words',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
};

@Component({ standalone: true, template: '' })
class BlankPageComponent {}

describe('EntityDetailOverlayHostComponent (composition root)', () => {
  let router: Router;
  let service: DetailOverlayHistoryService;

  beforeEach(() => {
    // jsdom lacks matchMedia; the app shell's theme service reads it on init.
    if (typeof window.matchMedia !== 'function') {
      Object.defineProperty(window, 'matchMedia', {
        configurable: true,
        writable: true,
        value: () => ({
          matches: false,
          addEventListener: () => undefined,
          removeEventListener: () => undefined,
          addListener: () => undefined,
          removeListener: () => undefined,
        }),
      });
    }

    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: '**', component: BlankPageComponent }]),
        provideLocationMocks(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    router = TestBed.inject(Router);
    service = TestBed.inject(DetailOverlayHistoryService);
    sessionStorage.clear();
    router.initialNavigation();
  });

  async function settle(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 10));
    await vi.waitFor(() => {
      if (router.getCurrentNavigation() !== null) {
        throw new Error('navigation in flight');
      }
    });
    await new Promise((resolve) => setTimeout(resolve, 0));
  }

  async function createApp() {
    await router.navigateByUrl('/dashboard/words/roots');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('mounts the persistent host beside the shell with no dialog while the URL has no overlay', async () => {
    const fixture = await createApp();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('qd-entity-detail-overlay-host')).not.toBeNull();
    expect(root.querySelector('[data-testid="detail-modal-shell"]')).toBeNull();
    expect(root.querySelector('qd-app-shell')!.hasAttribute('inert')).toBe(false);
  });

  it('opens the dialog with the entity-kind title and makes only the app shell inert', async () => {
    const fixture = await createApp();
    service.startStack(rootFrame);
    await settle();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const dialog = root.querySelector('[data-testid="detail-modal-shell"]')!;
    expect(dialog.textContent).toContain(ENTITY_DETAIL_KIND_TITLES.root);

    const shell = root.querySelector('qd-app-shell')!;
    expect(shell.hasAttribute('inert')).toBe(true);
    expect(shell.getAttribute('aria-hidden')).toBe('true');
    expect(dialog.closest('[inert]')).toBeNull();
  });

  it('Escape closes into the restore control and restore reopens the same stack', async () => {
    const fixture = await createApp();
    service.startStack(rootFrame);
    await settle();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    root
      .querySelector('[data-testid="detail-modal-shell"]')!
      .dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await settle();
    fixture.detectChanges();

    expect(root.querySelector('[data-testid="detail-modal-shell"]')).toBeNull();
    const restore = root.querySelector('[data-testid="detail-modal-restore"]') as HTMLButtonElement;
    expect(restore.getAttribute('aria-label')).toContain(ENTITY_DETAIL_KIND_TITLES.root);
    expect(root.querySelector('qd-app-shell')!.hasAttribute('inert')).toBe(false);

    restore.click();
    await settle();
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="detail-modal-shell"]')).not.toBeNull();
    expect(service.isOpen()).toBe(true);
  });

  it('dialog Back pops one frame while browser semantics stay URL-authoritative', async () => {
    const fixture = await createApp();
    service.startStack(rootFrame);
    await settle();
    service.appendFrame({ ...rootFrame, kind: 'root', id: 1000 });
    await settle();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    (root.querySelector('[data-testid="detail-modal-back"]') as HTMLButtonElement).click();
    await settle();
    fixture.detectChanges();

    expect(service.state().stack).toHaveLength(1);
    expect(root.querySelector('[data-testid="detail-modal-back"]')).toBeNull();
    expect(root.querySelector('[data-testid="detail-modal-shell"]')).not.toBeNull();
  });

  it('announces the Arabic cap status when a ninth append is refused', async () => {
    const fixture = await createApp();
    service.startStack(rootFrame);
    await settle();
    for (let id = 1; id <= 7; id += 1) {
      service.appendFrame({ ...rootFrame, id });
      await settle();
    }
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('[data-testid="detail-modal-live-status"]')).toBeNull();

    service.appendFrame({ ...rootFrame, id: 4242 });
    await settle();
    fixture.detectChanges();

    const status = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="detail-modal-live-status"]');
    expect(status?.textContent).toContain('ثماني');
    expect(service.state().stack).toHaveLength(8);
  });
});
