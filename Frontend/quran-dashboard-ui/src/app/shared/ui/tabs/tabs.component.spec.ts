import { Component, signal } from '@angular/core';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { QdTabsComponent, QdTabsLayout } from './tabs.component';
import { QdTabDirective } from './tab.directive';

type TabKey = 'a' | 'b' | 'c' | 'd';

@Component({
  selector: 'qd-test-tabs-host',
  standalone: true,
  imports: [QdTabsComponent, QdTabDirective],
  template: `
    <div [attr.dir]="dir()">
      <qd-tabs ariaLabel="أوضاع العرض" [layout]="layout()">
        <button qdTab type="button" panelId="synth-panel-a" [selected]="active() === 'a'" data-testid="tab-a">أ</button>
        <button qdTab type="button" [selected]="active() === 'b'" data-testid="tab-b">ب</button>
        <button qdTab type="button" [selected]="active() === 'c'" data-testid="tab-c">ج</button>
        @if (fourth()) {
          <button qdTab type="button" [selected]="active() === 'd'" data-testid="tab-d">د</button>
        }
      </qd-tabs>
    </div>
  `,
})
class TestTabsHostComponent {
  readonly active = signal<TabKey>('b');
  readonly dir = signal<'ltr' | 'rtl'>('ltr');
  readonly layout = signal<QdTabsLayout>('inline');
  readonly fourth = signal(false);
}

@Component({
  selector: 'qd-test-two-tablists-host',
  standalone: true,
  imports: [QdTabsComponent, QdTabDirective],
  template: `
    <qd-tabs ariaLabel="اللوحة المضمّنة">
      <button qdTab type="button" [selected]="true" data-testid="inline-tab">أ</button>
    </qd-tabs>
    <qd-tabs ariaLabel="اللوحة المنبثقة">
      <button qdTab type="button" [selected]="true" data-testid="overlay-tab">أ</button>
    </qd-tabs>
    <qd-tabs ariaLabel="لوحة تملك معرّفاتها">
      <button
        qdTab
        type="button"
        id="synth-owned-tab"
        aria-controls="synth-owned-panel"
        [selected]="true"
        data-testid="owned-tab"
      >أ</button>
    </qd-tabs>
  `,
})
class TestTwoTablistsHostComponent {}

@Component({
  selector: 'qd-test-tab-neighbour-host',
  standalone: true,
  imports: [QdTabsComponent, QdTabDirective],
  template: `
    <qd-tabs ariaLabel="لوحة">
      <button qdTab type="button" [selected]="true" data-testid="neighbour-tab-a">أ</button>
      <button qdTab type="button" data-testid="neighbour-tab-b">ب</button>
      <button type="button" data-testid="tab-adjacent">SYNTH_ADJACENT</button>
    </qd-tabs>
  `,
})
class TestTabNeighbourHostComponent {}

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

  // The handler is bound on the host, so every keydown bubbling through it arrives here. Only the
  // ones raised on a tab of this tablist may be answered — anything else keeps its own key model.
  it('ignores arrow and Home/End keys raised on a control that is not one of its tabs', () => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({ imports: [TestTabNeighbourHostComponent] });
    const fixture = TestBed.createComponent(TestTabNeighbourHostComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    const adjacent = root.querySelector('[data-testid="tab-adjacent"]') as HTMLElement;
    adjacent.focus();

    for (const key of ['ArrowRight', 'ArrowLeft', 'Home', 'End']) {
      const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
      adjacent.dispatchEvent(event);
      fixture.detectChanges();

      expect(event.defaultPrevented).toBe(false);
      expect(document.activeElement).toBe(adjacent);
    }
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

  // D31: an inline detail panel and the global overlay each mount a tablist; literal ids would make
  // one panel's aria-controls point at the other's tab.
  it('generates a per-instance id for every tab, and never overwrites one the consumer owns', async () => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({ imports: [TestTwoTablistsHostComponent] });
    const fixture = TestBed.createComponent(TestTwoTablistsHostComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    const inline = root.querySelector('[data-testid="inline-tab"]')!.getAttribute('id');
    const overlay = root.querySelector('[data-testid="overlay-tab"]')!.getAttribute('id');
    expect(inline).toBeTruthy();
    expect(inline).not.toBe(overlay);

    const owned = root.querySelector('[data-testid="owned-tab"]')!;
    expect(owned.getAttribute('id')).toBe('synth-owned-tab');
    expect(owned.getAttribute('aria-controls')).toBe('synth-owned-panel');

    const tablists = Array.from(root.querySelectorAll('[role="tablist"]')).map((el) => el.id);
    expect(new Set(tablists).size).toBe(tablists.length);
  });

  it('wires a tab to the panel it controls when the consumer names one', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="tab-a"]')?.getAttribute('aria-controls')).toBe('synth-panel-a');
    expect(root.querySelector('[data-testid="tab-b"]')?.hasAttribute('aria-controls')).toBe(false);
  });

  // D30: layout is decided by the tab count, never by an accidental CSS wrap into a second row.
  it('is segmented at three tabs and becomes a scrolling row at four', () => {
    const fixture = render();
    const tablist = (fixture.nativeElement as HTMLElement).querySelector('[role="tablist"]')!;

    expect(tablist.classList.contains('qd-tabs--segmented')).toBe(true);
    expect(tablist.classList.contains('qd-tabs--scrollable')).toBe(false);

    fixture.componentInstance.fourth.set(true);
    fixture.detectChanges();

    expect(tablist.classList.contains('qd-tabs--segmented')).toBe(false);
    expect(tablist.classList.contains('qd-tabs--scrollable')).toBe(true);
  });

  it('leaves an explicit grid layout out of the count-driven choice', () => {
    const fixture = render();
    fixture.componentInstance.layout.set('grid');
    fixture.detectChanges();
    const tablist = (fixture.nativeElement as HTMLElement).querySelector('[role="tablist"]')!;

    expect(tablist.classList.contains('qd-tabs--grid')).toBe(true);
    expect(tablist.classList.contains('qd-tabs--segmented')).toBe(false);
    expect(tablist.classList.contains('qd-tabs--scrollable')).toBe(false);
  });
});
