import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { QdResultItemDirective, QdResultListDirective, QdResultListVariant } from './result-list.directive';

@Component({
  standalone: true,
  imports: [QdResultListDirective, QdResultItemDirective],
  template: `
    <div qdResultList [listVariant]="variant()" [listLabel]="label()" data-testid="list">
      @for (row of rows(); track row.id) {
        <div
          qdResultItem
          [selected]="row.id === selectedId()"
          [current]="row.id === selectedId()"
          [selectable]="true"
          [position]="row.id"
          [setSize]="rows().length"
          [attr.data-testid]="'row-' + row.id"
        >
          {{ row.label }}
        </div>
      }
    </div>
  `,
})
class HostComponent {
  readonly variant = signal<QdResultListVariant>('display-only');
  readonly label = signal<string | null>('SYNTH_LIST');
  readonly selectedId = signal<number | null>(2);
  readonly rows = signal([
    { id: 1, label: 'SYNTH_ONE' },
    { id: 2, label: 'SYNTH_TWO' },
    { id: 3, label: 'SYNTH_THREE' },
  ]);
}

describe('qdResultList / qdResultItem', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<HostComponent>>;
  let host: HostComponent;

  const query = (testId: string): HTMLElement =>
    fixture.nativeElement.querySelector(`[data-testid="${testId}"]`);

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  // D25: every non-table result collection exposes the same list/listitem vocabulary, so AT
  // enumerates an Access rail, an audit feed and an explorer detail list identically.
  it('gives the collection list semantics and every row listitem semantics', () => {
    expect(query('list').getAttribute('role')).toBe('list');
    expect(query('list').getAttribute('aria-label')).toBe('SYNTH_LIST');

    const rows = Array.from(query('list').children) as HTMLElement[];
    expect(rows).toHaveLength(3);
    expect(rows.every((row) => row.getAttribute('role') === 'listitem')).toBe(true);
  });

  it('omits the list label entirely when the consumer has none rather than announcing an empty name', () => {
    host.label.set(null);
    fixture.detectChanges();

    expect(query('list').hasAttribute('aria-label')).toBe(false);
  });

  // D26: selection is one logical thread class, and the current row is announced — not inferred
  // from a colour.
  it('marks only the selected row, and announces it as the current one', () => {
    expect(query('row-2').classList).toContain('qd-is-selected');
    expect(query('row-2').getAttribute('aria-current')).toBe('true');

    expect(query('row-1').classList).not.toContain('qd-is-selected');
    expect(query('row-1').getAttribute('aria-current')).toBeNull();

    host.selectedId.set(3);
    fixture.detectChanges();

    expect(query('row-2').getAttribute('aria-current')).toBeNull();
    expect(query('row-3').getAttribute('aria-current')).toBe('true');
  });

  it('publishes the set metadata the consumer supplied', () => {
    expect(query('row-1').getAttribute('aria-posinset')).toBe('1');
    expect(query('row-1').getAttribute('aria-setsize')).toBe('3');
  });

  it.each([
    ['linked', 'qd-result-list--linked'],
    ['display-only', 'qd-result-list--display-only'],
    ['master', 'qd-result-list--master'],
    ['event', 'qd-result-list--event'],
    ['quran-result', 'qd-result-list--quran-result'],
  ] as const)('resolves the %s renderer to exactly one named list class', (variant, expected) => {
    host.variant.set(variant);
    fixture.detectChanges();

    const variantClasses = Array.from(query('list').classList).filter((name) =>
      name.startsWith('qd-result-list--'),
    );
    expect(variantClasses).toEqual([expected]);
  });

  // The frame must not manufacture tab stops: a row is focusable only because the consumer made it
  // a real control, never because it carries a truncated value (§8.1 disclosure ladder).
  it('adds no tabindex of its own to a row', () => {
    expect(query('row-1').hasAttribute('tabindex')).toBe(false);
    expect(query('row-2').hasAttribute('tabindex')).toBe(false);
  });
});

@Component({
  standalone: true,
  imports: [QdResultListDirective, QdResultItemDirective],
  template: `
    <div qdResultList data-testid="bare-list">
      <div qdResultItem data-testid="bare-row">SYNTH_ROW</div>
    </div>
  `,
})
class BareHostComponent {}

describe('qdResultItem without set metadata', () => {
  it('omits aria-posinset and aria-setsize so AT falls back to the real DOM count', async () => {
    await TestBed.configureTestingModule({ imports: [BareHostComponent] }).compileComponents();
    const fixture = TestBed.createComponent(BareHostComponent);
    fixture.detectChanges();
    const row = fixture.nativeElement.querySelector('[data-testid="bare-row"]') as HTMLElement;

    expect(row.hasAttribute('aria-posinset')).toBe(false);
    expect(row.hasAttribute('aria-setsize')).toBe(false);
    expect(row.classList).not.toContain('qd-is-selected');
  });
});
