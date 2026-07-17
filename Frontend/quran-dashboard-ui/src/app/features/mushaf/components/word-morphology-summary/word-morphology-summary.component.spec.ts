import { beforeEach, describe, expect, it } from 'vitest';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import {
  LemmaDetailFrame,
  RootDetailFrame,
  StemDetailFrame,
  WordTypeDetailFrame,
} from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { WordMorphologySummaryComponent } from './word-morphology-summary.component';
import { WordMorphologyDto } from '../../models/mushaf.models';

const ROOT_TEXT_PLACEHOLDER = 'جذر-تجريبي';

const ROOT_FRAME: RootDetailFrame = {
  kind: 'root',
  id: 999,
  view: 'words',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
};

const LEMMA_FRAME: LemmaDetailFrame = {
  kind: 'lemma',
  id: 555,
  view: 'words',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
  typeCode: null,
};

const STEM_FRAME: StemDetailFrame = {
  kind: 'stem',
  id: 777,
  view: 'words',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
  typeCode: null,
};

const WORD_TYPE_FRAME: WordTypeDetailFrame = {
  kind: 'wordType',
  tashkeelWordId: 101,
  contextCode: 'past',
  case: 'all',
  tense: 'all',
  voice: 'all',
  view: 'ayahs',
  detailPage: 1,
};

const ROOT_SERIALIZED = 'v1~root~999~words~simple~mentioned~1';
const LEMMA_SERIALIZED = 'v1~lemma~555~words~simple~mentioned~1~-';
const STEM_SERIALIZED = 'v1~stem~777~words~simple~mentioned~1~-';
const WORD_TYPE_SERIALIZED = 'v1~wordType~101~past~all~all~all~ayahs~1';

@Component({ standalone: true, template: '' })
class BlankPageComponent {}

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
    wordTypeFrame?: WordTypeDetailFrame | null;
    rootFrame?: RootDetailFrame | null;
    lemmaFrame?: LemmaDetailFrame | null;
    stemFrame?: StemDetailFrame | null;
  },
): void {
  fixture.componentRef.setInput('morphology', inputs.morphology);
  fixture.componentRef.setInput('wordTypeFrame', inputs.wordTypeFrame ?? null);
  fixture.componentRef.setInput('rootFrame', inputs.rootFrame ?? null);
  fixture.componentRef.setInput('lemmaFrame', inputs.lemmaFrame ?? null);
  fixture.componentRef.setInput('stemFrame', inputs.stemFrame ?? null);
  fixture.detectChanges();
}

describe('WordMorphologySummaryComponent', () => {
  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: '**', component: BlankPageComponent }]), provideLocationMocks()],
    });
    const router = TestBed.inject(Router);
    sessionStorage.clear();
    router.initialNavigation();
    await router.navigateByUrl('/dashboard/mushaf?page=5');
    TestBed.inject(DetailOverlayHistoryService).start();
  });

  it('renders a detail-overlay root link when a root frame is provided', () => {
    const fixture = TestBed.createComponent(WordMorphologySummaryComponent);
    setInputs(fixture, {
      morphology: buildMorphology({
        id: 999,
        text: ROOT_TEXT_PLACEHOLDER,
        buckwalter: 'jhr-test',
      }),
      rootFrame: ROOT_FRAME,
    });

    const root = fixture.nativeElement as HTMLElement;
    const link = root.querySelector(
      '[data-testid="word-morphology-root-link"]',
    ) as HTMLAnchorElement | null;

    expect(link).toBeTruthy();
    const href = link?.getAttribute('href') ?? '';
    expect(href).toContain('/dashboard/mushaf');
    expect(href).toContain('page=5');
    expect(href).toContain(`qdDetail=${encodeURIComponent(ROOT_SERIALIZED)}`);
    expect(href).toContain('qdDetailOpen=1');
    expect(link?.getAttribute('target')).toBeNull();
    expect(link?.getAttribute('rel')).toBeNull();
    expect(link?.getAttribute('aria-label')).toBe('افتح الجذر في بطاقة التفاصيل');
  });

  it('renders link columns for root, lemma, and stem when frames are provided', () => {
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
      rootFrame: ROOT_FRAME,
      lemmaFrame: LEMMA_FRAME,
      stemFrame: STEM_FRAME,
    });

    const root = fixture.nativeElement as HTMLElement;
    const lemmaLink = root.querySelector('[data-testid="word-morphology-lemma-link"]') as HTMLAnchorElement | null;
    const stemLink = root.querySelector('[data-testid="word-morphology-stem-link"]') as HTMLAnchorElement | null;

    expect(lemmaLink?.getAttribute('href')).toContain(`qdDetail=${encodeURIComponent(LEMMA_SERIALIZED)}`);
    expect(lemmaLink?.getAttribute('href')).toContain('qdDetailOpen=1');
    expect(lemmaLink?.getAttribute('target')).toBeNull();
    expect(lemmaLink?.getAttribute('rel')).toBeNull();
    expect(stemLink?.getAttribute('href')).toContain(`qdDetail=${encodeURIComponent(STEM_SERIALIZED)}`);
    expect(stemLink?.getAttribute('href')).toContain('qdDetailOpen=1');
    expect(stemLink?.getAttribute('target')).toBeNull();
    expect(stemLink?.getAttribute('rel')).toBeNull();
  });

  it('renders the type label as a detail-overlay link when a word-type frame is provided', () => {
    const fixture = TestBed.createComponent(WordMorphologySummaryComponent);
    setInputs(fixture, {
      morphology: buildMorphology(null),
      wordTypeFrame: WORD_TYPE_FRAME,
    });

    const root = fixture.nativeElement as HTMLElement;
    const link = root.querySelector(
      '[data-testid="word-morphology-type-link"]',
    ) as HTMLAnchorElement | null;

    expect(link).toBeTruthy();
    const href = link?.getAttribute('href') ?? '';
    expect(href).toContain('/dashboard/mushaf');
    expect(href).toContain(`qdDetail=${encodeURIComponent(WORD_TYPE_SERIALIZED)}`);
    expect(href).toContain('qdDetailOpen=1');
    expect(link?.getAttribute('target')).toBeNull();
    expect(link?.getAttribute('rel')).toBeNull();
    expect(link?.getAttribute('aria-label')).toBe('افتح نوع الكلمة في بطاقة التفاصيل');
    expect(link?.textContent).toContain('نوع الكلمة');
    expect(link?.textContent).toContain('فعل');
  });

  it('keeps the type column as plain text when no word-type frame is provided', () => {
    const fixture = TestBed.createComponent(WordMorphologySummaryComponent);
    setInputs(fixture, {
      morphology: buildMorphology(null),
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="word-morphology-type-link"]')).toBeNull();
    expect(root.textContent).toContain('نوع الكلمة');
    expect(root.textContent).toContain('فعل');
  });

  it('renders static root, lemma, and stem columns when frames are absent', () => {
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
