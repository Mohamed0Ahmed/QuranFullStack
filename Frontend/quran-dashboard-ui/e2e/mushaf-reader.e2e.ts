import { devices, type Page } from '@playwright/test';

import { expectNoBlockingAccessibilityViolations } from './fixtures/accessibility';
import { expect, test } from './fixtures/app-test';
import { openReader } from './fixtures/mushaf';
import oracleData from '../../../test-artifacts/compact-cross-stack-base/oracle.json';

const API_ORIGIN = 'https://localhost:5015';

interface OracleWord {
  location: string;
  verseKey: string;
  textUthmani: string;
  isAyahMarker: boolean;
}

interface PageOneOracle {
  contractVersion: number;
  artifactId: string;
  pageNumber: number;
  study: {
    verseKey: string;
    tafsir: {
      sourceKey: string;
      displayNameAr: string;
      text: string;
    };
    translation: {
      sourceKey: string;
      displayNameAr: string;
      displayNameEn: string;
      text: string;
    };
  };
  ayahs: Array<{
    verseKey: string;
    textUthmani: string;
    wordLocations: string[];
  }>;
  lines: Array<{
    lineNumber: number;
    lineType: string;
    isCentered: boolean;
    surahNumber: number | null;
    wordLocations: string[];
  }>;
  words: OracleWord[];
}

interface MushafPageEnvelope {
  isSuccess: boolean;
  data: {
    pageNumber: number;
    lines: Array<{
      lineNumber: number;
      lineType: string;
      isCentered: boolean;
      surahNumber: number | null;
      words: Array<{
        wordLocation: string;
        verseKey: string;
        textUthmani: string;
        isAyahMarker: boolean;
      }>;
    }>;
  } | null;
}

const pageOneOracle = oracleData as PageOneOracle;
const {
  defaultBrowserType: _approvedBrowserType,
  ...APPROVED_MUSHAF_MOBILE
} = devices['Pixel 7'];
const REPLACEMENT_GLYPH = '\uFFFD';

async function expectRenderedOracle(page: Page): Promise<void> {
  const words = page.locator('[data-word-location]');
  await expect(words.first()).toBeVisible();
  const renderedWords = await words.evaluateAll((elements) =>
    elements.map((element) => ({
      location: element.getAttribute('data-word-location'),
      verseKey: element.getAttribute('data-verse-key'),
      textUthmani: element.textContent?.trim() ?? null,
      isAyahMarker: element.getAttribute('data-is-marker') === 'true',
    })),
  );
  expect(renderedWords).toEqual(
    pageOneOracle.words.map((word) => ({
      location: word.location,
      verseKey: word.verseKey,
      textUthmani: word.textUthmani,
      isAyahMarker: word.isAyahMarker,
    })),
  );
  expect(renderedWords.map((word) => word.textUthmani).join('')).not.toContain(
    REPLACEMENT_GLYPH,
  );

  const renderedLines = await page.getByTestId('mushaf-line').evaluateAll((elements) =>
    elements.map((element) => ({
      lineNumber: Number(element.getAttribute('data-line-number')),
      lineType: element.getAttribute('data-line-type'),
      isCentered: element.getAttribute('data-is-centered') === 'true',
      surahNumber: element.hasAttribute('data-surah-number')
        ? Number(element.getAttribute('data-surah-number'))
        : null,
      wordLocations: (element.getAttribute('data-word-locations') ?? '')
        .split(' ')
        .filter(Boolean),
    })),
  );
  expect(renderedLines).toEqual(pageOneOracle.lines);
}

