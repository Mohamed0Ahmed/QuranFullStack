import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { QdSkeletonRowsComponent } from './skeleton-rows.component';

describe('QdSkeletonRowsComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [QdSkeletonRowsComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function render(count: number, rowTemplate: string, loadingLabel?: string) {
    const fixture = TestBed.createComponent(QdSkeletonRowsComponent);
    fixture.componentRef.setInput('count', count);
    fixture.componentRef.setInput('rowTemplate', rowTemplate);
    if (loadingLabel !== undefined) {
      fixture.componentRef.setInput('loadingLabel', loadingLabel);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('renders count rows, each using the supplied grid template', () => {
    const fixture = render(3, '2.5rem 1fr auto');
    const root = fixture.nativeElement as HTMLElement;

    const rows = Array.from(root.querySelectorAll('.qd-skeleton-rows__row')) as HTMLElement[];
    expect(rows).toHaveLength(3);
    for (const row of rows) {
      expect(getComputedStyle(row).gridTemplateColumns).toBe('2.5rem 1fr auto');
    }
  });

  it('renders one skeleton cell per column track in the row template', () => {
    const fixture = render(2, '2rem minmax(0, 1fr) auto');
    const root = fixture.nativeElement as HTMLElement;

    const firstRow = root.querySelector('[data-testid="qd-skeleton-row-0"]') as HTMLElement;
    expect(firstRow.querySelectorAll('.qd-skeleton')).toHaveLength(3);
  });

  it('is non-interactive: aria-busy on the container plus a single sr-only status label', () => {
    const fixture = render(4, '1fr auto', 'جارٍ تحميل القائمة…');
    const root = fixture.nativeElement as HTMLElement;

    const container = root.querySelector('[data-testid="qd-skeleton-rows"]');
    expect(container?.getAttribute('aria-busy')).toBe('true');

    const statusEls = root.querySelectorAll('[role="status"]');
    expect(statusEls).toHaveLength(1);
    expect(statusEls[0].textContent?.trim()).toBe('جارٍ تحميل القائمة…');
    expect(statusEls[0].classList.contains('qd-sr-only')).toBe(true);

    const rows = root.querySelectorAll('.qd-skeleton-rows__row');
    rows.forEach((row) => expect(row.getAttribute('aria-hidden')).toBe('true'));
  });

  it('renders zero rows for a zero count without throwing', () => {
    const fixture = render(0, '1fr');
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelectorAll('.qd-skeleton-rows__row')).toHaveLength(0);
  });
});
