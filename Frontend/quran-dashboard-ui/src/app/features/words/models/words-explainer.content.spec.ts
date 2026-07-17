import { describe, expect, it } from 'vitest';

import {
  WORDS_EXPLAINER_CONTENT,
  WORDS_EXPLAINER_ORDER,
  WORDS_HUB_CHAIN,
  WORDS_HUB_INTRO,
  WordsExplainerKey,
} from './words-explainer.content';
import { ACTIVE_HUB_SECTION } from './unique-words.labels';
import { ROOTS_PAGE_TITLE } from './roots.labels';
import { LEMMAS_PAGE_TITLE } from './lemmas.labels';
import { STEMS_PAGE_TITLE } from './stems.labels';
import { WORD_TYPES_PAGE_TITLE } from './word-types.labels';

/**
 * The content records are real approved data (plan P0): construct them, never mock them. These
 * assertions guard the two things that would silently break the feature — a hero title drifting
 * from its page's own title, and a non-canonical morphology term slipping into the copy.
 */
describe('words-explainer.content', () => {
  it('covers exactly the five explorer keys, in canonical order', () => {
    expect(WORDS_EXPLAINER_ORDER).toEqual(['unique', 'roots', 'lemmas', 'stems', 'word-types']);
    expect(Object.keys(WORDS_EXPLAINER_CONTENT).sort()).toEqual([...WORDS_EXPLAINER_ORDER].sort());
  });

  it('numbers the sections ٠١…٠٥ in order', () => {
    const ordinals = WORDS_EXPLAINER_ORDER.map((key) => WORDS_EXPLAINER_CONTENT[key].ordinal);
    expect(ordinals).toEqual(['٠١', '٠٢', '٠٣', '٠٤', '٠٥']);
  });

  it("each record's title equals its explorer page's own pageTitle (card ⇄ hero cannot drift)", () => {
    expect(WORDS_EXPLAINER_CONTENT.unique.title).toBe(ACTIVE_HUB_SECTION.labelAr);
    expect(WORDS_EXPLAINER_CONTENT.roots.title).toBe(ROOTS_PAGE_TITLE);
    expect(WORDS_EXPLAINER_CONTENT.lemmas.title).toBe(LEMMAS_PAGE_TITLE);
    expect(WORDS_EXPLAINER_CONTENT.stems.title).toBe(STEMS_PAGE_TITLE);
    expect(WORDS_EXPLAINER_CONTENT['word-types'].title).toBe(WORD_TYPES_PAGE_TITLE);
  });

  it('populates every prose slot for every key, keyed by its own slug', () => {
    for (const key of WORDS_EXPLAINER_ORDER) {
      const record = WORDS_EXPLAINER_CONTENT[key];
      expect(record.key).toBe(key);
      for (const slot of ['ordinal', 'eyebrow', 'title', 'tagline', 'body', 'benefit'] as const) {
        expect(record[slot].length).toBeGreaterThan(0);
      }
    }
  });

  it('uses only the canonical morphology terms (never the internal لِمّة / جذع forms)', () => {
    const corpus = [
      WORDS_HUB_INTRO.title,
      WORDS_HUB_INTRO.subtitle,
      ...WORDS_HUB_CHAIN.map((node) => node.label),
      ...WORDS_EXPLAINER_ORDER.flatMap((key) => {
        const r = WORDS_EXPLAINER_CONTENT[key];
        return [r.eyebrow, r.tagline, r.body, r.benefit];
      }),
    ].join(' ');

    expect(corpus).not.toContain('اللِمّة');
    expect(corpus).not.toContain('اللمة');
    expect(corpus).not.toContain('الجذع');
    expect(corpus).toContain('الصيغة المعجمية');
    expect(corpus).toContain('الأصل الصرفي');
  });

  it('matches the approved taglines verbatim', () => {
    const taglines: Record<WordsExplainerKey, string> = {
      unique: 'كل كلمة قرآنية فريدة، وأين وردت — وأين لم تَرِد.',
      roots: 'النواة التي تخرج منها الأسماء والأفعال جميعًا.',
      lemmas: 'مدخل القاموس: صورة واحدة تجمع تصاريفها الإعرابية.',
      stems: 'قلب الكلمة بعد نزع السوابق واللواحق.',
      'word-types': 'استعلامات نحوية حقيقية عبر القرآن كله.',
    };
    for (const key of WORDS_EXPLAINER_ORDER) {
      expect(WORDS_EXPLAINER_CONTENT[key].tagline).toBe(taglines[key]);
    }
  });

  it('models the hub orientation chain widest → narrowest, with the grammatical axis split off by "+"', () => {
    expect(WORDS_HUB_CHAIN.map((node) => node.label)).toEqual([
      'الجذر',
      'الصيغة المعجمية',
      'الأصل الصرفي',
      'الكلمة',
      'أنواع الكلمات (نحوي)',
    ]);
    expect(WORDS_HUB_CHAIN.map((node) => node.connector)).toEqual([
      'arrow',
      'arrow',
      'arrow',
      'plus',
      null,
    ]);
  });
});
