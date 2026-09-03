import { execFileSync } from 'node:child_process';
import { resolve } from 'node:path';
import type { APIRequestContext, Page } from '@playwright/test';
import type { PhraseSimilarityLinkingSelectionResponse } from '../src/app/core/api/generated/models/phrase-similarity-linking-selection-response';

import oracleData from '../../../test-oracles/phrase-search.json';
import {
  WORDS_PHRASES_CONTEXT_SEGMENT,
  WORDS_PHRASES_REPETITIONS_SEGMENT,
  WORDS_PHRASES_SIMILARITY_SEGMENT,
  phraseSearchRoutePath,
} from '../src/app/core/navigation/route-paths';
import { environment } from '../src/environments/environment.development';
import { readApiData } from './fixtures/api-envelope';
import {
  createAccessibilityAudit,
  type AccessibilityAudit,
} from './fixtures/accessibility';
import { expect, test } from './fixtures/auth';

const API_ORIGIN = environment.apiBaseUrl;
const PREPARE_LINKING = resolve(process.cwd(), 'e2e/prepare-linking.mjs');

interface PhraseReadyOracle {
  query: {
    raw: string;
    displayText: string;
    wordCount: number;
    selectedQuranWordIds: number[];
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
  activeBuildId: string;
  exactReady: boolean;
  similarityReady: boolean;
  modes: Array<{
    mode: string;
    supportedLengths: number[];
    similarityLengths: number[];
  }>;
}

interface LinkingWorkspace {
  sources: Array<{
    sourceIdentity: string;
    descriptor: {
      kind: string | null;
      label: string | null;
      contextKey: string | null;
      manualAyahs: Array<{ verseKey: string | null }> | null;
    };
    manualAyahs: Array<{
      ayahId: number;
      orderValue: number;
      pageHint: number | null;
      verseKey: string;
    }>;
    manualLinkShape: string | null;
    selectedWords: Array<{
      ayahId: number | null;
      quranWordId: number | null;
    }>;
  }>;
}

const oracle = oracleData as PhraseReadyOracle;

test(
  'an Owner follows the reviewed PhraseSearch path and persists Add to Workspace',
  {
    annotation: [
      { type: 'critical' },
      { type: 'mutating' },
      { type: 'artifact', description: 'compact-phrase-search-ready' },
      { type: 'journey', description: 'phrase-search.available-add-to-workspace' },
    ],
  },
  async ({ page, request, ownerPersona }, testInfo) => {
    const accessibility = createAccessibilityAudit(testInfo);
    try {
      prepareLinking();

      const capabilitiesBefore = await readPublicData<PhraseCapabilities>(
        request,
        '/api/quran/phrase-search/capabilities',
        'ready PhraseSearch capabilities before the journey',
      );
      expect(capabilitiesBefore).toMatchObject({
        exactReady: true,
        similarityReady: true,
      });
      expect(capabilitiesBefore.activeBuildId).not.toBe('');
      expect(capabilitiesBefore.modes).toContainEqual(
        expect.objectContaining({
          mode: 'simple',
          supportedLengths: expect.arrayContaining([oracle.query.wordCount]),
          similarityLengths: expect.arrayContaining([oracle.query.wordCount]),
        }),
      );

      await exerciseRepetitions(page, accessibility);
      await exerciseContextAndPersist(page, request, ownerPersona.accessToken, accessibility);
      await exerciseSimilarity(page, accessibility);

      const capabilitiesAfter = await readPublicData<PhraseCapabilities>(
        request,
        '/api/quran/phrase-search/capabilities',
        'ready PhraseSearch capabilities after the journey',
      );
      expect(capabilitiesAfter.activeBuildId).toBe(capabilitiesBefore.activeBuildId);
      expect(capabilitiesAfter).toEqual(capabilitiesBefore);
    } finally {
      await accessibility.attachObservations();
    }
  },
);

function prepareLinking(): void {
  execFileSync(process.execPath, [PREPARE_LINKING], { stdio: 'inherit' });
}

async function exerciseContextAndPersist(
  page: Page,
  request: APIRequestContext,
  ownerAccessToken: string,
  accessibility: AccessibilityAudit,
): Promise<void> {
  await page.goto(phraseSearchRoutePath(WORDS_PHRASES_CONTEXT_SEGMENT));
  const query = page.getByRole('textbox', { name: 'العبارة المراد استكشاف سياقها' });
  await query.fill(oracle.query.raw);
  await page.getByRole('button', { name: 'بحث', exact: true }).click();

  await expect(page.getByText(oracle.query.displayText, { exact: true })).toBeVisible();
  await expect(page.getByLabel('ملخص نتائج السياق')).toContainText(
    `${oracle.context.verseKeys.length} آية`,
  );
  for (const verseKey of oracle.context.verseKeys) {
    await expect(page.getByRole('link', { name: `فتح الآية ${verseKey} في المصحف` })).toBeVisible();
  }

  const selectedVerseKey = oracle.context.selectedVerseKey;
  expect(selectedVerseKey).toBe('1:1');
  const selectedMushafLink = page.getByRole('link', {
    name: `فتح الآية ${selectedVerseKey} في المصحف`,
  });
  const selectedMushafHref = new URL(await selectedMushafLink.getAttribute('href') ?? '', page.url());
  expect(selectedMushafHref.pathname).toBe('/dashboard/mushaf');
  expect(selectedMushafHref.searchParams.get('ayah')).toBe(selectedVerseKey);
  expect(selectedMushafHref.searchParams.get('focusAyah')).toBe(selectedVerseKey);
  expect(selectedMushafHref.searchParams.get('panel')).toBe('ayah');
  expect(Number(selectedMushafHref.searchParams.get('page'))).toBeGreaterThan(0);

  await selectedMushafLink.click();
  await expect
    .poll(() => {
      const url = new URL(page.url());
      return {
        ayah: url.searchParams.get('ayah'),
        page: Number(url.searchParams.get('page')),
        panel: url.searchParams.get('panel'),
      };
    })
    .toEqual({ ayah: selectedVerseKey, page: Number(selectedMushafHref.searchParams.get('page')), panel: 'ayah' });
  await expect(page.getByTestId('mushaf-reader-page')).toHaveAttribute('dir', 'rtl');
  await expect(page.getByTestId('selected-ayah-section')).toBeVisible();

  await page.goBack();
  await expect(page.getByRole('checkbox', { name: `تحديد الآية ${selectedVerseKey}` })).toBeVisible();
  const selectedAyah = page.getByRole('checkbox', { name: `تحديد الآية ${selectedVerseKey}` });
  await selectedAyah.check();

  const addToWorkspace = page.getByRole('button', {
    name: 'إضافة للربط: 1 آية محددة',
    exact: true,
  });
  await addToWorkspace.focus();
  await expect(addToWorkspace).toBeFocused();
  const persistenceResponse = page.waitForResponse(
    (response) =>
      response.request().method() === 'POST'
      && new URL(response.url()).pathname === '/api/linking/workspace/sources',
  );
  await addToWorkspace.press('Enter');
  expect((await persistenceResponse).status()).toBe(200);

  const workspace = await readAuthorizedData<LinkingWorkspace>(
    request,
    ownerAccessToken,
    '/api/linking/workspace',
    'independent Linking workspace read after Add to Workspace',
  );
  expect(workspace.sources).toHaveLength(1);
  const persistedSource = workspace.sources[0];
  expect(persistedSource?.sourceIdentity).toBe('manual-mushaf-ayahs|1%3A1');
  expect(persistedSource?.descriptor).toEqual({
    contextKey: null,
    kind: 'manual-mushaf-ayahs',
    label: `البحث عن «${oracle.query.raw}»`,
    lemmaId: null,
    manualAyahs: [{ verseKey: selectedVerseKey }],
    mode: null,
    rootId: null,
    selection: null,
    stemId: null,
    typeCode: null,
    typeCodes: null,
    wordId: null,
  });
  expect(persistedSource?.manualLinkShape).toBe('independent');
  expect(persistedSource?.manualAyahs).toEqual([
    expect.objectContaining({ ayahId: expect.any(Number), verseKey: selectedVerseKey }),
  ]);
  const persistedAyahId = persistedSource?.manualAyahs[0]?.ayahId;
  expect(persistedAyahId).toBeGreaterThan(0);
  expect(persistedSource?.selectedWords).toEqual(
    oracle.query.selectedQuranWordIds.map((quranWordId) => ({
      ayahId: persistedAyahId,
      quranWordId,
    })),
  );

  await page
    .getByRole('checkbox', { name: 'تحديد كل الآيات المطابقة', exact: true })
    .check();
  await expect(page.getByLabel('ملخص نتائج السياق')).toContainText(
    `${oracle.context.verseKeys.length} آية محددة`,
  );
  const validContextUrl = page.url();
  const invalidContextUrl = new URL(validContextUrl);
  invalidContextUrl.searchParams.set('contextsPage', '0');
  await pushClientUrl(page, invalidContextUrl.toString());
  await expect(page.getByRole('button', { name: 'بدء بحث جديد', exact: true })).toBeVisible();
  await pushClientUrl(page, validContextUrl);
  await expect(page.getByLabel('ملخص نتائج السياق')).toContainText('0 آية محددة');
  await accessibility.expectNoBlockingViolations(page);
}

async function exerciseRepetitions(page: Page, accessibility: AccessibilityAudit): Promise<void> {
  const phraseLength = oracle.query.wordCount;
  await page.goto(
    `${phraseSearchRoutePath(WORDS_PHRASES_REPETITIONS_SEGMENT)}?length=${phraseLength}`,
  );
  await expect(page.getByLabel('عدد كلمات العبارة')).toHaveValue(String(phraseLength));
  const search = page.getByRole('search');
  await search.getByRole('searchbox', {
    name: `بحث داخل عبارات ${phraseLength} كلمات`,
  }).fill(oracle.query.raw);
  await search.getByRole('button', { name: 'بحث', exact: true }).click();

  const phrase = page.getByText(oracle.repetitions.displayText, { exact: true });
  await expect(phrase).toBeVisible();
  await phrase.click();
  for (const verseKey of oracle.repetitions.verseKeys) {
    await expect(page.getByRole('link', { name: `فتح الآية ${verseKey} في المصحف` })).toBeVisible();
  }
  await accessibility.expectNoBlockingViolations(page);
}

async function exerciseSimilarity(page: Page, accessibility: AccessibilityAudit): Promise<void> {
  await page.goto(phraseSearchRoutePath(WORDS_PHRASES_SIMILARITY_SEGMENT));
  const query = page.getByRole('textbox', { name: 'العبارة المرجعية' });
  await query.fill(oracle.query.raw);
  await page.getByRole('button', { name: 'بحث', exact: true }).click();

  const comparisonRange = page.getByLabel('مدى المقارنة');
  await comparisonRange.selectOption(String(oracle.similarity.maximumDifferences));
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

  const selectAll = page
    .getByRole('table', { name: 'جدول الآيات المطابقة والمتشابهة', exact: true })
    .getByRole('checkbox', { name: 'تحديد كل آيات نتائج التشابه', exact: true });
  await selectAll.check();
  const excludedVerseKey = oracle.similarity.nonIdenticalVerseKey;
  const excludedAyah = page.getByRole('checkbox', { name: `تحديد الآية ${excludedVerseKey}` });
  const excludedAyahId = Number(await excludedAyah.getAttribute('data-ayah-id'));
  expect(excludedAyahId).toBeGreaterThan(0);
  await excludedAyah.uncheck();

  const staleIntercepted = deferred<void>();
  const staleRelease = deferred<void>();
  const staleSettled = deferred<void>();
  let requestCount = 0;
  let freshSelection: PhraseSimilarityLinkingSelectionResponse | null = null;
  let freshRequestBody: unknown = null;
  await page.route('**/api/quran/phrase-search/similarities/linking-selection', async (route) => {
    requestCount += 1;
    const response = await route.fetch();
    if (requestCount === 1) {
      expect(route.request().postDataJSON()).toEqual({
        ayahIds: [excludedAyahId],
        minimumMatchedWords:
          oracle.query.wordCount - oracle.similarity.maximumDifferences,
        resolutionRef: expect.any(String),
        selectionMode: 'all-except',
      });
      staleIntercepted.resolve();
      await staleRelease.promise;
      await route.fulfill({ response });
      staleSettled.resolve();
      return;
    }
    freshRequestBody = route.request().postDataJSON();
    const envelope = await response.json() as {
      data: PhraseSimilarityLinkingSelectionResponse | null;
    };
    freshSelection = envelope.data;
    await route.fulfill({ response });
  });

  const direct = page.getByRole('button', { name: /^ربط مباشر: \d+ آية محددة$/u });
  await direct.click();
  await staleIntercepted.promise;
  await excludedAyah.check();
  staleRelease.resolve();
  await staleSettled.promise;
  await expect(page.getByRole('dialog', { name: 'ربط مباشر', exact: true })).toBeHidden();

  const sourcePageRequest = page.waitForRequest(
    (request) =>
      request.method() === 'POST' &&
      new URL(request.url()).pathname === '/api/linking/sources/resolve-page',
  );
  await direct.click();
  const dialog = page.getByRole('dialog', { name: 'ربط مباشر', exact: true });
  await expect(dialog).toBeVisible();
  const acceptedSelection = freshSelection as PhraseSimilarityLinkingSelectionResponse | null;
  if (acceptedSelection === null) {
    throw new Error('The fresh Similarity selection response was not captured.');
  }
  expect(freshRequestBody).toEqual({
    ayahIds: [],
    minimumMatchedWords:
      oracle.query.wordCount - oracle.similarity.maximumDifferences,
    resolutionRef: expect.any(String),
    selectionMode: 'all-except',
  });
  const canonicalAyahs = [...acceptedSelection.ayahs].sort(
    (left, right) => compareVerseKeys(left.verseKey, right.verseKey) || left.ayahId - right.ayahId,
  );
  expect(acceptedSelection.selectedAyahCount).toBe(
    oracle.similarity.verseKeys.length,
  );
  expect(canonicalAyahs.map((ayah) => ayah.verseKey)).toEqual(
    [...oracle.similarity.verseKeys].sort(compareVerseKeys),
  );
  for (const ayah of canonicalAyahs) {
    expect(ayah.selectedQuranWordIds).toEqual(
      [...ayah.selectedQuranWordIds].sort((left, right) => left - right),
    );
  }
  const sourceRequestBody = (await sourcePageRequest).postDataJSON() as {
    descriptor: {
      contextKey: string | null;
      kind: string | null;
      label: string | null;
      manualAyahs: Array<{ verseKey: string | null }> | null;
    };
  };
  expect(sourceRequestBody.descriptor).toMatchObject({
    contextKey: null,
    kind: 'manual-mushaf-ayahs',
    label: `متشابهات العبارة «${oracle.query.displayText}»`,
    manualAyahs: canonicalAyahs.map((ayah) => ({ verseKey: ayah.verseKey })),
  });
  await expect(dialog).toContainText(`متشابهات العبارة «${oracle.query.displayText}»`);
  await accessibility.expectNoBlockingViolations(page);
}

function deferred<T>(): { promise: Promise<T>; resolve: (value: T) => void } {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((resolvePromise) => {
    resolve = resolvePromise;
  });
  return { promise, resolve };
}

function compareVerseKeys(left: string, right: string): number {
  const [leftSurah, leftAyah] = left.split(':').map(Number);
  const [rightSurah, rightAyah] = right.split(':').map(Number);
  return leftSurah - rightSurah || leftAyah - rightAyah;
}

async function pushClientUrl(page: Page, url: string): Promise<void> {
  await page.evaluate((nextUrl) => {
    window.history.pushState({}, '', nextUrl);
    window.dispatchEvent(new PopStateEvent('popstate'));
  }, url);
}

function readAuthorizedData<T>(
  request: APIRequestContext,
  ownerAccessToken: string,
  path: string,
  operation: string,
): Promise<T> {
  return readRequestData<T>(request, path, operation, {
    Authorization: `Bearer ${ownerAccessToken}`,
  });
}

function readPublicData<T>(
  request: APIRequestContext,
  path: string,
  operation: string,
): Promise<T> {
  return readRequestData<T>(request, path, operation, {});
}

async function readRequestData<T>(
  request: APIRequestContext,
  path: string,
  operation: string,
  headers: Record<string, string>,
): Promise<T> {
  const response = await request.get(`${API_ORIGIN}${path}`, { headers });
  return readApiData<T>(response, operation, 200);
}
