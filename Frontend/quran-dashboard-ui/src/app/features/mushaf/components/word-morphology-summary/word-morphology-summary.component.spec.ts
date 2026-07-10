import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WordMorphologySummaryComponent } from './word-morphology-summary.component';
import { WordMorphologyDto } from '../../models/mushaf.models';

const ROOT_TEXT_PLACEHOLDER = 'جذر-تجريبي';

function buildMorphology(root: WordMorphologyDto['root']): WordMorphologyDto {
  return {
    headPos: 'V',
    headPosLabel: { ar: 'فعل', en: 'Verb' },
    root,
    lemma: null,
    stem: null,
    isVerb: true,
    verbTense: 'past',
    verbVoice: 'active',
    caseFeature: null,
  };
}

function setInputs(
  fixture: ComponentFixture<WordMorphologySummaryComponent>,
  inputs: {
    morphology: WordMorphologyDto;
    rootExplorerHref?: string;
    lemmaExplorerHref?: string;
    stemExplorerHref?: string;
  },
): void {
  fixture.componentRef.setInput('morphology', inputs.morphology);
  fixture.componentRef.setInput('rootExplorerHref', inputs.rootExplorerHref ?? '');
  fixture.componentRef.setInput('lemmaExplorerHref', inputs.lemmaExplorerHref ?? '');
  fixture.componentRef.setInput('stemExplorerHref', inputs.stemExplorerHref ?? '');
  fixture.detectChanges();
}

describe('WordMorphologySummaryComponent', () => {
  it('renders a root explorer link when rootExplorerHref is provided', () => {
    const fixture = TestBed.createComponent(WordMorphologySummaryComponent);
    setInputs(fixture, {
      morphology: buildMorphology({
        id: 999,
        text: ROOT_TEXT_PLACEHOLDER,
        buckwalter: 'jhr-test',
      }),
      rootExplorerHref: '/dashboard/words/roots?root=999',
    });

    const root = fixture.nativeElement as HTMLElement;
    const link = root.querySelector(
      '[data-testid="word-morphology-root-link"]',
    ) as HTMLAnchorElement | null;

    expect(link).toBeTruthy();
    expect(link?.getAttribute('href')).toBe('/dashboard/words/roots?root=999');
    expect(link?.getAttribute('target')).toBe('_blank');
    expect(link?.getAttribute('rel')).toBe('noopener noreferrer');
    expect(link?.getAttribute('aria-label')).toBe('افتح الجذر في مستكشف الجذور');
  });

  it('renders link columns for root, lemma, and stem when hrefs are provided', () => {
    const fixture = TestBed.createComponent(WordMorphologySummaryComponent);
    setInputs(fixture, {
      morphology: {
        ...buildMorphology({
          id: 999,
          text: ROOT_TEXT_PLACEHOLDER,
          buckwalter: 'jhr-test',
        }),
        lemma: { id: 555, text: 'لِمَة-تجريبية', buckwalter: 'lemma-test' },
        stem: { id: 777, text: 'سِتَم-تجريبي' },
      },
      rootExplorerHref: '/dashboard/words/roots?root=999',
      lemmaExplorerHref: '/dashboard/words/lemmas?lemma=555&view=words&wordView=simple',
      stemExplorerHref: '/dashboard/words/stems?stem=777&view=words&wordView=simple',
    });

    const root = fixture.nativeElement as HTMLElement;
    const lemmaLink = root.querySelector('[data-testid="word-morphology-lemma-link"]') as HTMLAnchorElement | null;
    const stemLink = root.querySelector('[data-testid="word-morphology-stem-link"]') as HTMLAnchorElement | null;

    expect(lemmaLink?.getAttribute('href')).toBe('/dashboard/words/lemmas?lemma=555&view=words&wordView=simple');
    expect(lemmaLink?.getAttribute('target')).toBe('_blank');
    expect(lemmaLink?.getAttribute('rel')).toBe('noopener noreferrer');
    expect(stemLink?.getAttribute('href')).toBe('/dashboard/words/stems?stem=777&view=words&wordView=simple');
    expect(stemLink?.getAttribute('target')).toBe('_blank');
    expect(stemLink?.getAttribute('rel')).toBe('noopener noreferrer');
  });

  it('renders static root, lemma, and stem columns when hrefs are absent', () => {
    const fixture = TestBed.createComponent(WordMorphologySummaryComponent);
    setInputs(fixture, {
      morphology: {
        ...buildMorphology({
          id: 999,
          text: ROOT_TEXT_PLACEHOLDER,
          buckwalter: 'jhr-test',
        }),
        lemma: { id: 555, text: 'لِمَة-تجريبية', buckwalter: 'lemma-test' },
        stem: { id: 777, text: 'سِتَم-تجريبي' },
      },
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="word-morphology-root-link"]')).toBeNull();
    expect(root.querySelector('[data-testid="word-morphology-lemma-link"]')).toBeNull();
    expect(root.querySelector('[data-testid="word-morphology-stem-link"]')).toBeNull();
    expect(root.textContent).toContain(ROOT_TEXT_PLACEHOLDER);
    expect(root.textContent).toContain('لِمَة-تجريبية');
    expect(root.textContent).toContain('سِتَم-تجريبي');
  });

});
