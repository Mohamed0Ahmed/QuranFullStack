import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabToolbarComponent } from './abwab-toolbar.component';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { ABWAB_LABELS } from '../../models/abwab.labels';

const SECTIONS: AbwabTreeSectionDto[] = [
  { id: 1, name: 'اللغة العربية', orderValue: 1, version: 1, doorsInScopeCount: 5 },
  { id: 2, name: 'العلوم الحديثة', orderValue: 2, version: 1, doorsInScopeCount: 3 },
];

function render(overrides: Record<string, unknown> = {}) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ imports: [AbwabToolbarComponent] });
  const fixture = TestBed.createComponent(AbwabToolbarComponent);
  fixture.componentRef.setInput('sections', SECTIONS);
  fixture.componentRef.setInput('activeSectionId', null);
  for (const [key, value] of Object.entries(overrides)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

// The tab's visible label is its own leading text node — the count badge is a sibling <span>
// appended after it (§4.2-10), so a bare textContent read would concatenate both.
function visibleLabel(el: Element): string {
  return el.childNodes[0]?.textContent?.trim() ?? '';
}

describe('AbwabToolbarComponent', () => {
  it('renders «كل الأبواب» plus one tab per section, and no «الأبواب الرئيسية» tab', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    const labels = Array.from(root.querySelectorAll('[role="tab"]')).map(visibleLabel);

    expect(labels).toEqual(['كل الأبواب', 'اللغة العربية', 'العلوم الحديثة']);
    expect(labels).not.toContain('الأبواب الرئيسية');
  });

  it('marks the active section tab as selected', () => {
    const fixture = render({ activeSectionId: 2 });
    const root = fixture.nativeElement as HTMLElement;

    const selected = root.querySelector('[aria-selected="true"]');
    expect(selected && visibleLabel(selected)).toBe('العلوم الحديثة');
  });

  it('emits sectionChanged with the section id, or null for «كل الأبواب»', () => {
    const fixture = render();
    const changes: (number | null)[] = [];
    fixture.componentInstance.sectionChanged.subscribe((id: number | null) => changes.push(id));

    const root = fixture.nativeElement as HTMLElement;
    const tabs = Array.from(root.querySelectorAll('[role="tab"]')) as HTMLElement[];
    tabs[1].click();
    tabs[0].click();

    expect(changes).toEqual([1, null]);
  });

  describe('T502 — the tree/cards view toggle', () => {
    it('marks the active view button and emits viewChanged when the other is clicked', () => {
      const fixture = render({ view: 'tree' });
      const changes: string[] = [];
      fixture.componentInstance.viewChanged.subscribe((view: string) => changes.push(view));

      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-toolbar-view-tree"]')?.classList).toContain(
        'abwab-toolbar__view-btn--active',
      );

      (root.querySelector('[data-testid="abwab-toolbar-view-cards"]') as HTMLElement).click();

      expect(changes).toEqual(['cards']);
    });

    // F-59: the active view was carried by class + colour alone. aria-pressed is the same
    // attribute the relations modal's direction pill already uses.
    it('exposes the active view through aria-pressed on both buttons', () => {
      const treeRoot = render({ view: 'tree' }).nativeElement as HTMLElement;
      expect(treeRoot.querySelector('[data-testid="abwab-toolbar-view-tree"]')?.getAttribute('aria-pressed')).toBe(
        'true',
      );
      expect(treeRoot.querySelector('[data-testid="abwab-toolbar-view-cards"]')?.getAttribute('aria-pressed')).toBe(
        'false',
      );

      const cardsRoot = render({ view: 'cards' }).nativeElement as HTMLElement;
      expect(cardsRoot.querySelector('[data-testid="abwab-toolbar-view-tree"]')?.getAttribute('aria-pressed')).toBe(
        'false',
      );
      expect(cardsRoot.querySelector('[data-testid="abwab-toolbar-view-cards"]')?.getAttribute('aria-pressed')).toBe(
        'true',
      );
    });
  });

  describe('T508 — hideSectionControls (the archive view has no live section grouping)', () => {
    it('hides the section tabs and view toggle, keeping the search box, when set', () => {
      const root = render({ hideSectionControls: true }).nativeElement as HTMLElement;
      expect(root.querySelector('[role="tab"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-toolbar-view-tree"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-toolbar-search"]')).toBeTruthy();
    });
  });

  describe('item 19 — the tab count badges', () => {
    it('renders a badge on every tab, «كل الأبواب» showing the total and each section its own root count', () => {
      const root = render({
        totalRootCount: 8,
        rootCountBySectionId: new Map([[1, 3]]),
      }).nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-toolbar-tab-all-count"]')?.textContent?.trim()).toBe('8');
      expect(root.querySelector('[data-testid="abwab-toolbar-tab-1-count"]')?.textContent?.trim()).toBe('3');
      // Section 2 has no entry in the map — it renders zero rather than unmounting the box.
      expect(root.querySelector('[data-testid="abwab-toolbar-tab-2-count"]')?.textContent?.trim()).toBe('0');
    });

    it('marks a zero count with the --empty class rather than unmounting the box', () => {
      const root = render({ totalRootCount: 0, rootCountBySectionId: new Map() }).nativeElement as HTMLElement;

      const allCount = root.querySelector('[data-testid="abwab-toolbar-tab-all-count"]');
      expect(allCount).toBeTruthy();
      expect(allCount?.classList).toContain('qd-tabs__count--empty');

      const sectionCount = root.querySelector('[data-testid="abwab-toolbar-tab-1-count"]');
      expect(sectionCount?.classList).toContain('qd-tabs__count--empty');
    });

    it('does not mark a non-zero count with the --empty class', () => {
      const root = render({ totalRootCount: 8, rootCountBySectionId: new Map([[1, 3]]) })
        .nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-toolbar-tab-all-count"]')?.classList).not.toContain(
        'qd-tabs__count--empty',
      );
      expect(root.querySelector('[data-testid="abwab-toolbar-tab-1-count"]')?.classList).not.toContain(
        'qd-tabs__count--empty',
      );
    });

    // F-74: the tab badge counts ROOT doors while the stat bar's «كل الأبواب» line counts
    // every depth — the title gives a sighted user the same scope-naming phrase the tab's
    // aria-label already carries, so the two adjacent numbers stop reading as one count.
    it('carries a visible root-scope qualifier on every badge, matching the tab’s accessible phrase', () => {
      const root = render({
        totalRootCount: 8,
        rootCountBySectionId: new Map([[1, 3]]),
      }).nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-toolbar-tab-all-count"]')?.getAttribute('title')).toBe(
        ABWAB_LABELS.allDoorsTabRootCountAriaLabel(8),
      );
      expect(root.querySelector('[data-testid="abwab-toolbar-tab-1-count"]')?.getAttribute('title')).toBe(
        ABWAB_LABELS.tabRootCountAriaLabel('اللغة العربية', 3),
      );
      expect(ABWAB_LABELS.allDoorsTabRootCountAriaLabel(8)).toContain('رئيسية');
    });

    it('hides the badge digits from assistive technology and names the counted noun on the tab instead', () => {
      const root = render({
        totalRootCount: 8,
        rootCountBySectionId: new Map([[1, 3]]),
      }).nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-toolbar-tab-all-count"]')?.getAttribute('aria-hidden')).toBe(
        'true',
      );
      expect(root.querySelector('[data-testid="abwab-toolbar-tab-all"]')?.getAttribute('aria-label')).toContain(
        'كل الأبواب',
      );
      const sectionTab = root.querySelector('[data-testid="abwab-toolbar-tab-1"]');
      expect(sectionTab?.getAttribute('aria-label')).toContain('اللغة العربية');
      expect(sectionTab?.getAttribute('aria-label')).toContain('3');
    });
  });

  // ux-slice-l. The seen count and the spoken count are two elements on purpose: a live
  // `role="status"` bound to the count would speak once per typed character.
  describe('the search match count', () => {
    const visibleCount = (fixture: ReturnType<typeof render>) =>
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-toolbar-search-count"]');
    const announceRegion = (fixture: ReturnType<typeof render>) =>
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-toolbar-search-count-announce"]')!;

    it('shows the visible count only while a query is present, and hides it from AT', () => {
      const withQuery = render({ searchQuery: 'صبر', searchMatchCount: 3 });
      expect(visibleCount(withQuery)?.textContent?.trim()).toBe(ABWAB_LABELS.searchMatchCount(3));
      expect(visibleCount(withQuery)?.getAttribute('aria-hidden')).toBe('true');

      expect(visibleCount(render({ searchQuery: '', searchMatchCount: 0 }))).toBeNull();
    });

    it('mounts the status region even with no query, so a later change is announced at all', () => {
      const fixture = render({ searchQuery: '', searchMatchCount: 0 });

      expect(announceRegion(fixture).getAttribute('role')).toBe('status');
      expect(announceRegion(fixture).textContent?.trim()).toBe('');
    });

    describe('the announcement settles before it speaks', () => {
      beforeEach(() => vi.useFakeTimers());
      afterEach(() => vi.useRealTimers());

      const type = (fixture: ReturnType<typeof render>, query: string, count: number) => {
        fixture.componentRef.setInput('searchQuery', query);
        fixture.componentRef.setInput('searchMatchCount', count);
        fixture.detectChanges();
      };

      it('stays silent through rapid input and speaks the settled count once typing stops', () => {
        const fixture = render({ searchQuery: '', searchMatchCount: 0 });

        type(fixture, 'ص', 9);
        vi.advanceTimersByTime(200);
        type(fixture, 'صب', 4);
        vi.advanceTimersByTime(200);
        type(fixture, 'صبر', 2);

        // Two keystrokes went by inside one 500ms window and neither reached the region.
        vi.advanceTimersByTime(499);
        fixture.detectChanges();
        expect(announceRegion(fixture).textContent?.trim()).toBe('');

        vi.advanceTimersByTime(1);
        fixture.detectChanges();
        expect(announceRegion(fixture).textContent?.trim()).toBe(ABWAB_LABELS.searchMatchCount(2));
      });

      it('empties immediately on clear and announces nothing', () => {
        const fixture = render({ searchQuery: '', searchMatchCount: 0 });
        type(fixture, 'صبر', 2);
        vi.advanceTimersByTime(500);
        fixture.detectChanges();
        expect(announceRegion(fixture).textContent?.trim()).toBe(ABWAB_LABELS.searchMatchCount(2));

        type(fixture, '', 0);
        fixture.detectChanges();
        // Emptied now, not after a settle — a stale count must not be re-read later.
        expect(announceRegion(fixture).textContent?.trim()).toBe('');

        vi.advanceTimersByTime(1000);
        fixture.detectChanges();
        expect(announceRegion(fixture).textContent?.trim()).toBe('');
      });
    });
  });
});
