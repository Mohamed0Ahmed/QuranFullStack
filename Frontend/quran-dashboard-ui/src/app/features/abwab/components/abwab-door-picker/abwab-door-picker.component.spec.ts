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

describe('AbwabDoorPickerComponent — the exclusion reason reaches assistive tech (F-96)', () => {
  it('folds the disabled tag into the checkbox’s accessible name so the reason is heard, not only seen', () => {
    const fixture = render({ disabledIds: [3], disabledTag: 'مرتبط بالفعل' });

    const box = el(fixture, 'picker-pick-checkbox-3') as HTMLInputElement;
    expect(box.disabled).toBe(true);
    expect(box.getAttribute('aria-label')).toBe('الشكر — مرتبط بالفعل');
  });

  it('leaves the accessible name as the bare door name when the row is enabled or the tag is empty', () => {
    const tagged = render({ disabledIds: [3], disabledTag: 'مرتبط بالفعل' });
    expect(el(tagged, 'picker-pick-checkbox-1')!.getAttribute('aria-label')).toBe('الصبر');

    const untagged = render({ disabledIds: [3] });
    expect(el(untagged, 'picker-pick-checkbox-3')!.getAttribute('aria-label')).toBe('الشكر');
  });

  it('carries the excluded row’s reason and disabled state on a role that exposes them', () => {
    const fixture = render({ excludedIds: [1], excludedTag: 'الباب المفتوح' });

    const excludedRow = el(fixture, 'picker-excluded-1')!;
    expect(excludedRow.getAttribute('role')).toBe('group');
    expect(excludedRow.getAttribute('aria-disabled')).toBe('true');
    expect(excludedRow.getAttribute('aria-label')).toBe('الصبر — الباب المفتوح');
    // A pickable row stays a plain container: those attributes are the excluded row's alone.
    const pickableRow = el(fixture, 'picker-pick-3')!;
    expect(pickableRow.getAttribute('role')).toBeNull();
    expect(pickableRow.getAttribute('aria-label')).toBeNull();
  });

  it('names the excluded row by the door alone when no excludedTag is supplied', () => {
    const excludedRow = el(render({ excludedIds: [1] }), 'picker-excluded-1')!;
    expect(excludedRow.getAttribute('aria-label')).toBe('الصبر');
  });
});

describe('AbwabDoorPickerComponent — the load-state surfaces (F-66, F-97)', () => {
  const LOAD_ERROR = 'تعذّر تحميل الأبواب.';

  it('shows the doors error and its retry above the rows when the load fails while rows are still bound', () => {
    const fixture = render({ status: 'error', errorMessage: LOAD_ERROR });

    const error = el(fixture, 'picker-doors-error');
    expect(error).toBeTruthy();
    expect(error!.textContent).toContain(LOAD_ERROR);
    // The stale rows survive: the failure arrived over an already-rendered list.
    expect(el(fixture, 'picker-pick-1')).toBeTruthy();
    expect(error!.compareDocumentPosition(el(fixture, 'picker-pick-1')!)).toBe(
      Node.DOCUMENT_POSITION_FOLLOWING,
    );
  });

  it('emits retry from the error shown over stale rows', () => {
    const fixture = render({ status: 'error', errorMessage: LOAD_ERROR });
    const retried = vi.fn();
    fixture.componentInstance.retry.subscribe(retried);

    (el(fixture, 'qd-state-action') as HTMLButtonElement).click();

    expect(retried).toHaveBeenCalledTimes(1);
  });

  it('renders the empty state, never an empty danger box, when the error status carries no message', () => {
    const fixture = render({ nodes: [], status: 'error' });

    expect(el(fixture, 'picker-doors-error')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('[data-testid="qd-state-error"]')).toBeNull();
    expect(el(fixture, 'picker-doors-empty')!.textContent).toContain('لا توجد أبواب.');
  });

  it('shows the error alone — not the empty state too — when the load fails with no rows', () => {
    const fixture = render({ nodes: [], status: 'error', errorMessage: LOAD_ERROR });

    expect(el(fixture, 'picker-doors-error')!.textContent).toContain(LOAD_ERROR);
    expect(el(fixture, 'picker-doors-empty')).toBeNull();
  });

  it('keeps the loading skeleton and the plain empty state on their own statuses', () => {
    expect(el(render({ nodes: [], status: 'loading' }), 'picker-loading')).toBeTruthy();
    expect(el(render({ nodes: [], status: 'empty' }), 'picker-doors-empty')).toBeTruthy();
  });
});
