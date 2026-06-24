import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { RootStemsListComponent } from './root-stems-list.component';

describe('RootStemsListComponent US5', () => {
  it('renders stems as non-interactive items with counts', async () => {
    await TestBed.configureTestingModule({
      imports: [RootStemsListComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(RootStemsListComponent);
    fixture.componentRef.setInput('stems', [
      { stemId: 200, stemText: 'أصل-اختبار', occurrencesCount: 2 },
    ]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="root-stem-item"]')).toBeTruthy();
    expect(root.querySelector('button')).toBeNull();
    expect(root.querySelector('a')).toBeNull();
    expect(root.textContent).toContain('2');
  });
});