async function expectLocalMushafFontsReady(page: Page): Promise<void> {
  const expectedFonts = [
    {
      selector: '[data-word-location][data-is-marker="false"]',
      family: 'Amiri',
      path: '/fonts/amiri-regular.woff2',
    },
    {
      selector: '[data-word-location][data-is-marker="true"]',
      family: 'Uthmanic Hafs',
      path: '/assets/fonts/quran/UthmanicHafs_V22.ttf',
    },
    {
      selector: '[data-testid="mushaf-page-surah-glyph"]',
      family: 'Mushaf Surah Name',
      path: '/fonts/mushaf/surah-name-v1.woff2',
    },
    {
      selector: '[data-testid="mushaf-page-juz-glyph"]',
      family: 'Mushaf Common',
      path: '/fonts/mushaf/quran-common.woff2',
    },
  ] as const;

  await page.evaluate(async () => {
    await document.fonts.ready;
  });
  await expect
    .poll(() => page.evaluate(() => document.fonts.status), {
      message: 'local Mushaf fonts should finish loading',
    })
    .toBe('loaded');

  for (const expectedFont of expectedFonts) {
    const element = page.locator(expectedFont.selector).first();
    await expect(element).toBeVisible();
    const readiness = await element.evaluate((node, expected) => {
      const loadedFaces = Array.from(document.fonts)
        .filter((face) => face.status === 'loaded')
        .map((face) => face.family.replaceAll(/["']/g, ''));
      const fontResource = performance
        .getEntriesByType('resource')
        .filter((entry): entry is PerformanceResourceTiming => entry instanceof PerformanceResourceTiming)
        .find((entry) => new URL(entry.name).pathname === expected.path);
      return {
        computedFamily: getComputedStyle(node).fontFamily,
        glyphsCovered: document.fonts.check(
          `16px "${expected.family}"`,
          node.textContent ?? '',
        ),
        loadedFace: loadedFaces.includes(expected.family),
        resourceOrigin: fontResource ? new URL(fontResource.name).origin : null,
        resourceStatus: fontResource?.responseStatus ?? null,
      };
    }, expectedFont);
    expect(readiness.computedFamily).toContain(expectedFont.family);
    expect(readiness.loadedFace, `${expectedFont.family} FontFace should be loaded`).toBe(true);
    expect(readiness.glyphsCovered, `${expectedFont.family} should cover rendered glyphs`).toBe(true);
    expect(readiness.resourceOrigin).toBe(new URL(page.url()).origin);
    expect(readiness.resourceStatus).toBe(200);
  }
}

async function expectReviewedStudySourcesVisible(page: Page): Promise<void> {
  await page.getByTestId('study-context-tab-sources').click();
  await expect(page.getByTestId('selected-ayah-section')).toBeVisible();
  await expect(page.getByTestId('source-single-option')).toHaveText(
    pageOneOracle.study.tafsir.displayNameAr,
  );
  await expect(page.getByTestId('tafsir-card')).toContainText('أبتدئ قراءة القرآن باسم الله');

  await page.getByRole('tab', { name: 'الترجمة', exact: true }).click();
  await expect(page.getByTestId('source-single-option')).toHaveText(
    `${pageOneOracle.study.translation.displayNameAr} (بملاحظات)`,
  );
  await expect(page.getByTestId('translation-card')).toHaveText(
    pageOneOracle.study.translation.text,
  );
}

test('the reader opens on page 1 and renders the Mushaf page', async ({ page }) => {
  await page.goto('/dashboard/mushaf');

  await expect(page.getByTestId('mushaf-reader-page')).toHaveAttribute('dir', 'rtl');
  await expect(page.getByTestId('mushaf-page-area')).toBeVisible();
  await expect(page.getByTestId('mushaf-page-view')).toBeVisible();
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('1');
});

test('next then previous returns to the starting page', async ({ page }) => {
  await page.goto('/dashboard/mushaf');
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('1');

  await page.getByTestId('mushaf-next-page').click();

  await expect(page).toHaveURL(/[?&]page=2(&|$)/);
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('2');

  await page.getByTestId('mushaf-prev-page').click();

  // In-app paging writes the page key unconditionally (mushaf-reader.facade.ts changePage);
  // only session restoration omits the default. A redundant page=1 still hydrates page 1.
  await expect(page).toHaveURL(/[?&]page=1(&|$)/);
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('1');
});

test('the first word tap after a touch swipe selects the word on mobile and tablet', async ({ page }) => {
  for (const viewport of [
    { width: 390, height: 844 },
    { width: 768, height: 900 },
  ]) {
    await page.setViewportSize(viewport);
    await page.goto('/dashboard/mushaf?page=1');
    await expect(page.getByTestId('mushaf-page-view')).toBeVisible();

    const pageView = page.getByTestId('mushaf-page-view');
    await pageView.dispatchEvent('pointerdown', {
      pointerType: 'touch',
      pointerId: 1,
      isPrimary: true,
      clientX: 120,
      clientY: 240,
    });
    await pageView.dispatchEvent('pointerup', {
      pointerType: 'touch',
      pointerId: 1,
      isPrimary: true,
      clientX: 200,
      clientY: 240,
    });
    await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('2');

    const word = page.locator('[data-word-location][data-is-marker="false"]').first();
    await word.dispatchEvent('pointerdown', {
      pointerType: 'touch',
      pointerId: 2,
      isPrimary: true,
      clientX: 120,
      clientY: 240,
    });
    await word.dispatchEvent('pointerup', {
      pointerType: 'touch',
      pointerId: 2,
      isPrimary: true,
      clientX: 120,
      clientY: 240,
    });
    await word.dispatchEvent('click');

    await expect(page).toHaveURL(/[?&]word=/);
    await expect(word).toHaveAttribute('aria-current', 'true');
  }
});

test('a page deep link hydrates the reader', async ({ page }) => {
  await page.goto('/dashboard/mushaf?page=5');

  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('5');
  await expect(page.getByTestId('mushaf-page-view')).toBeVisible();
  await expect(page).toHaveURL(/[?&]page=5(&|$)/);
});

test('the surah jump picker moves the reader to another page', async ({ page }) => {
  await page.goto('/dashboard/mushaf');
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('1');

  await page.getByTestId('surah-jump-picker-trigger').click();
  await expect(page.getByTestId('surah-jump-picker-panel')).toBeVisible();

  await page.getByTestId('surah-jump-picker-search').fill('البقرة');
  await page.getByTestId('surah-jump-picker-row').first().click();

  await expect(page.getByTestId('surah-jump-picker-panel')).toHaveCount(0);
  // Al-Baqara opens page 2 of the 604-page Madani mushaf — a layout invariant, not seed data.
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('2');
  await expect(page).toHaveURL(/[?&]page=2(&|$)/);
  await expect(page.getByTestId('mushaf-page-view')).toBeVisible();
});

test(
  'the real Mushaf stack renders the verified page-1 Quran oracle',
  {
    annotation: [
      { type: 'critical' },
      { type: 'read-only' },
      { type: 'artifact', description: 'compact-cross-stack-base' },
      { type: 'journey', description: 'quran-fidelity.mushaf-font-rendering' },
    ],
  },
  async ({ page, request }, testInfo) => {
    expect(pageOneOracle.artifactId).toBe('compact-cross-stack-base');

    const apiResponse = await request.get(
      `${API_ORIGIN}/api/mushaf/pages/${pageOneOracle.pageNumber}`,
    );
    expect(apiResponse.status()).toBe(200);
    const envelope = (await apiResponse.json()) as MushafPageEnvelope;
    expect(envelope.isSuccess).toBe(true);
    expect(envelope.data).not.toBeNull();
    expect(envelope.data?.pageNumber).toBe(pageOneOracle.pageNumber);

    const apiWords = envelope.data?.lines.flatMap((line) => line.words) ?? [];
    expect(
      apiWords.map((word) => ({
        location: word.wordLocation,
        verseKey: word.verseKey,
        textUthmani: word.textUthmani,
        isAyahMarker: word.isAyahMarker,
      })),
    ).toEqual(pageOneOracle.words);
    expect([...new Set(apiWords.map((word) => word.verseKey))]).toEqual(
      pageOneOracle.ayahs.map((ayah) => ayah.verseKey),
    );
    expect(
      envelope.data?.lines.map((line) => ({
        lineNumber: line.lineNumber,
        lineType: line.lineType,
        isCentered: line.isCentered,
        surahNumber: line.surahNumber,
        wordLocations: line.words.map((word) => word.wordLocation),
      })),
    ).toEqual(pageOneOracle.lines);

    const studyApiResponse = await request.get(
      `${API_ORIGIN}/api/mushaf/ayahs/${pageOneOracle.study.verseKey}/study`,
      {
        params: {
          tafsirSource: pageOneOracle.study.tafsir.sourceKey,
          translationSource: pageOneOracle.study.translation.sourceKey,
        },
      },
    );
    expect(studyApiResponse.status()).toBe(200);
    const studyEnvelope = (await studyApiResponse.json()) as {
      isSuccess: boolean;
      data: {
        selectedSources: { tafsirSource: string; translationSource: string };
        tafsir: { sourceKey: string; displayNameAr: string; text: string };
        translation: {
          sourceKey: string;
          displayNameAr: string;
          displayNameEn: string;
          text: string;
        };
      };
    };
    expect(studyEnvelope.isSuccess).toBe(true);
    expect(studyEnvelope.data.selectedSources).toMatchObject({
      tafsirSource: pageOneOracle.study.tafsir.sourceKey,
      translationSource: pageOneOracle.study.translation.sourceKey,
    });
    expect(studyEnvelope.data.tafsir).toMatchObject(pageOneOracle.study.tafsir);
    expect(studyEnvelope.data.translation).toMatchObject(pageOneOracle.study.translation);

    await openReader(page);

    await expect(page.getByTestId('mushaf-page-surah-glyph').first()).toBeVisible();
    await expect(page.getByTestId('mushaf-page-juz-glyph').first()).toBeVisible();
    await expectRenderedOracle(page);
    await expectLocalMushafFontsReady(page);

    const reviewedWord = page.locator(
      `[data-word-location="${pageOneOracle.words[0].location}"]`,
    );
    await reviewedWord.focus();
    await expect(reviewedWord).toBeFocused();
    await page.keyboard.press('Enter');
    await expect
      .poll(() => {
        const url = new URL(page.url());
        return {
          ayah: url.searchParams.get('ayah'),
          word: url.searchParams.get('word'),
        };
      })
      .toEqual({
        ayah: pageOneOracle.words[0].verseKey,
        word: pageOneOracle.words[0].location,
      });
    await expect(reviewedWord).toHaveAttribute('aria-current', 'true');
    await expect(reviewedWord).toBeFocused();
    await expect(page.getByTestId('word-analysis-error')).toHaveText(
      'بيانات تحليل الكلمة غير مكتملة',
    );
    await expectReviewedStudySourcesVisible(page);

    await expectNoBlockingAccessibilityViolations(page, testInfo);

    await page.goto('/dashboard/mushaf?page=2');
    await expect(page.getByTestId('mushaf-page-error')).toHaveText('المورد غير موجود');
    await expect(page.getByTestId('mushaf-page-view')).toHaveCount(0);
  },
);

test.describe('approved Mushaf mobile variant', () => {
  test.use({ ...APPROVED_MUSHAF_MOBILE });

  test(
    'the source-reviewed Quran oracle remains exact on the approved mobile reader',
    {
      annotation: [
        { type: 'critical' },
        { type: 'mobile' },
        { type: 'read-only' },
        { type: 'artifact', description: 'compact-cross-stack-base' },
        { type: 'journey', description: 'quran-fidelity.mushaf-mobile' },
      ],
    },
    async ({ page }, testInfo) => {
      await openReader(page);
      await expect(page.getByTestId('mushaf-reader-page')).toHaveAttribute('dir', 'rtl');
      await expectRenderedOracle(page);
      await expectLocalMushafFontsReady(page);

      const reviewedWord = page.locator(
        `[data-word-location="${pageOneOracle.words[0].location}"]`,
      );
      await reviewedWord.tap();
      await expect(reviewedWord).toHaveAttribute('aria-current', 'true');
      await expect(page.getByTestId('word-analysis-error')).toHaveText(
        'بيانات تحليل الكلمة غير مكتملة',
      );
      await expectReviewedStudySourcesVisible(page);
      await expectNoBlockingAccessibilityViolations(page, testInfo);
    },
  );
});
