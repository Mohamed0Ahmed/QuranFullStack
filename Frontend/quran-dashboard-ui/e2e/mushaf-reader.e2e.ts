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
  artifactId: string;
  pageNumber: number;
  verseKeys: string[];
  words: OracleWord[];
}

interface MushafPageEnvelope {
  isSuccess: boolean;
  data: {
    pageNumber: number;
    lines: Array<{
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
    await expect(word).toHaveClass(/mushaf-word--selected-word/);
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
  async ({ page, request }) => {
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
      pageOneOracle.verseKeys,
    );

    await openReader(page);

    await expect(page.getByTestId('mushaf-page-surah-glyph').first()).toBeVisible();
    await expect(page.getByTestId('mushaf-page-juz-glyph').first()).toBeVisible();

    const words = page.locator('[data-word-location]');
    await expect(words.first()).toBeVisible();
    const renderedWords = await words.evaluateAll((elements) =>
      elements.map((element) => ({
        location: element.getAttribute('data-word-location'),
        textUthmani: element.querySelector('.mushaf-word__text')?.textContent?.trim() ?? null,
        isAyahMarker: element.getAttribute('data-is-marker') === 'true',
      })),
    );
    expect(renderedWords).toEqual(
      pageOneOracle.words.map((word) => ({
        location: word.location,
        textUthmani: word.textUthmani,
        isAyahMarker: word.isAyahMarker,
      })),
    );

    // The reader must render in Amiri, never UthmanicHafs_V22, which mis-renders U+06DF
    // (Frontend/quran-dashboard-ui/README.md).
    const fontFamily = await words
      .first()
      .evaluate((element) => getComputedStyle(element).fontFamily);
    expect(fontFamily).toContain('Amiri');
  },
);
