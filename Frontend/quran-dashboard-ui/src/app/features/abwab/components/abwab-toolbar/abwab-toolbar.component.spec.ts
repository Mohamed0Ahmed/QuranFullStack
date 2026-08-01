import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabToolbarComponent } from './abwab-toolbar.component';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';

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
});
