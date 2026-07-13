import { getTestBed, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { WordTypeTableViewTabsComponent } from './word-type-table-view-tabs.component';
import { WORD_TYPE_TABLE_VIEW_TABS_LABEL } from '../../models/word-types.labels';
import { WordTypeTableView } from '../../models/word-types.models';

describe('WordTypeTableViewTabsComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [WordTypeTableViewTabsComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function createTabs(selectedView: WordTypeTableView = 'words') {
    const fixture = TestBed.createComponent(WordTypeTableViewTabsComponent);
    fixture.componentRef.setInput('selectedView', selectedView);
    fixture.detectChanges();
    return fixture;
  }

  it('renders the locked RTL labels and roving tab semantics', () => {
    const fixture = createTabs('stems');
    const root = fixture.nativeElement as HTMLElement;
    const tabs = Array.from(root.querySelectorAll('[role="tab"]')) as HTMLButtonElement[];

    expect(root.querySelector('[role="tablist"]')?.getAttribute('aria-label')).toBe(WORD_TYPE_TABLE_VIEW_TABS_LABEL);
    expect(tabs.map((tab) => tab.textContent?.trim())).toEqual(['كلمات', 'جذور', 'أصول', 'صيغ']);
    expect(tabs.map((tab) => tab.getAttribute('data-testid'))).toEqual([
      'word-type-table-view-tab--words',
      'word-type-table-view-tab--roots',
      'word-type-table-view-tab--stems',
      'word-type-table-view-tab--lemmas',
    ]);
    expect(tabs.map((tab) => tab.getAttribute('aria-selected'))).toEqual(['false', 'false', 'true', 'false']);
    expect(tabs.map((tab) => tab.getAttribute('tabindex'))).toEqual(['-1', '-1', '0', '-1']);
    expect(tabs[2].classList.contains('qd-is-selected')).toBe(true);
  });

  it.each([
    ['ArrowLeft', 'words', 'roots'],
    ['ArrowRight', 'roots', 'words'],
    ['Home', 'lemmas', 'words'],
    ['End', 'words', 'lemmas'],
  ] as const)('uses %s from %s to select and focus %s in RTL order', (key, current, expected) => {
    const fixture = createTabs(current);
    const emitted: WordTypeTableView[] = [];
    fixture.componentInstance.viewSelected.subscribe((view) => emitted.push(view));

    const currentTab = fixture.nativeElement.querySelector(
      `[data-testid="word-type-table-view-tab--${current}"]`,
    ) as HTMLButtonElement;
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    currentTab.focus();
    currentTab.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(emitted).toEqual([expected]);
    expect(document.activeElement).toBe(fixture.nativeElement.querySelector(
      `[data-testid="word-type-table-view-tab--${expected}"]`,
    ));
  });

  it('does not emit when disabled and leaves unrelated keys alone', () => {
    const fixture = createTabs();
    const emitted: WordTypeTableView[] = [];
    fixture.componentInstance.viewSelected.subscribe((view) => emitted.push(view));
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    const rootsTab = fixture.nativeElement.querySelector(
      '[data-testid="word-type-table-view-tab--roots"]',
    ) as HTMLButtonElement;
    rootsTab.click();
    const disabledArrowKey = new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true, cancelable: true });
    rootsTab.dispatchEvent(disabledArrowKey);

    fixture.componentRef.setInput('disabled', false);
    fixture.detectChanges();
    const unrelatedKey = new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true, cancelable: true });
    rootsTab.dispatchEvent(unrelatedKey);

    expect(rootsTab.disabled).toBe(false);
    expect(emitted).toEqual([]);
    expect(disabledArrowKey.defaultPrevented).toBe(false);
    expect(unrelatedKey.defaultPrevented).toBe(false);
  });
});
