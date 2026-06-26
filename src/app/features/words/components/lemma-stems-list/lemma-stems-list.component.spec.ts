import { afterEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { LemmaStemsListComponent } from './lemma-stems-list.component';

describe('LemmaStemsListComponent US6', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('renders safe new-tab anchors with related counts', async () => {
    await TestBed.configureTestingModule({
      imports: [LemmaStemsListComponent],
      teardown: { destroyAfterEach: true },
    }).compileComponents();

    const fixture = TestBed.createComponent(LemmaStemsListComponent);
    fixture.componentRef.setInput('stems', [
      { stemId: 600, stemText: 'كَلَّمَ', occurrencesCount: 11 },
      { stemId: 604, stemText: 'حَكَمَ', occurrencesCount: 4 },
    ]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const links = root.querySelectorAll('[data-testid="lemma-stems-list-link"]');

    expect(links).toHaveLength(2);
    expect(links[0].getAttribute('href')).toBe('/dashboard/words/stems?stem=600&view=words&wordView=simple');
    expect(links[0].getAttribute('target')).toBe('_blank');
    expect(links[0].getAttribute('rel')).toBe('noopener noreferrer');
    expect(root.textContent).toContain('11');
    expect(root.textContent).toContain('4');
  });
});
