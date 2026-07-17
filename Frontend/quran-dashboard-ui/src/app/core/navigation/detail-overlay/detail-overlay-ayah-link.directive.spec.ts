import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Location } from '@angular/common';
import { provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DetailOverlayAyahLinkDirective, DetailOverlayBaseTarget } from './detail-overlay-ayah-link.directive';
import { DetailOverlayHistoryService } from './detail-overlay-history.service';
import { RootDetailFrame } from './detail-overlay.models';

const rootFrame: RootDetailFrame = {
  kind: 'root',
  id: 999,
  view: 'ayahs',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
};

const ROOT_SERIALIZED = 'v1~root~999~ayahs~simple~mentioned~1';

const mushafTarget: DetailOverlayBaseTarget = {
  basePath: '/dashboard/mushaf',
  queryParams: { page: '92', ayah: '4:57', focusAyah: '4:57', panel: 'ayah' },
};

@Component({ standalone: true, template: '' })
class BlankPageComponent {}

@Component({
  standalone: true,
  imports: [DetailOverlayAyahLinkDirective],
  template: `
    <a [qdAyahOverlayLink]="target" [qdAyahLinkParentFrame]="parentFrame" data-testid="ayah-link">SYNTH_AYAH_LINK</a>
  `,
})
class AyahLinkHostComponent {
  target = mushafTarget;
  parentFrame: RootDetailFrame | null = null;
}

describe('DetailOverlayAyahLinkDirective', () => {
  let router: Router;
  let location: Location;
  let service: DetailOverlayHistoryService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: '**', component: BlankPageComponent }]), provideLocationMocks()],
    });
    router = TestBed.inject(Router);
    location = TestBed.inject(Location);
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

  async function createHost(parentFrame: RootDetailFrame | null = null) {
    await router.navigateByUrl('/dashboard/words/roots?root=999&view=ayahs');
    service.start();
    const fixture = TestBed.createComponent(AyahLinkHostComponent);
    fixture.componentInstance.parentFrame = parentFrame;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  function link(fixture: { nativeElement: HTMLElement }): HTMLAnchorElement {
    return fixture.nativeElement.querySelector('[data-testid="ayah-link"]') as HTMLAnchorElement;
  }

  it('renders a bare destination href when the overlay is closed and no parent frame exists', async () => {
    const fixture = await createHost();
    const href = link(fixture).getAttribute('href')!;

    expect(href).toContain('/dashboard/mushaf');
    expect(href).toContain('page=92');
    expect(href).toContain('panel=ayah');
    expect(href).not.toContain('qdDetail');
    // The destination base is explicit: the source page's own query never rides along.
    expect(href).not.toContain('root=999');
  });

  it('renders a promoted one-frame href when the overlay is closed and a parent frame exists', async () => {
    const fixture = await createHost(rootFrame);
    const href = link(fixture).getAttribute('href')!;

    expect(href).toContain('/dashboard/mushaf');
    expect(href).toContain(`qdDetail=${encodeURIComponent(ROOT_SERIALIZED)}`);
    expect(href).toContain('qdDetailOpen=1');
  });

  it('renders the current open stack in the href regardless of the parent frame (urlEpoch reactive)', async () => {
    const fixture = await createHost(rootFrame);
    service.startStack({ ...rootFrame, id: 111 });
    await settle();
    fixture.detectChanges();

    const href = link(fixture).getAttribute('href')!;
    expect(href).toContain(encodeURIComponent('v1~root~111~ayahs~simple~mentioned~1'));
    expect(href).not.toContain(encodeURIComponent(ROOT_SERIALIZED));
    expect(href).toContain('qdDetailOpen=1');
  });

  it('intercepts an unmodified primary click and navigates the base with overlay continuity', async () => {
    const fixture = await createHost(rootFrame);
    const navigateSpy = vi.spyOn(service, 'navigateBaseWithOverlay');

    const event = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 });
    link(fixture).dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(navigateSpy).toHaveBeenCalledWith('/dashboard/mushaf', mushafTarget.queryParams, {
      promoteFrame: rootFrame,
    });
  });

  it('passes no promote frame on click when the parent frame is null', async () => {
    const fixture = await createHost(null);
    const navigateSpy = vi.spyOn(service, 'navigateBaseWithOverlay');

    link(fixture).dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 }));

    expect(navigateSpy).toHaveBeenCalledWith('/dashboard/mushaf', mushafTarget.queryParams, {
      promoteFrame: undefined,
    });
  });

  it('lands on the destination with the promoted stack after a real click', async () => {
    const fixture = await createHost(rootFrame);

    link(fixture).dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 }));
    await settle();

    expect(location.path()).toContain('/dashboard/mushaf');
    expect(location.path()).toContain(`qdDetail=${encodeURIComponent(ROOT_SERIALIZED)}`);
    expect(location.path()).toContain('qdDetailOpen=1');
    expect(service.isOpen()).toBe(true);
  });

  it.each([
    ['ctrl', { ctrlKey: true }],
    ['meta', { metaKey: true }],
    ['shift', { shiftKey: true }],
    ['alt', { altKey: true }],
    ['middle button', { button: 1 }],
  ] as const)('leaves a %s click to the browser', async (_label, init) => {
    const fixture = await createHost(rootFrame);
    const navigateSpy = vi.spyOn(service, 'navigateBaseWithOverlay');

    const event = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0, ...init });
    link(fixture).dispatchEvent(event);

    expect(event.defaultPrevented).toBe(false);
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('does not intercept the context menu', async () => {
    const fixture = await createHost(rootFrame);
    const navigateSpy = vi.spyOn(service, 'navigateBaseWithOverlay');

    const event = new MouseEvent('contextmenu', { bubbles: true, cancelable: true, button: 2 });
    link(fixture).dispatchEvent(event);

    expect(event.defaultPrevented).toBe(false);
    expect(navigateSpy).not.toHaveBeenCalled();
  });
});
