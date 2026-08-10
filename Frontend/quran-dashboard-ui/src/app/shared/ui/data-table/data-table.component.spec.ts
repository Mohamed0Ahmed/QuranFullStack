import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { describe, expect, it, vi } from 'vitest';

import { QdDataTableComponent } from './data-table.component';

interface TableRow {
  id: number;
  label: string;
}

@Component({
  standalone: true,
  imports: [QdDataTableComponent],
  template: `
    <qd-data-table
      [renderer]="renderer"
      [rows]="rows"
      [rowId]="rowId"
      ariaLabel="Example results"
      bodyAriaLabel="Example rows"
      [columnCount]="2"
      [totalRowCount]="totalRowCount"
      [selectedRow]="selectedRow"
      [state]="state"
      [selectable]="true"
      (rowSelected)="selectedFromTable = $event"
    >
      <ng-template #headerTemplate><span role="columnheader">Name</span></ng-template>
      <ng-template #rowTemplate let-row let-index="index"><span data-testid="row">{{ index }} {{ row.label }}</span></ng-template>
      <ng-template #compactRowTemplate let-row><span data-testid="compact-row">{{ row.label }}</span></ng-template>
      <ng-template #loadingTemplate><span data-testid="loading">Loading</span></ng-template>
      <ng-template #emptyTemplate><span data-testid="empty">Empty</span></ng-template>
      <ng-template #errorTemplate><span data-testid="error">Error</span></ng-template>
      <ng-template #paginationTemplate><nav data-testid="pagination">Pages</nav></ng-template>
    </qd-data-table>
  `,
})
class DataTableHostComponent {
  renderer: 'standard' | 'wide-columns' | 'grouped-rows' = 'standard';
  rows: readonly TableRow[] = [
    { id: 1, label: 'First' },
    { id: 2, label: 'Second' },
  ];
  totalRowCount = 12;
  selectedRow: TableRow | null = this.rows[1];
  state: 'loading' | 'refreshing' | 'ready' | 'empty' | 'error' = 'ready';
  selectedFromTable: TableRow | undefined;
  readonly rowId = (row: TableRow): number => row.id;
}

