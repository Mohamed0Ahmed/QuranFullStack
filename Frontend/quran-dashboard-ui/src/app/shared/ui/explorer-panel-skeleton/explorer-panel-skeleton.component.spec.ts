import { Component } from '@angular/core';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { ExplorerPanelSkeletonComponent } from './explorer-panel-skeleton.component';

@Component({
  selector: 'qd-test-legacy-alias-host',
  standalone: true,
  imports: [ExplorerPanelSkeletonComponent],
  template: `<qd-explorer-panel-skeleton [loadingLabel]="'جارٍ التحميل…'" />`,
})
class LegacyAliasHostComponent {}

describe('ExplorerPanelSkeletonComponent (qd-panel-skeleton)', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [ExplorerPanelSkeletonComponent, LegacyAliasHostComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function render(overrides: Record<string, unknown> = {}) {
    const fixture = TestBed.createComponent(ExplorerPanelSkeletonComponent);
    for (const [key, value] of Object.entries(overrides)) {
      (fixture.componentRef as { setInput: (name: string, value: unknown) => void }).setInput(key, value);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('defaults to the "lines" shape, reproducing the original six-line panel skeleton', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    const container = root.querySelector('[data-testid="explorer-panel-skeleton"]');
    expect(container).toBeTruthy();
    expect(container?.getAttribute('aria-busy')).toBe('true');
    expect(container?.querySelectorAll(':scope > .qd-skeleton')).toHaveLength(6);

    const status = root.querySelector('[role="status"]');
    expect(status?.classList.contains('qd-sr-only')).toBe(true);
    expect(status?.textContent?.trim()).toBe('جارٍ التحميل…');
  });

  it('keeps the legacy qd-explorer-panel-skeleton selector working as a thin alias', () => {
    const fixture = TestBed.createComponent(LegacyAliasHostComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    const container = root.querySelector('[data-testid="explorer-panel-skeleton"]');
    expect(container).toBeTruthy();
    expect(container?.querySelectorAll(':scope > .qd-skeleton')).toHaveLength(6);
  });

  it('shape="rows" delegates to qd-skeleton-rows with a single aria-busy/status region (no duplicate)', () => {
    const fixture = render({ shape: 'rows', rowsCount: 3, rowTemplate: '2rem 1fr', loadingLabel: 'جارٍ تحميل الصفوف…' });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelectorAll('.qd-skeleton-rows__row')).toHaveLength(3);
    expect(root.querySelectorAll('[role="status"]')).toHaveLength(1);
    expect(root.querySelector('[role="status"]')?.textContent?.trim()).toBe('جارٍ تحميل الصفوف…');
  });

  it('shape="panel" renders a single rounded block skeleton', () => {
    const fixture = render({ shape: 'panel' });
    const root = fixture.nativeElement as HTMLElement;

    const blocks = root.querySelectorAll('.qd-skeleton--block');
    expect(blocks).toHaveLength(1);
    expect(root.querySelectorAll('[role="status"]')).toHaveLength(1);
  });

  it('shape="panel" marks host and container so the block fills a host with a block size', () => {
    const fixture = render({ shape: 'panel' });
    const host = fixture.nativeElement as HTMLElement;

    // A panel skeleton stands in for a whole panel body, so it must be able to
    // stretch into a fixed-height host instead of leaving a 3rem bar in a tall box.
    expect(host.classList.contains('qd-panel-skeleton--panel-shape')).toBe(true);
    expect(
      host.querySelector('[data-testid="explorer-panel-skeleton"]')?.classList,
    ).toContain('explorer-panel-skeleton--panel');
  });

  it('leaves the default "lines" shape unstretched', () => {
    const fixture = render();
    const host = fixture.nativeElement as HTMLElement;

    expect(host.classList.contains('qd-panel-skeleton--panel-shape')).toBe(false);
    expect(
      host.querySelector('[data-testid="explorer-panel-skeleton"]')?.classList,
    ).not.toContain('explorer-panel-skeleton--panel');
  });

  // D40 permits a text loader only for a single-value region; it stays part of the loading
  // owner rather than becoming a fourth async concept, and it announces once, politely.
  describe('shape="text" (the single-value text loader)', () => {
    it('renders one polite busy region carrying the visible label and no skeleton blocks', () => {
      const fixture = render({ shape: 'text', loadingLabel: 'جارٍ تحميل المستخدمين…' });
      const root = fixture.nativeElement as HTMLElement;

      const region = root.querySelector('.qd-loading-state') as HTMLElement;
      expect(region.getAttribute('role')).toBe('status');
      expect(region.getAttribute('aria-live')).toBe('polite');
      expect(region.getAttribute('aria-busy')).toBe('true');
      expect(region.textContent?.trim()).toBe('جارٍ تحميل المستخدمين…');
      expect(root.querySelectorAll('.qd-skeleton')).toHaveLength(0);
      expect(root.querySelectorAll('[role="status"]')).toHaveLength(1);
    });

    it('reserves its message line only when the caller asks for it', () => {
      const plain = render({ shape: 'text', loadingLabel: 'جارٍ التحميل…' });
      expect(
        (plain.nativeElement as HTMLElement).querySelector('.qd-state--reserve'),
      ).toBeNull();

      const reserved = render({ shape: 'text', loadingLabel: '', reserve: true });
      const region = (reserved.nativeElement as HTMLElement).querySelector('.qd-loading-state');
      expect(region?.classList.contains('qd-state--reserve')).toBe(true);
      expect(region?.querySelector('.qd-state__message--visible')).toBeNull();
    });

    it('lets a legacy call-site keep its own test id', () => {
      const fixture = render({ shape: 'text', testId: 'qd-state-loading' });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="qd-state-loading"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="explorer-panel-skeleton"]')).toBeNull();
    });
  });

  it('is static under reduced motion (no inline transform set by the component)', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    const bar = root.querySelector('.qd-skeleton') as HTMLElement;

    expect(bar.getAttribute('style')).toBeNull();
  });
});
