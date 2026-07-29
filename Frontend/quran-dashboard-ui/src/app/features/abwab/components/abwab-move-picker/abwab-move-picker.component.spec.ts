import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabMovePickerComponent } from './abwab-move-picker.component';
import { AbwabNode } from '../../models/abwab.models';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';

function node(overrides: Partial<AbwabNode> & { id: number; name: string }): AbwabNode {
  return {
    description: null,
    representativeAyahText: null,
    aliases: [],
    sectionId: null,
    parentId: null,
    orderValue: overrides.id,
    globalOrderValue: overrides.id,
    version: 1,
    isArchived: false,
    depth: 0,
    liveChildCount: 0,
    children: [],
    ...overrides,
  };
}

const SECTIONS: AbwabTreeSectionDto[] = [
  { id: 1, name: 'اللغة العربية', orderValue: 1, version: 1, doorsInScopeCount: 2 },
];

// section 1: root(1) -> child(2); section-less: root(3)
const CHILD = node({ id: 2, name: 'الفرع', parentId: 1, sectionId: 1, depth: 1 });
const ROOT = node({ id: 1, name: 'الأصل', sectionId: 1, children: [CHILD] });
const FREE_ROOT = node({ id: 3, name: 'بلا قسم', sectionId: null });
const LIVE_ROOTS = [ROOT, FREE_ROOT];

function render(overrides: Record<string, unknown> = {}) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ imports: [AbwabMovePickerComponent] });
  const fixture = TestBed.createComponent(AbwabMovePickerComponent);
  fixture.componentRef.setInput('open', true);
  fixture.componentRef.setInput('sections', SECTIONS);
  fixture.componentRef.setInput('liveRoots', LIVE_ROOTS);
  fixture.componentRef.setInput('titleText', 'نقل «الأصل»');
  for (const [key, value] of Object.entries(overrides)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('AbwabMovePickerComponent — M30', () => {
  it('stage one lists every real section plus «بلا قسم»', () => {
    const root = render().nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="abwab-move-picker-section-1"]')?.textContent).toContain(
      'اللغة العربية',
    );
    expect(root.querySelector('[data-testid="abwab-move-picker-section-none"]')?.textContent).toContain('بلا قسم');
  });

  it('stage two, after picking a section, offers «كباب رئيسي» plus that section’s doors, indented by depth', () => {
    const fixture = render();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="abwab-move-picker-dest-1"]')?.textContent).toContain('الأصل');
    expect(root.querySelector('[data-testid="abwab-move-picker-dest-2"]')?.textContent).toContain('الفرع');
  });

  it('excludes the moved door and its descendants from the destination list', () => {
    const fixture = render({ excludedIds: new Set([1, 2]) });
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="abwab-move-picker-dest-1"]')).toBeNull();
    expect(root.querySelector('[data-testid="abwab-move-picker-dest-2"]')).toBeNull();
    expect(root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]')).toBeTruthy();
  });

  it('confirms «كباب رئيسي» scoped to the picked section as {targetParentId: null, targetSectionId}', () => {
    const fixture = render();
    const confirmed: unknown[] = [];
    fixture.componentInstance.confirmed.subscribe((v) => confirmed.push(v));

    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-dest-asmain"]') as HTMLElement).click();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLElement).click();

    expect(confirmed).toEqual([{ targetParentId: null, targetSectionId: 1 }]);
  });

  it('confirms nesting under a picked destination door as {targetParentId: <id>, targetSectionId}', () => {
    const fixture = render();
    const confirmed: unknown[] = [];
    fixture.componentInstance.confirmed.subscribe((v) => confirmed.push(v));

    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-dest-1"]') as HTMLElement).click();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLElement).click();

    expect(confirmed).toEqual([{ targetParentId: 1, targetSectionId: 1 }]);
  });

  it('«بلا قسم» stage one maps to a null section scope', () => {
    const fixture = render();
    const confirmed: unknown[] = [];
    fixture.componentInstance.confirmed.subscribe((v) => confirmed.push(v));

    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-none"]') as HTMLElement).click();
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-dest-asmain"]') as HTMLElement).click();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLElement).click();

    expect(confirmed).toEqual([{ targetParentId: null, targetSectionId: null }]);
  });

  it('T402 — the destination order follows liveRoots’ given order (the superset’s global order), not a per-section re-sort by orderValue', () => {
    // orderValue says door 6 ("أ") comes before door 5 ("ب") in section 1's own order — but
    // liveRoots (the global order this component is handed) lists 5 before 6, so the
    // destination list must too, per the T402 decision recorded on the component.
    const doorFive = node({ id: 5, name: 'ب', sectionId: 1, orderValue: 2 });
    const doorSix = node({ id: 6, name: 'أ', sectionId: 1, orderValue: 1 });
    const fixture = render({ liveRoots: [doorFive, doorSix] });

    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
    fixture.detectChanges();

    const destIds = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('[data-testid^="abwab-move-picker-dest-"]'),
    ).map((el) => el.getAttribute('data-testid'));
    expect(destIds).toEqual(['abwab-move-picker-dest-asmain', 'abwab-move-picker-dest-5', 'abwab-move-picker-dest-6']);
  });

  it('emits closed on cancel without confirming', () => {
    const fixture = render();
    const closed: void[] = [];
    fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-cancel"]') as HTMLElement).click();

    expect(closed).toHaveLength(1);
  });
});
