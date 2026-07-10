import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { RootStemsListComponent } from './root-stems-list.component';

describe('RootStemsListComponent US5', () => {
  it('renders stems as anchors with counts', async () => {
    await TestBed.configureTestingModule({
      imports: [RootStemsListComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(RootStemsListComponent);
    fixture.componentRef.setInput('stems', [
      { stemId: 200, stemText: 'أصل-اختبار', occurrencesCount: 2 },
    ]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const link = root.querySelector('[data-testid="root-stem-item"]') as HTMLAnchorElement | null;
    expect(link).toBeTruthy();
    expect(link?.tagName).toBe('A');
    expect(link?.getAttribute('href')).toContain('/dashboard/words/stems?stem=200');
    expect(link?.getAttribute('target')).toBe('_blank');
    expect(link?.getAttribute('rel')).toBe('noopener noreferrer');
    expect(root.textContent).toContain('2');
  });
});
