import type { APIRequestContext, Page } from '@playwright/test';

import oracleData from '../../../test-oracles/phrase-search.json';
import {
  WORDS_PHRASES_CONTEXT_SEGMENT,
  WORDS_PHRASES_REPETITIONS_SEGMENT,
  WORDS_PHRASES_SIMILARITY_SEGMENT,
  phraseSearchRoutePath,
} from '../src/app/core/navigation/route-paths';
import { readApiData } from './fixtures/api-envelope';
import { createAccessibilityAudit } from './fixtures/accessibility';
import { expect, test } from './fixtures/app-test';

const API_ORIGIN = 'https://localhost:5015';

interface PhraseSearchOracle {
  query: {
    raw: string;
    displayText: string;
    wordCount: number;
  };
  repetitions: {
    displayText: string;
    verseKeys: string[];
  };
  context: {
    verseKeys: string[];
    selectedVerseKey: string;
  };
  similarity: {
    maximumDifferences: number;
    verseKeys: string[];
    nonIdenticalVerseKey: string;
    nonIdenticalVisibleText: string;
  };
}

interface PhraseCapabilities {
  exactReady: boolean;
  similarityReady: boolean;
  modes: Array<{
    mode: string;
    supportedLengths: number[];
    similarityLengths: number[];
  }>;
}

const oracle = oracleData as PhraseSearchOracle;

test(
  'a visitor follows the source-reviewed PhraseSearch read path',
  {
    annotation: [
      { type: 'critical' },
      { type: 'canonical-read' },
      { type: 'fixture-policy', description: 'canonical-read-only' },
      { type: 'journey', description: 'phrase-search.canonical-read' },
    ],
  },
  async ({ page, request }, testInfo) => {
    const capabilities = await readCapabilities(request);
    expect(capabilities).toMatchObject({ exactReady: true, similarityReady: true });
    expect(capabilities.modes).toContainEqual(
      expect.objectContaining({
        mode: 'simple',
        supportedLengths: expect.arrayContaining([oracle.query.wordCount]),
        similarityLengths: expect.arrayContaining([oracle.query.wordCount]),
      }),
    );

    const accessibility = createAccessibilityAudit(testInfo);
    try {
      await exerciseRepetitions(page);
      await accessibility.expectNoBlockingViolations(page);
      await exerciseContext(page);
      await accessibility.expectNoBlockingViolations(page);
      await exerciseSimilarity(page);
      await accessibility.expectNoBlockingViolations(page);
    } finally {
      await accessibility.attachObservations();
    }
  },
);

async function readCapabilities(request: APIRequestContext): Promise<PhraseCapabilities> {
  const response = await request.get(`${API_ORIGIN}/api/quran/phrase-search/capabilities`);
  return readApiData<PhraseCapabilities>(response, 'canonical PhraseSearch capabilities', 200);
}

async function exerciseRepetitions(page: Page): Promise<void> {
  await page.goto(
    `${phraseSearchRoutePath(WORDS_PHRASES_REPETITIONS_SEGMENT)}?length=${oracle.query.wordCount}`,
  );
  await page
    .getByRole('searchbox', { name: `بحث داخل عبارات ${oracle.query.wordCount} كلمات` })
    .fill(oracle.query.raw);
  await page.getByRole('button', { name: 'بحث', exact: true }).click();

  const phrase = page.getByText(oracle.repetitions.displayText, { exact: true });
  await expect(phrase).toBeVisible();
  await phrase.click();
  for (const verseKey of oracle.repetitions.verseKeys) {
    await expect(page.getByRole('link', { name: `فتح الآية ${verseKey} في المصحف` })).toBeVisible();
  }
}

async function exerciseContext(page: Page): Promise<void> {
  await page.goto(phraseSearchRoutePath(WORDS_PHRASES_CONTEXT_SEGMENT));
  await page.getByRole('textbox', { name: 'العبارة المراد استكشاف سياقها' }).fill(oracle.query.raw);
  await page.getByRole('button', { name: 'بحث', exact: true }).click();

  await expect(page.getByText(oracle.query.displayText, { exact: true })).toBeVisible();
  await expect(page.getByLabel('ملخص نتائج السياق')).toContainText(
    `${oracle.context.verseKeys.length} آية`,
  );
  for (const verseKey of oracle.context.verseKeys) {
    await expect(page.getByRole('link', { name: `فتح الآية ${verseKey} في المصحف` })).toBeVisible();
  }

  const selectedMushafLink = page.getByRole('link', {
    name: `فتح الآية ${oracle.context.selectedVerseKey} في المصحف`,
  });
  const href = new URL((await selectedMushafLink.getAttribute('href')) ?? '', page.url());
  expect(href.pathname).toBe('/dashboard/mushaf');
  expect(href.searchParams.get('ayah')).toBe(oracle.context.selectedVerseKey);
  expect(href.searchParams.get('focusAyah')).toBe(oracle.context.selectedVerseKey);
  expect(href.searchParams.get('panel')).toBe('ayah');
}

async function exerciseSimilarity(page: Page): Promise<void> {
  await page.goto(phraseSearchRoutePath(WORDS_PHRASES_SIMILARITY_SEGMENT));
  await page.getByRole('textbox', { name: 'العبارة المرجعية' }).fill(oracle.query.raw);
  await page.getByRole('button', { name: 'بحث', exact: true }).click();
  await page.getByLabel('مدى المقارنة').selectOption(String(oracle.similarity.maximumDifferences));

  await expect(page.getByLabel('ملخص النتائج')).toContainText(
    `${oracle.similarity.verseKeys.length} آية`,
  );
  for (const verseKey of oracle.similarity.verseKeys) {
    await expect(page.getByRole('link', { name: `فتح الآية ${verseKey} في المصحف` })).toBeVisible();
  }
  await expect(
    page.getByRole('link', {
      name: `فتح الآية ${oracle.similarity.nonIdenticalVerseKey} في المصحف`,
    }),
  ).toContainText(oracle.similarity.nonIdenticalVisibleText);
}
