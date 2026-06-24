import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { RootLemmasListComponent } from './root-lemmas-list.component';

describe('RootLemmasListComponent US5', () => {
  it('renders lemmas as non-interactive items with counts', async () => {
    await TestBed.configureTestingModule({
      imports: [RootLemmasListComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(RootLemmasListComponent);
    fixture.componentRef.setInput('lemmas', [
      { lemmaId: 100, lemmaText: 'صيغة-اختبار', occurrencesCount: 3 },
    ]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="root-lemma-item"]')).toBeTruthy();
    expect(root.querySelector('button')).toBeNull();
    expect(root.querySelector('a')).toBeNull();
    expect(root.textContent).toContain('3');
  });
});
