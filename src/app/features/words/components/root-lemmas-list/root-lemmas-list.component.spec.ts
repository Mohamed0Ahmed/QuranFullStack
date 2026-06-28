import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { RootLemmasListComponent } from './root-lemmas-list.component';

describe('RootLemmasListComponent US5', () => {
  it('renders lemmas as anchors with counts', async () => {
    await TestBed.configureTestingModule({
      imports: [RootLemmasListComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(RootLemmasListComponent);
    fixture.componentRef.setInput('lemmas', [
      { lemmaId: 100, lemmaText: 'صيغة-اختبار', occurrencesCount: 3 },
    ]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const link = root.querySelector('[data-testid="root-lemma-item"]') as HTMLAnchorElement | null;
    expect(link).toBeTruthy();
    expect(link?.tagName).toBe('A');
    expect(link?.getAttribute('href')).toContain('/dashboard/words/lemmas?lemma=100');
    expect(link?.getAttribute('target')).toBe('_blank');
    expect(link?.getAttribute('rel')).toBe('noopener noreferrer');
    expect(root.textContent).toContain('3');
  });
});
