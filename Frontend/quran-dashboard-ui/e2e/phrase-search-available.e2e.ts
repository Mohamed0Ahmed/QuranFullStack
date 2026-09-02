import { execFileSync } from 'node:child_process';
import { resolve } from 'node:path';
import type { APIRequestContext, Page } from '@playwright/test';

import oracleData from '../../../test-artifacts/compact-phrase-search-ready/oracle.json';
import manifestData from '../../../test-artifacts/compact-phrase-search-ready/manifest.json';
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
  artifactId: string;
  phraseSearch: {
    activeBuildId: string;
    sourceFingerprint: string;
    readiness: 'available';
    query: {
      raw: string;
      displayText: string;
      exactTokenIds: number[];
    };
    repetitions: {
      displayText: string;
      verseKeys: string[];
    };
    context: {
      verseKeys: string[];
      selectedVerseKey: string;
      selectedQuranWordIds: number[];
    };
    similarity: {
      maximumDifferences: number;
      verseKeys: string[];
      nonIdenticalVerseKey: string;
    };
  };
}

interface PhraseReadyManifest {
  artifactId: string;
  phraseSearch: {
    sourceFingerprint: string;
    readiness: 'available';
    activeBuildId: string;
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
const manifest = manifestData as PhraseReadyManifest;

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
      expect(oracle.artifactId).toBe('compact-phrase-search-ready');
      expect(manifest.artifactId).toBe(oracle.artifactId);
      expect(manifest.phraseSearch).toEqual({
        activeBuildId: oracle.phraseSearch.activeBuildId,
        sourceFingerprint: oracle.phraseSearch.sourceFingerprint,
        readiness: oracle.phraseSearch.readiness,
      });

      const capabilitiesBefore = await readPublicData<PhraseCapabilities>(
        request,
        '/api/quran/phrase-search/capabilities',
        'ready PhraseSearch capabilities before the journey',
      );
      expect(capabilitiesBefore).toMatchObject({
        activeBuildId: manifest.phraseSearch.activeBuildId,
        exactReady: true,
        similarityReady: true,
      });
      expect(capabilitiesBefore.modes).toContainEqual(
        expect.objectContaining({
          mode: 'simple',
          supportedLengths: expect.arrayContaining([oracle.phraseSearch.query.exactTokenIds.length]),
          similarityLengths: expect.arrayContaining([oracle.phraseSearch.query.exactTokenIds.length]),
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
  await query.fill(oracle.phraseSearch.query.raw);
  await page.getByRole('button', { name: 'بحث', exact: true }).click();

  await expect(page.getByText(oracle.phraseSearch.query.displayText, { exact: true })).toBeVisible();
  await expect(page.getByLabel('ملخص نتائج السياق')).toContainText(
    `${oracle.phraseSearch.context.verseKeys.length} آية`,
  );
  for (const verseKey of oracle.phraseSearch.context.verseKeys) {
    await expect(page.getByRole('link', { name: `فتح الآية ${verseKey} في المصحف` })).toBeVisible();
  }

  const selectedVerseKey = oracle.phraseSearch.context.selectedVerseKey;
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
    label: `البحث عن «${oracle.phraseSearch.query.raw}»`,
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
    oracle.phraseSearch.context.selectedQuranWordIds.map((quranWordId) => ({
      ayahId: persistedAyahId,
      quranWordId,
    })),
  );
  await accessibility.expectNoBlockingViolations(page);
}

async function exerciseRepetitions(page: Page, accessibility: AccessibilityAudit): Promise<void> {
  const phraseLength = oracle.phraseSearch.query.exactTokenIds.length;
  await page.goto(
    `${phraseSearchRoutePath(WORDS_PHRASES_REPETITIONS_SEGMENT)}?length=${phraseLength}`,
  );
  await expect(page.getByLabel('عدد كلمات العبارة')).toHaveValue(String(phraseLength));
  const search = page.getByRole('search');
  await search.getByRole('searchbox', {
    name: `بحث داخل عبارات ${phraseLength} كلمات`,
  }).fill(oracle.phraseSearch.query.raw);
  await search.getByRole('button', { name: 'بحث', exact: true }).click();

  const phrase = page.getByText(oracle.phraseSearch.repetitions.displayText, { exact: true });
  await expect(phrase).toBeVisible();
  await phrase.click();
  for (const verseKey of oracle.phraseSearch.repetitions.verseKeys) {
    await expect(page.getByRole('link', { name: `فتح الآية ${verseKey} في المصحف` })).toBeVisible();
  }
  await accessibility.expectNoBlockingViolations(page);
}

async function exerciseSimilarity(page: Page, accessibility: AccessibilityAudit): Promise<void> {
  await page.goto(phraseSearchRoutePath(WORDS_PHRASES_SIMILARITY_SEGMENT));
  const query = page.getByRole('textbox', { name: 'العبارة المرجعية' });
  await query.fill(oracle.phraseSearch.query.raw);
  await page.getByRole('button', { name: 'بحث', exact: true }).click();

  const comparisonRange = page.getByLabel('مدى المقارنة');
  await comparisonRange.selectOption(String(oracle.phraseSearch.similarity.maximumDifferences));
  await expect(page.getByLabel('ملخص النتائج')).toContainText(
    `${oracle.phraseSearch.similarity.verseKeys.length} آية`,
  );
  for (const verseKey of oracle.phraseSearch.similarity.verseKeys) {
    await expect(page.getByRole('link', { name: `فتح الآية ${verseKey} في المصحف` })).toBeVisible();
  }
  await expect(
    page.getByRole('link', {
      name: `فتح الآية ${oracle.phraseSearch.similarity.nonIdenticalVerseKey} في المصحف`,
    }),
  ).toContainText('مَجْر۪ىٰهَا');
  await accessibility.expectNoBlockingViolations(page);
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
