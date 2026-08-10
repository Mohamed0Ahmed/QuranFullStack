import { Component, signal, viewChild } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import {
  QdHierarchyKeyboardDirective,
  QdHierarchyRow,
  flattenQdHierarchyRows,
  isQdNativeActivationKey,
  resolveQdHierarchyIntent,
} from './hierarchy-keyboard.directive';

interface SynthNode {
  readonly id: number;
  readonly children: readonly SynthNode[];
}

const node = (id: number, children: readonly SynthNode[] = []): SynthNode => ({ id, children });

// root 1 → child 2 → grandchild 3; root 4 is a leaf.
const SYNTH_ROOTS: readonly SynthNode[] = [node(1, [node(2, [node(3)])]), node(4)];

const rowsWith = (expanded: readonly number[]): QdHierarchyRow[] =>
  flattenQdHierarchyRows(SYNTH_ROOTS, (n) => n.children, new Set(expanded));

@Component({
  standalone: true,
  imports: [QdHierarchyKeyboardDirective],
  template: `
    <div dir="rtl">
      <div qdHierarchyKeyboard [qdHierarchyRows]="rows()" role="tree" data-testid="tree">
        @for (row of rows(); track row.id) {
          <div
            role="treeitem"
            tabindex="-1"
            [attr.data-qd-hierarchy-id]="row.id"
            [attr.data-testid]="'row-' + row.id"
          ></div>
        }
      </div>
    </div>
  `,
})
class HostComponent {
  readonly rows = signal<QdHierarchyRow[]>(rowsWith([1, 2]));
  readonly hierarchy = viewChild.required(QdHierarchyKeyboardDirective);
}

describe('flattenQdHierarchyRows', () => {
  it('emits one row per visible node with its parent, child and expansion facts', () => {
    expect(rowsWith([])).toEqual([
      { id: 1, parentId: null, hasChildren: true, isExpanded: false },
      { id: 4, parentId: null, hasChildren: false, isExpanded: false },
    ]);

    expect(rowsWith([1])).toEqual([
      { id: 1, parentId: null, hasChildren: true, isExpanded: true },
      { id: 2, parentId: 1, hasChildren: true, isExpanded: false },
      { id: 4, parentId: null, hasChildren: false, isExpanded: false },
    ]);
  });

  it('does not descend into a collapsed branch', () => {
    expect(rowsWith([2]).map((row) => row.id)).toEqual([1, 4]);
  });
});

describe('resolveQdHierarchyIntent', () => {
  const resolve = (key: string, focusedId: number, direction: 'ltr' | 'rtl', expanded = [1, 2]) =>
    resolveQdHierarchyIntent({ key, rows: rowsWith(expanded), focusedId, direction });

  it('moves focus along the visible order and clamps at both ends', () => {
    expect(resolve('ArrowDown', 1, 'rtl')).toEqual({ type: 'focus', id: 2 });
    expect(resolve('ArrowUp', 2, 'rtl')).toEqual({ type: 'focus', id: 1 });
    expect(resolve('ArrowUp', 1, 'rtl')).toEqual({ type: 'none' });
    expect(resolve('ArrowDown', 4, 'rtl')).toEqual({ type: 'none' });
  });

  it('jumps to the first and last visible rows on Home and End', () => {
    expect(resolve('Home', 3, 'rtl')).toEqual({ type: 'focus', id: 1 });
    expect(resolve('End', 1, 'rtl')).toEqual({ type: 'focus', id: 4 });
  });

  // The arrow meaning is logical, not physical: inline-start enters the branch under RTL and
  // leaves it under LTR, so one keyboard script serves both directions.
  it('mirrors the branch arrows by direction', () => {
    expect(resolve('ArrowLeft', 1, 'rtl', [])).toEqual({ type: 'expand', id: 1 });
    expect(resolve('ArrowRight', 1, 'ltr', [])).toEqual({ type: 'expand', id: 1 });

    expect(resolve('ArrowRight', 1, 'rtl')).toEqual({ type: 'collapse', id: 1 });
    expect(resolve('ArrowLeft', 1, 'ltr')).toEqual({ type: 'collapse', id: 1 });
  });

  it('steps into the first child of an already expanded branch and out to the parent', () => {
    expect(resolve('ArrowLeft', 1, 'rtl')).toEqual({ type: 'focus', id: 2 });
    expect(resolve('ArrowRight', 3, 'rtl')).toEqual({ type: 'focus', id: 2 });
  });

  it('does nothing on a leaf, on an unknown focus target, or on a key it does not own', () => {
    expect(resolve('ArrowLeft', 4, 'rtl')).toEqual({ type: 'none' });
    expect(resolve('ArrowDown', 99, 'rtl')).toEqual({ type: 'none' });
    expect(resolve('Enter', 1, 'rtl')).toEqual({ type: 'none' });
    expect(resolve('ContextMenu', 1, 'rtl')).toEqual({ type: 'none' });
  });
});

describe('isQdNativeActivationKey', () => {
  it('names only the keys a nested native control activates on', () => {
    expect(isQdNativeActivationKey('Enter')).toBe(true);
    expect(isQdNativeActivationKey(' ')).toBe(true);
    expect(isQdNativeActivationKey('Spacebar')).toBe(true);
    expect(isQdNativeActivationKey('ArrowDown')).toBe(false);
  });
});

describe('QdHierarchyKeyboardDirective', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<HostComponent>>;
  let host: HostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('reads its direction from the nearest dir host', () => {
    expect(host.hierarchy().direction()).toBe('rtl');
  });

  it('resolves against its own rows input', () => {
    expect(host.hierarchy().resolve('ArrowDown', 1)).toEqual({ type: 'focus', id: 2 });
  });

  // The row lookup is by the neutral hierarchy attribute, never by a feature test id, so the
  // directive stays domain-free.
  it('finds a row element by the neutral hierarchy attribute', () => {
    const row = host.hierarchy().rowElement(2);
    expect(row?.getAttribute('data-testid')).toBe('row-2');
    expect(host.hierarchy().rowElement(999)).toBeNull();
  });

  it('moves DOM focus to the requested row', async () => {
    host.hierarchy().focusRow(3);
    await Promise.resolve();

    expect(document.activeElement?.getAttribute('data-testid')).toBe('row-3');
  });
});
