import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Location } from '@angular/common';
import { provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DetailOverlayHistoryService } from './detail-overlay-history.service';
import { DetailOverlayLinkDirective } from './detail-overlay-link.directive';
import { RootDetailFrame } from './detail-overlay.models';

const rootFrame: RootDetailFrame = {
  kind: 'root',
  id: 999,
  view: 'words',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
};

const ROOT_SERIALIZED = 'v1~root~999~words~simple~mentioned~1';

@Component({ standalone: true, template: '' })
class BlankPageComponent {}

@Component({
  standalone: true,
  imports: [DetailOverlayLinkDirective],
  template: `
    <a [qdDetailLink]="frame" data-testid="start-link">SYNTH_ROOT_LINK</a>
    <a [qdDetailLink]="frame" qdDetailLinkMode="append" data-testid="append-link">SYNTH_APPEND_LINK</a>
  `,
})
class LinkHostComponent {
  frame = rootFrame;
}

describe('DetailOverlayLinkDirective', () => {
  let router: Router;
  let service: DetailOverlayHistoryService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: '**', component: BlankPageComponent }]), provideLocationMocks()],
    });
    router = TestBed.inject(Router);
    TestBed.inject(Location);
    service = TestBed.inject(DetailOverlayHistoryService);
    sessionStorage.clear();
    router.initialNavigation();
  });

  async function createHost() {
    await router.navigateByUrl('/dashboard/words/roots?root=5');
    service.start();
    const fixture = TestBed.createComponent(LinkHostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  function query(fixture: { nativeElement: HTMLElement }, testid: string): HTMLAnchorElement {
    return fixture.nativeElement.querySelector(`[data-testid="${testid}"]`) as HTMLAnchorElement;
  }

  it('renders a real canonical href over the current base URL', async () => {
    const fixture = await createHost();
    const href = query(fixture, 'start-link').getAttribute('href')!;

    expect(href).toContain('/dashboard/words/roots');
    expect(href).toContain('root=5');
    expect(href).toContain(`qdDetail=${encodeURIComponent(ROOT_SERIALIZED)}`);
    expect(href).toContain('qdDetailOpen=1');
  });

  it('intercepts an unmodified primary click and starts a one-frame stack', async () => {
    const fixture = await createHost();
    const startSpy = vi.spyOn(service, 'startStack');

    const event = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 });
    query(fixture, 'start-link').dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(startSpy).toHaveBeenCalledWith(rootFrame);
  });

  it('appends to the stack in append mode', async () => {
    const fixture = await createHost();
    const appendSpy = vi.spyOn(service, 'appendFrame');

    const event = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 });
    query(fixture, 'append-link').dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(appendSpy).toHaveBeenCalledWith(rootFrame);
  });

  it.each([
    ['ctrl', { ctrlKey: true }],
    ['meta', { metaKey: true }],
    ['shift', { shiftKey: true }],
    ['alt', { altKey: true }],
    ['middle button', { button: 1 }],
  ] as const)('leaves a %s click to the browser', async (_label, init) => {
    const fixture = await createHost();
    const startSpy = vi.spyOn(service, 'startStack');
    const appendSpy = vi.spyOn(service, 'appendFrame');

    const event = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0, ...init });
    query(fixture, 'start-link').dispatchEvent(event);

    expect(event.defaultPrevented).toBe(false);
    expect(startSpy).not.toHaveBeenCalled();
    expect(appendSpy).not.toHaveBeenCalled();
  });

  it('does not intercept the context menu', async () => {
    const fixture = await createHost();
    const startSpy = vi.spyOn(service, 'startStack');

    const event = new MouseEvent('contextmenu', { bubbles: true, cancelable: true, button: 2 });
    query(fixture, 'start-link').dispatchEvent(event);

    expect(event.defaultPrevented).toBe(false);
    expect(startSpy).not.toHaveBeenCalled();
  });
});
