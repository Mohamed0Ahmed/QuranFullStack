import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { QdSortableHeaderComponent } from './sortable-header.component';

@Component({
  standalone: true,
  imports: [QdSortableHeaderComponent],
  template: `
    <qd-sortable-header
      label="Occurrences"
      currentSortLabel="descending"
      nextSortLabel="ascending"
      actionAriaLabel="Sort by occurrences ascending"
      [sortState]="sortState"
      glyph="▼"
      testId="occurrences-sort"
      (activated)="activations += 1"
    />
  `,
})
class SortableHeaderHostComponent {
  activations = 0;
  sortState: 'ascending' | 'descending' | 'none' = 'descending';
}

describe('QdSortableHeaderComponent', () => {
  async function create(): Promise<{
    fixture: ComponentFixture<SortableHeaderHostComponent>;
    host: SortableHeaderHostComponent;
    header: HTMLElement;
  }> {
    await TestBed.configureTestingModule({ imports: [SortableHeaderHostComponent] }).compileComponents();
    const fixture = TestBed.createComponent(SortableHeaderHostComponent);
    fixture.detectChanges();
    return {
      fixture,
      host: fixture.componentInstance,
      header: fixture.nativeElement.querySelector('qd-sortable-header') as HTMLElement,
    };
  }

  it('exposes its supplied sort state and next action through a native button', async () => {
    const { header } = await create();
    const button = header.querySelector('button') as HTMLButtonElement;

    expect(header.getAttribute('aria-sort')).toBe('descending');
    expect(button.type).toBe('button');
    expect(button.getAttribute('aria-label')).toBe('Sort by occurrences ascending');
    expect(button.getAttribute('data-testid')).toBe('occurrences-sort');
    expect(button.textContent).toContain('▼');
  });

  it('omits aria-sort when the header is inactive', async () => {
    const { fixture, host, header } = await create();
    host.sortState = 'none';
    fixture.detectChanges();

    expect(header.hasAttribute('aria-sort')).toBe(false);
  });

  it('emits activation without interpreting the caller sort grammar', async () => {
    const { fixture, host, header } = await create();
    (header.querySelector('button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(host.activations).toBe(1);
  });
});
