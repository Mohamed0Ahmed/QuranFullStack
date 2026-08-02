import { describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabDoorPickerComponent } from './abwab-door-picker.component';
import { AbwabNode } from '../../models/abwab.models';

function node(overrides: Partial<AbwabNode> & { id: number; name: string }): AbwabNode {
  return {
    description: null,
    representativeAyahText: null,
    aliases: [],
    sectionId: 1,
    sectionRetired: false,
    parentId: null,
    orderValue: overrides.id,
    globalOrderValue: overrides.id,
    version: 1,
    isArchived: false,
    depth: 0,
    liveChildCount: 0,
    liveDescendantCount: 0,
    maxRelativeDepth: 0,
    relationCount: 0,
    children: [],
    ...overrides,
  };
}

// root(1) -> child(2) -> grandchild(4); root(3) is a sibling root.
const GRANDCHILD = node({ id: 4, name: 'حفيد الصبر', parentId: 2, depth: 2 });
const CHILD = node({ id: 2, name: 'فرع الصبر', parentId: 1, depth: 1, children: [GRANDCHILD] });
const ANCHOR_ROOT = node({ id: 1, name: 'الصبر', children: [CHILD] });
const OTHER_ROOT = node({ id: 3, name: 'الشكر' });
const NODES = [ANCHOR_ROOT, OTHER_ROOT];

function render(overrides: Record<string, unknown> = {}) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ imports: [AbwabDoorPickerComponent] });
  const fixture = TestBed.createComponent(AbwabDoorPickerComponent);
  fixture.componentRef.setInput('nodes', NODES);
  fixture.componentRef.setInput('pickedIds', []);
  fixture.componentRef.setInput('emptyMessage', 'لا توجد أبواب.');
  fixture.componentRef.setInput('searchPlaceholder', 'ابحث…');
  fixture.componentRef.setInput('testIdPrefix', 'picker');
  for (const [key, value] of Object.entries(overrides)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

function el(fixture: ReturnType<typeof render>, testId: string): HTMLElement | null {
  return (fixture.nativeElement as HTMLElement).querySelector(`[data-testid="${testId}"]`);
}

describe('AbwabDoorPickerComponent — the excluded-door contract (slice K)', () => {
  it('renders an excluded root as a disabled context row: no pick id, no control, tagged, aria-disabled', () => {
    const fixture = render({ excludedIds: [1], excludedTag: 'الباب المفتوح' });

    expect(el(fixture, 'picker-pick-1')).toBeNull();
    const excludedRow = el(fixture, 'picker-excluded-1');
    expect(excludedRow).toBeTruthy();
    expect(excludedRow!.getAttribute('aria-disabled')).toBe('true');
    expect(excludedRow!.querySelector('input')).toBeNull();
    expect(excludedRow!.textContent).toContain('الباب المفتوح');
  });

  it('shows the excluded root’s child at depth 1 by default, while other roots stay collapsed', () => {
    const fixture = render({ excludedIds: [1] });

    const child = el(fixture, 'picker-pick-2');
    expect(child).toBeTruthy();
    expect((child as HTMLElement).style.getPropertyValue('--abwab-door-picker-depth')).toBe('1');
    // The grandchild stays hidden: only the excluded row itself is default-expanded.
    expect(el(fixture, 'picker-pick-4')).toBeNull();
    // The unrelated root renders collapsed at depth 0 with a real control.
    const other = el(fixture, 'picker-pick-3');
    expect((other as HTMLElement).style.getPropertyValue('--abwab-door-picker-depth')).toBe('0');
    expect(other!.querySelector('input')).toBeTruthy();
  });

  it('collapses and re-expands the excluded row through its own chevron', () => {
    const fixture = render({ excludedIds: [1] });

    const chevron = () => el(fixture, 'picker-pick-chevron-1') as HTMLElement;
    expect(el(fixture, 'picker-pick-2')).toBeTruthy();

    chevron().click();
    fixture.detectChanges();
    expect(el(fixture, 'picker-pick-2')).toBeNull();

    chevron().click();
    fixture.detectChanges();
    expect(el(fixture, 'picker-pick-2')).toBeTruthy();
  });

  it('emits nothing when the excluded row itself is clicked', () => {
    const fixture = render({ excludedIds: [1] });
    const toggled = vi.fn();
    fixture.componentInstance.toggled.subscribe(toggled);

    (el(fixture, 'picker-excluded-1') as HTMLElement).click();
    expect(toggled).not.toHaveBeenCalled();
  });

  it('renders no tag when excludedTag is empty', () => {
    const fixture = render({ excludedIds: [1] });
    const excludedRow = el(fixture, 'picker-excluded-1');
    expect(excludedRow!.querySelector('.abwab-door-picker__tag')).toBeNull();
  });

  it('search still reveals a deep match under an excluded root', () => {
    const fixture = render({ excludedIds: [1] });
    const search = el(fixture, 'picker-search') as HTMLInputElement;
    search.value = 'حفيد';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(el(fixture, 'picker-pick-4')).toBeTruthy();
    expect(el(fixture, 'picker-pick-3')).toBeNull();
  });

  it('reset() restores a collapsed excluded row to its default-expanded state', () => {
    const fixture = render({ excludedIds: [1] });
    (el(fixture, 'picker-pick-chevron-1') as HTMLElement).click();
    fixture.detectChanges();
    expect(el(fixture, 'picker-pick-2')).toBeNull();

    fixture.componentInstance.reset();
    fixture.detectChanges();
    expect(el(fixture, 'picker-pick-2')).toBeTruthy();
  });

  it('leaves non-excluded behavior untouched: collapsed roots, functional checkbox, toggled emission', () => {
    const fixture = render();
    const toggled = vi.fn();
    fixture.componentInstance.toggled.subscribe(toggled);

    expect(el(fixture, 'picker-pick-1')).toBeTruthy();
    expect(el(fixture, 'picker-pick-2')).toBeNull();

    (el(fixture, 'picker-pick-1') as HTMLElement).click();
    expect(toggled).toHaveBeenCalledWith(1);

    (el(fixture, 'picker-pick-chevron-1') as HTMLElement).click();
    fixture.detectChanges();
    expect(el(fixture, 'picker-pick-2')).toBeTruthy();
  });
});
