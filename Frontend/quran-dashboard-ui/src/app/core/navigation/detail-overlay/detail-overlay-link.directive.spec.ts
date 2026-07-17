import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Location } from '@angular/common';
import { provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DetailOverlayHistoryService } from './detail-overlay-history.service';
import { DETAIL_OVERLAY_LINK_MODE, DetailOverlayLinkDirective } from './detail-overlay-link.directive';
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

@Component({
  standalone: true,
  imports: [DetailOverlayLinkDirective],
  providers: [{ provide: DETAIL_OVERLAY_LINK_MODE, useValue: 'append' }],
  template: `
    <a [qdDetailLink]="frame" data-testid="context-default-link">SYNTH_CONTEXT_LINK</a>
    <a [qdDetailLink]="frame" qdDetailLinkMode="start" data-testid="context-override-link">SYNTH_OVERRIDE_LINK</a>
  `,
})
class AppendContextHostComponent {
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

  async function createHost(component: typeof LinkHostComponent | typeof AppendContextHostComponent = LinkHostComponent) {
    await router.navigateByUrl('/dashboard/words/roots?root=5');
    service.start();
    const fixture = TestBed.createComponent(component);
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

  it('uses append as the default mode when the component tree provides the mode token', async () => {
    const fixture = await createHost(AppendContextHostComponent);
    const appendSpy = vi.spyOn(service, 'appendFrame').mockReturnValue(true);
    const startSpy = vi.spyOn(service, 'startStack').mockReturnValue(undefined);

    const event = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 });
    query(fixture, 'context-default-link').dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(appendSpy).toHaveBeenCalledWith(rootFrame);
    expect(startSpy).not.toHaveBeenCalled();
  });

  it('lets an explicit mode input beat the provided mode token', async () => {
    const fixture = await createHost(AppendContextHostComponent);
    const appendSpy = vi.spyOn(service, 'appendFrame').mockReturnValue(true);
    const startSpy = vi.spyOn(service, 'startStack').mockReturnValue(undefined);

    const event = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 });
    query(fixture, 'context-override-link').dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(startSpy).toHaveBeenCalledWith(rootFrame);
    expect(appendSpy).not.toHaveBeenCalled();
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
