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
  inputs: { morphology: WordMorphologyDto; rootExplorerHref?: string },
): void {
  fixture.componentRef.setInput('morphology', inputs.morphology);
  fixture.componentRef.setInput('rootExplorerHref', inputs.rootExplorerHref ?? '');
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
    expect(link?.getAttribute('rel')).toBe('noopener');
    expect(link?.getAttribute('aria-label')).toBe('افتح الجذر في مستكشف الجذور');
  });

  it('renders a static root column when rootExplorerHref is absent', () => {
    const fixture = TestBed.createComponent(WordMorphologySummaryComponent);
    setInputs(fixture, {
      morphology: buildMorphology({
        id: 999,
        text: ROOT_TEXT_PLACEHOLDER,
        buckwalter: 'jhr-test',
      }),
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="word-morphology-root-link"]')).toBeNull();
    expect(root.textContent).toContain(ROOT_TEXT_PLACEHOLDER);
  });
});