describe('QdDataTableComponent', () => {
  async function create(): Promise<{
    fixture: ComponentFixture<DataTableHostComponent>;
    host: DataTableHostComponent;
    table: HTMLElement;
  }> {
    await TestBed.configureTestingModule({ imports: [DataTableHostComponent] }).compileComponents();
    const fixture = TestBed.createComponent(DataTableHostComponent);
    fixture.detectChanges();
    return {
      fixture,
      host: fixture.componentInstance,
      table: fixture.nativeElement.querySelector('qd-data-table') as HTMLElement,
    };
  }

  it.each(['standard', 'wide-columns', 'grouped-rows'] as const)(
    'renders the %s renderer as a table outside Compact',
    async (renderer) => {
      const { fixture, host, table } = await create();
      host.renderer = renderer;
      fixture.detectChanges();

      expect(table.getAttribute('role')).toBe('table');
      expect(table.getAttribute('data-renderer')).toBe(renderer);
    },
  );

  it('renders row counts, selection state, and the projected pagination slot', async () => {
    const { fixture, table } = await create();
    fixture.detectChanges();

    expect(table.getAttribute('aria-rowcount')).toBe('12');
    expect(table.getAttribute('aria-colcount')).toBe('2');
    expect(table.getAttribute('aria-label')).toBe('Example results');
    expect(table.querySelector('[data-testid="qd-data-table-body"]')?.getAttribute('aria-label')).toBe('Example rows');
    const selected = table.querySelector('[role="row"][aria-selected="true"]') as HTMLElement;
    expect(selected.getAttribute('data-row-id')).toBe('2');
    expect(selected.getAttribute('aria-current')).toBe('true');
    expect(selected.classList).toContain('qd-data-table__row--selected');
    expect(table.querySelector('[data-testid="pagination"]')).not.toBeNull();
  });

  it.each([
    ['loading', 'loading'],
    ['empty', 'empty'],
    ['error', 'error'],
  ] as const)('keeps its shell mounted for the %s lifecycle state', async (state, testId) => {
    const { fixture, host, table } = await create();
    host.state = state;
    fixture.detectChanges();

    expect(table.querySelector(`[data-testid="${testId}"]`)).not.toBeNull();
  });

  it('uses semantic list cards at the Compact breakpoint', async () => {
    const matchMedia = vi.fn().mockReturnValue({
      matches: true,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    });
    vi.stubGlobal('matchMedia', matchMedia);
    const { fixture, table } = await create();
    fixture.detectChanges();

    expect(table.getAttribute('role')).toBe('list');
    expect(table.getAttribute('aria-rowcount')).toBeNull();
    expect(table.getAttribute('aria-colcount')).toBeNull();
    expect(table.querySelectorAll('[role="listitem"]')).toHaveLength(2);
    expect(table.querySelector('[data-testid="compact-row"]')).not.toBeNull();
  });

  it('keeps ready rows mounted and marks the shell busy while refreshing', async () => {
    const { fixture, host, table } = await create();
    host.state = 'refreshing';
    fixture.detectChanges();

    expect(table.getAttribute('aria-busy')).toBe('true');
    expect(table.querySelector('[data-testid="row"]')).not.toBeNull();

    // The F12 refreshing owner is the shared indicator, anchored by the region class on the shell,
    // and it stays out of the accessibility tree: the shell's aria-busy is the only announcement.
    expect(table.classList.contains('qd-refreshing-region')).toBe(true);
    const indicator = table.querySelector('[data-testid="qd-data-table-refreshing"]');
    expect(indicator).not.toBeNull();
    expect(indicator!.getAttribute('aria-hidden')).toBe('true');
    expect(indicator!.getAttribute('role')).toBeNull();

    host.state = 'ready';
    fixture.detectChanges();
    expect(table.classList.contains('qd-refreshing-region')).toBe(false);
    expect(table.querySelector('[data-testid="qd-data-table-refreshing"]')).toBeNull();
  });

  it('supports row activation for standard tables and forbids it for grouped display rows', async () => {
    const { fixture, host, table } = await create();
    const row = table.querySelector('[role="row"][tabindex="0"]') as HTMLElement;
    row.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    expect(host.selectedFromTable).toBe(host.rows[0]);

    host.selectedFromTable = undefined;
    host.renderer = 'grouped-rows';
    fixture.detectChanges();
    const groupedRow = table.querySelector('[role="row"]') as HTMLElement;
    groupedRow.click();
    expect(groupedRow.getAttribute('tabindex')).toBeNull();
    expect(host.selectedFromTable).toBeUndefined();
  });

  it('uses the virtual body when ResizeObserver is available outside Compact', async () => {
    vi.stubGlobal(
      'ResizeObserver',
      class {
        observe(): void {}
        unobserve(): void {}
        disconnect(): void {}
      },
    );
    const { fixture, table } = await create();
    fixture.detectChanges();

    expect(table.querySelector('cdk-virtual-scroll-viewport')).not.toBeNull();
  });

  it('uses the plain body when ResizeObserver is unavailable and scrolls it to the top', async () => {
    vi.stubGlobal('ResizeObserver', undefined);
    const { fixture, table } = await create();
    fixture.detectChanges();
    const body = table.querySelector('[data-testid="qd-data-table-body"]') as HTMLElement;
    body.scrollTop = 240;

    const component = fixture.debugElement.query(By.directive(QdDataTableComponent)).componentInstance as QdDataTableComponent<TableRow>;
    component.scrollToTop();

    expect(body.scrollTop).toBe(0);
    expect(table.querySelector('cdk-virtual-scroll-viewport')).toBeNull();
  });

  it('keeps row elements stable by the supplied row identity when ordering changes', async () => {
    vi.stubGlobal('ResizeObserver', undefined);
    const { fixture, host, table } = await create();
    const firstRow = table.querySelector('[data-row-id="1"]');

    host.rows = [host.rows[1], host.rows[0]];
    fixture.detectChanges();

    expect(table.querySelector('[data-row-id="1"]')).toBe(firstRow);
  });
});
