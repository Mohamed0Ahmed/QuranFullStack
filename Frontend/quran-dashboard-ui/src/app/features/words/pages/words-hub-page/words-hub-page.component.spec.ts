import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { WordsHubPageComponent } from './words-hub-page.component';
import {
  lemmasRoutePath,
  rootsRoutePath,
  stemsRoutePath,
  uniqueWordsRoutePath,
  wordTypesRoutePath,
} from '../../../../core/navigation/route-paths';
import {
  WORDS_EXPLAINER_CONTENT,
  WORDS_HUB_CHAIN,
  WORDS_HUB_INTRO,
} from '../../models/words-explainer.content';

describe('WordsHubPageComponent', () => {
  beforeEach(async () => {
    getTestBed().resetTestingModule();
    await TestBed.configureTestingModule({
      providers: [provideRouter([])],
      teardown: { destroyAfterEach: true },
    }).compileComponents();
  });

  async function createComponent(): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(WordsHubPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders the hub intro title and subtitle from the shared content source', async () => {
    const root = await createComponent();

    expect(root.querySelector('[data-testid="words-hub-title"]')?.textContent).toContain(WORDS_HUB_INTRO.title);
    expect(root.querySelector('[data-testid="words-hub-subtitle"]')?.textContent).toContain(WORDS_HUB_INTRO.subtitle);
  });

  it('renders the orientation chain with every node label, widest → narrowest', async () => {
    const root = await createComponent();

    const chain = root.querySelector('[data-testid="words-hub-chain"]') as HTMLElement;
    const nodes = Array.from(chain.querySelectorAll('.words-hub-chain__node')).map((n) => n.textContent?.trim());
    expect(nodes).toEqual(WORDS_HUB_CHAIN.map((node) => node.label));
  });

  it('renders exactly five navigation cards, keyed by stable slug (never the Arabic label)', async () => {
    const root = await createComponent();

    for (const key of ['unique', 'roots', 'lemmas', 'stems', 'word-types']) {
      expect(root.querySelector(`[data-testid="words-hub-card--${key}"]`)).toBeTruthy();
    }
    expect(root.querySelectorAll('qd-word-section-card')).toHaveLength(5);
    expect(root.querySelector('[data-testid="words-hub-card--الجذور"]')).toBeNull();
    expect(root.querySelector('[data-testid="words-hub-card--disabled"]')).toBeNull();
    expect(root.querySelectorAll('[data-testid="word-section-coming-soon"]')).toHaveLength(0);
  });

  it('feeds each card its page hero title + tagline and the correct explorer route', async () => {
    const root = await createComponent();

    const cases: ReadonlyArray<{ key: keyof typeof WORDS_EXPLAINER_CONTENT; route: string }> = [
      { key: 'unique', route: uniqueWordsRoutePath('tashkeel') },
      { key: 'roots', route: rootsRoutePath() },
      { key: 'lemmas', route: lemmasRoutePath() },
      { key: 'stems', route: stemsRoutePath() },
      { key: 'word-types', route: wordTypesRoutePath() },
    ];

    for (const { key, route } of cases) {
      const card = root.querySelector(`[data-testid="words-hub-card--${key}"]`) as HTMLElement;
      const content = WORDS_EXPLAINER_CONTENT[key];
      expect(card.querySelector('.qd-card-title')?.textContent?.trim()).toBe(content.title);
      expect(card.querySelector('.word-section-card__desc')?.textContent?.trim()).toBe(content.tagline);
      expect(card.querySelector('a[data-testid="word-section-card"]')?.getAttribute('href')).toContain(route);
    }
  });
});
