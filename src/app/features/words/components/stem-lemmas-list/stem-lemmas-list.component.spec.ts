import { afterEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { StemLemmasListComponent } from './stem-lemmas-list.component';

describe('StemLemmasListComponent US6', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('renders safe new-tab anchors, buckwalter text, and a missing fallback', async () => {
    await TestBed.configureTestingModule({
      imports: [StemLemmasListComponent],
      teardown: { destroyAfterEach: true },
    }).compileComponents();

    const fixture = TestBed.createComponent(StemLemmasListComponent);
    fixture.componentRef.setInput('lemmas', [
      { lemmaId: 502, lemmaText: 'عِلْم', lemmaBuckwalter: 'Ailm', occurrencesCount: 3 },
      { lemmaId: 504, lemmaText: 'مَعْرِفَة', lemmaBuckwalter: null, occurrencesCount: 1 },
    ]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const links = root.querySelectorAll('[data-testid="stem-lemmas-list-link"]');

    expect(links).toHaveLength(2);
    expect(links[0].getAttribute('href')).toBe('/dashboard/words/lemmas?lemma=502&view=words&wordView=simple');
    expect(links[0].getAttribute('target')).toBe('_blank');
    expect(links[0].getAttribute('rel')).toBe('noopener noreferrer');
    expect(root.querySelector('[data-testid="stem-lemmas-list-buckwalter"]')?.textContent).toContain('Ailm');
    expect(root.querySelector('[data-testid="stem-lemmas-list-buckwalter-missing"]')?.textContent).toContain('—');
    expect(root.textContent).toContain('3');
    expect(root.textContent).toContain('1');
  });
});
