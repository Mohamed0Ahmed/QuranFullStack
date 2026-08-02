import { Component, signal } from '@angular/core';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { QdTabsComponent, QdTabsLayout } from './tabs.component';
import { QdTabDirective } from './tab.directive';

type TabKey = 'a' | 'b' | 'c';

@Component({
  selector: 'qd-test-tabs-host',
  standalone: true,
  imports: [QdTabsComponent, QdTabDirective],
  template: `
    <div [attr.dir]="dir()">
      <qd-tabs ariaLabel="أوضاع العرض" [layout]="layout()">
        <button qdTab type="button" [selected]="active() === 'a'" data-testid="tab-a">أ</button>
        <button qdTab type="button" [selected]="active() === 'b'" data-testid="tab-b">ب</button>
        <button qdTab type="button" [selected]="active() === 'c'" data-testid="tab-c">ج</button>
      </qd-tabs>
    </div>
  `,
})
class TestTabsHostComponent {
  readonly active = signal<TabKey>('b');
  readonly dir = signal<'ltr' | 'rtl'>('ltr');
  readonly layout = signal<QdTabsLayout>('inline');
}

describe('QdTabsComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [TestTabsHostComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function render() {
    const fixture = TestBed.createComponent(TestTabsHostComponent);
    fixture.detectChanges();
    fixture.detectChanges();
    return fixture;
  }

  function tabsOf(root: HTMLElement) {
    return Array.from(root.querySelectorAll('[role="tab"]')) as HTMLElement[];
  }

  it('renders a tablist and marks exactly the selected tab as aria-selected', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    const tablist = root.querySelector('[role="tablist"]');
    expect(tablist).toBeTruthy();
    expect(tablist?.getAttribute('aria-label')).toBe('أوضاع العرض');

    const tabs = tabsOf(root);
    expect(tabs).toHaveLength(3);
    expect(tabs.map((tab) => tab.getAttribute('aria-selected'))).toEqual(['false', 'true', 'false']);
    expect(root.querySelector('[data-testid="tab-b"]')?.classList.contains('qd-is-selected')).toBe(true);
  });

  it('seeds roving tabindex on the selected tab and keeps the rest out of the tab order', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    const tabs = tabsOf(root);
    expect(tabs.map((tab) => tab.getAttribute('tabindex'))).toEqual(['-1', '0', '-1']);
  });

  it.each([
    ['ArrowRight', 'tab-b', 'tab-c'],
    ['ArrowLeft', 'tab-b', 'tab-a'],
    ['Home', 'tab-b', 'tab-a'],
    ['End', 'tab-b', 'tab-c'],
  ] as const)('LTR: %s from %s moves roving focus to %s', (key, from, expected) => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    const source = root.querySelector(`[data-testid="${from}"]`) as HTMLElement;
    source.focus();

    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    source.dispatchEvent(event);
    fixture.detectChanges();

    expect(event.defaultPrevented).toBe(true);
    const expectedEl = root.querySelector(`[data-testid="${expected}"]`);
    expect(document.activeElement).toBe(expectedEl);
    expect(expectedEl?.getAttribute('tabindex')).toBe('0');
    expect(source.getAttribute('tabindex')).toBe('-1');
  });

  it.each([
    ['ArrowLeft', 'tab-b', 'tab-c'],
    ['ArrowRight', 'tab-b', 'tab-a'],
    ['Home', 'tab-b', 'tab-a'],
    ['End', 'tab-b', 'tab-c'],
  ] as const)('RTL: %s from %s honors the reversed arrow-key direction', (key, from, expected) => {
    const fixture = render();
    fixture.componentInstance.dir.set('rtl');
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const source = root.querySelector(`[data-testid="${from}"]`) as HTMLElement;
    source.focus();

    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    source.dispatchEvent(event);
    fixture.detectChanges();

    expect(document.activeElement).toBe(root.querySelector(`[data-testid="${expected}"]`));
  });

  it('does not change the selected tab — arrow keys move focus only', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    const tabB = root.querySelector('[data-testid="tab-b"]') as HTMLElement;
    tabB.focus();
    tabB.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true, cancelable: true }));
    fixture.detectChanges();

    // Selection is consumer-owned and untouched by qd-tabs.
    expect(fixture.componentInstance.active()).toBe('b');
    expect(root.querySelector('[data-testid="tab-b"]')?.getAttribute('aria-selected')).toBe('true');
  });

  it('ignores unrelated keys', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    const tabB = root.querySelector('[data-testid="tab-b"]') as HTMLElement;
    tabB.focus();
    const event = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
    tabB.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(false);
    expect(document.activeElement).toBe(tabB);
  });

  it('applies no transform to a tab in its resting or selected state', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    const tabB = root.querySelector('[data-testid="tab-b"]') as HTMLElement;

    expect(['none', '']).toContain(getComputedStyle(tabB).transform);
  });

  // `layout` is a layout switch only: the grid strip keeps the horizontal Arrow/Home/End model
  // rather than gaining a row-aware one, so what is asserted here is the class the stylesheet keys
  // off — the geometry itself is a browser fact jsdom cannot measure.
  it('marks the tablist as a grid when asked, and leaves it inline by default', () => {
    const fixture = render();
    const tablist = (fixture.nativeElement as HTMLElement).querySelector('[role="tablist"]')!;
    expect(tablist.classList.contains('qd-tabs--grid')).toBe(false);

    fixture.componentInstance.layout.set('grid');
    fixture.detectChanges();

    expect(tablist.classList.contains('qd-tabs--grid')).toBe(true);
    expect(tablist.getAttribute('aria-orientation')).toBe('horizontal');
  });
});
