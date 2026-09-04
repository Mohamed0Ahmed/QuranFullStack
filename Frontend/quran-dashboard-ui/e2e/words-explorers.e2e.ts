import { expect, test } from './fixtures/app-test';
import { test as authenticatedTest } from './fixtures/auth';

const SEARCHABLE_EXPLORERS = [
  {
    name: 'roots',
    path: '/dashboard/words/roots',
    title: 'roots-explorer-page-title',
    search: 'roots-search-input',
    rowButton: 'roots-table-root-button',
    panelEntity: 'root-details-panel-entity',
    query: 'كتب',
  },
  {
    name: 'lemmas',
    path: '/dashboard/words/lemmas',
    title: 'lemmas-explorer-page-title',
    search: 'lemmas-search-input',
    rowButton: 'lemmas-table-lemma-button',
    panelEntity: 'lemma-details-panel-entity',
    // Lemma text is Uthmani: كِتَٰب carries a superscript alef (U+0670) that the backend's
    // ArabicSearchQueryNormalizer does not fold to a plain alef, so `كتاب` matches nothing.
    query: 'كتب',
  },
  {
    name: 'stems',
    path: '/dashboard/words/stems',
    title: 'stems-explorer-page-title',
    search: 'stems-search-input',
    rowButton: 'stems-table-stem-button',
    panelEntity: 'stem-details-panel-entity',
    query: 'كتاب',
  },
] as const;

const CANONICAL_READ = {
  annotation: [
    { type: 'canonical-read' },
    { type: 'fixture-policy', description: 'canonical-read-only' },
  ],
};

for (const explorer of SEARCHABLE_EXPLORERS) {
  test(`${explorer.name} explorer: search then open a row shows the details panel`, CANONICAL_READ, async ({
      page,
    }) => {
    await page.goto(explorer.path);
    await expect(page.getByTestId(explorer.title)).toBeVisible();

    await page.getByTestId(explorer.search).fill(explorer.query);
    await expect(page).toHaveURL(/[?&]search=[^&]/);

    const rows = page.getByTestId(explorer.rowButton);
    await expect(rows.first()).toBeVisible();

    await rows.first().click();

    // The desktop panel is `inline`, so its header — and this span — is mounted with an empty
    // selection too; only non-empty entity text proves the selection actually loaded.
    const entity = page.getByTestId(explorer.panelEntity);
    await expect(entity).toBeVisible();
    await expect(entity).not.toBeEmpty();
  });
}

authenticatedTest(
  'word-types explorer: detail sessions fence stale reads and remain isolated',
  {
    annotation: [
      { type: 'mutating' },
      { type: 'fixture-policy', description: 'mutable-authenticated' },
    ],
  },
  async ({ page, ownerPersona: _ownerPersona }) => {
    let releaseStaleDetail!: () => void;
    const staleDetailReleased = new Promise<void>((resolve) => {
      releaseStaleDetail = resolve;
    });
    let markStaleDetailStarted!: () => void;
    const staleDetailStarted = new Promise<void>((resolve) => {
      markStaleDetailStarted = resolve;
    });
    let markStaleDetailSettled!: () => void;
    let markStaleDetailFailed!: (error: unknown) => void;
    const staleDetailSettled = new Promise<void>((resolve, reject) => {
      markStaleDetailSettled = resolve;
      markStaleDetailFailed = reject;
    });
    let overlayDetailAttempts = 0;

    await page.route('**/api/words/word-types/**', async (route) => {
      const pathname = new URL(route.request().url()).pathname;

      if (pathname.endsWith('/tree')) {
        await route.fulfill({
          json: {
            isSuccess: true,
            message: null,
            data: {
              mainTypes: [
                {
                  code: 'noun',
                  label: { ar: 'اسم' },
                  count: 3,
                  children: [{ code: 'N', childCode: 'N', label: { ar: 'اسم' }, count: 3 }],
                  secondaryFilter: { kind: 'case', options: [], voiceOptions: [] },
                },
              ],
            },
            errors: null,
          },
        });
        return;
      }

      if (pathname.endsWith('/table')) {
        await route.fulfill({
          json: {
            isSuccess: true,
            message: null,
            data: {
              page: 1,
              pageSize: 50,
              totalCount: 2,
              items: [
                {
                  kind: 'word',
                  tashkeelWordId: 41,
                  contextCode: 'N',
                  case: null,
                  tense: null,
                  voice: null,
                  displayText: 'كتاب',
                  typeCode: 'N',
                  typeLabel: { ar: 'اسم' },
                  broadLabel: { ar: 'اسم' },
                  caseOrFeature: null,
                  rootText: 'كتب',
                  lemmaText: 'كتاب',
                  stemText: 'كتاب',
                  occurrencesCount: 1,
                  ayahsCount: 1,
                  surahsCount: 1,
                },
                {
                  kind: 'word',
                  tashkeelWordId: 42,
                  contextCode: 'N',
                  case: null,
                  tense: null,
                  voice: null,
                  displayText: 'قلم',
                  typeCode: 'N',
                  typeLabel: { ar: 'اسم' },
                  broadLabel: { ar: 'اسم' },
                  caseOrFeature: null,
                  rootText: 'قلم',
                  lemmaText: 'قلم',
                  stemText: 'قلم',
                  occurrencesCount: 1,
                  ayahsCount: 1,
                  surahsCount: 1,
                },
              ],
            },
            errors: null,
          },
        });
        return;
      }

      if (pathname.endsWith('/scope-counts')) {
        await route.fulfill({
          json: {
            isSuccess: true,
            message: null,
            data: { wordsCount: 3, rootsCount: 1, stemsCount: 1, lemmasCount: 1 },
            errors: null,
          },
        });
        return;
      }

      if (pathname.endsWith('/words/41/ayahs')) {
        markStaleDetailStarted();
        await staleDetailReleased;
        try {
          await route.fulfill({
            json: {
              isSuccess: true,
              message: null,
              data: {
                page: 1,
                pageSize: 20,
                totalCount: 1,
                items: [
                  {
                    ayahNumber: 2,
                    matchedWordIds: [4101],
                    matchedWordPositions: [2],
                    pageNumber: 2,
                    surahNameArabic: 'البقرة',
                    surahNumber: 2,
                    verseKey: '2:2',
                    words: [{ quranWordId: 4101, textUthmani: 'الْكِتَابُ', isAyahMarker: false }],
                  },
                ],
              },
              errors: null,
            },
          });
          markStaleDetailSettled();
        } catch (error) {
          markStaleDetailFailed(error);
          throw error;
        }
        return;
      }

      if (pathname.endsWith('/words/42/ayahs')) {
        await route.fulfill({
          json: {
            isSuccess: true,
            message: null,
            data: {
              page: 1,
              pageSize: 20,
              totalCount: 1,
              items: [
                {
                  ayahNumber: 1,
                  matchedWordIds: [4201],
                  matchedWordPositions: [2],
                  pageNumber: 564,
                  surahNameArabic: 'القلم',
                  surahNumber: 68,
                  verseKey: '68:1',
                  words: [{ quranWordId: 4201, textUthmani: 'وَٱلْقَلَمِ', isAyahMarker: false }],
                },
              ],
            },
            errors: null,
          },
        });
        return;
      }

      if (pathname.endsWith('/words/43/ayahs')) {
        overlayDetailAttempts++;
        if (overlayDetailAttempts === 1) {
          await route.fulfill({
            status: 500,
            json: {
              isSuccess: false,
              message: 'تعذر تحميل اختبار النافذة',
              data: null,
              errors: null,
            },
          });
          return;
        }

        await route.fulfill({
          json: {
            isSuccess: true,
            message: null,
            data: {
              page: 1,
              pageSize: 20,
              totalCount: 1,
              items: [
                {
                  ayahNumber: 2,
                  matchedWordIds: [4301],
                  matchedWordPositions: [1],
                  pageNumber: 415,
                  surahNameArabic: 'السجدة',
                  surahNumber: 32,
                  verseKey: '32:2',
                  words: [{ quranWordId: 4301, textUthmani: 'تَنزِيلُ', isAyahMarker: false }],
                },
              ],
            },
            errors: null,
          },
        });
        return;
      }

      if (pathname.endsWith('/words/41')) {
        await route.fulfill({
          json: {
            isSuccess: true,
            message: null,
            data: {
              tashkeelWordId: 41,
              contextCode: 'N',
              displayText: 'كتاب',
              typeLabel: { ar: 'اسم' },
              broadLabel: { ar: 'اسم' },
              caseOrFeature: null,
              rootText: 'كتب',
              lemmaText: 'كتاب',
              stemText: 'كتاب',
              occurrencesCount: 1,
              ayahsCount: 1,
              surahsCount: 1,
            },
            errors: null,
          },
        });
        return;
      }

      if (pathname.endsWith('/words/43')) {
        await route.fulfill({
          json: {
            isSuccess: true,
            message: null,
            data: {
              tashkeelWordId: 43,
              contextCode: 'N',
              displayText: 'تنزيل',
              typeLabel: { ar: 'اسم' },
              broadLabel: { ar: 'اسم' },
              caseOrFeature: null,
              rootText: 'نزل',
              lemmaText: 'تنزيل',
              stemText: 'تنزيل',
              occurrencesCount: 1,
              ayahsCount: 1,
              surahsCount: 1,
            },
            errors: null,
          },
        });
        return;
      }

      await route.fallback();
    });

    await page.goto('/dashboard/words/types');

    // The table lists nothing until a subtype is chosen — the default scope only renders the
    // select-a-subtype prompt (word-types-table.component.html, `word-types-select-subtype`).
    await expect(page.getByTestId('word-types-select-subtype')).toBeVisible();

    await page
      .getByRole('group', { name: 'الأنواع الفرعية' })
      .getByRole('listitem')
      .getByRole('button')
      .first()
      .click();
    await expect(page).toHaveURL(/[?&]childCode=[^&]/);

    const rows = page.locator('qd-word-types-table [data-row-id]');
    await expect(rows.first()).toBeVisible();

    // Rows are not clickable: the three statistic chips are the only interactive elements in a row
    // (word-types-table.component.html, `#rowCells`).
    await rows.first().getByTestId('word-count-chip').first().click();
    await staleDetailStarted;
    await rows.nth(1).getByTestId('word-count-chip').first().click();

    const entity = page.getByTestId('word-type-details-panel-entity');
    await expect(entity).toBeVisible();
    await expect(entity).toHaveText('قلم');
    await expect(page.getByTestId('ayah-match-card')).toContainText('القلم');
    releaseStaleDetail();
    await staleDetailSettled;
    await expect(page.getByTestId('ayah-match-card')).not.toContainText('البقرة');
    await expect(page.locator('[data-linking-source-action]').first()).toBeVisible();

    const validDetailUrl = new URL(page.url());
    const wordId = validDetailUrl.searchParams.get('word');
    const contextCode = validDetailUrl.searchParams.get('contextCode');
    const caseValue = validDetailUrl.searchParams.get('detailCase');
    const tense = validDetailUrl.searchParams.get('detailTense');
    const voice = validDetailUrl.searchParams.get('detailVoice');
    expect(wordId).not.toBeNull();
    expect(contextCode).not.toBeNull();
    expect(caseValue).not.toBeNull();
    expect(tense).not.toBeNull();
    expect(voice).not.toBeNull();

    const encodedContext = encodeURIComponent(contextCode!).replace(/~/g, '%7E');
    const frame = [
      'v1',
      'wordType',
      '43',
      encodedContext,
      caseValue!,
      tense!,
      voice!,
      'ayahs',
      '1',
    ].join('~');
    const scopeLessOverlayUrl = new URL(validDetailUrl);
    scopeLessOverlayUrl.searchParams.append('qdDetail', frame);
    scopeLessOverlayUrl.searchParams.set('qdDetailOpen', '1');

    await page.evaluate((url) => {
      window.history.pushState({}, '', url);
      window.dispatchEvent(new PopStateEvent('popstate'));
    }, scopeLessOverlayUrl.toString());

    const overlay = page.getByTestId('detail-modal-shell');
    await expect(overlay).toBeVisible();
    await expect(overlay.getByTestId('overlay-word-type-panel-error')).toContainText(
      'تعذر تحميل اختبار النافذة',
    );
    await expect(page.getByTestId('word-type-details-panel-entity')).toHaveText('قلم');
    await expect(page.getByTestId('ayah-match-card').filter({ hasText: 'القلم' })).toBeVisible();
    await expect(overlay.locator('[data-linking-source-action]')).toHaveCount(0);

    const overlayRecovery = page.waitForResponse(
      (response) =>
        response.status() === 200 &&
        new URL(response.url()).pathname === '/api/words/word-types/words/43/ayahs',
    );
    await overlay.getByRole('button', { name: 'إعادة المحاولة' }).click();
    await overlayRecovery;
    await expect(overlay.getByTestId('overlay-word-type-ayahs-view')).toContainText('السجدة');
    expect(overlayDetailAttempts).toBe(2);

    await overlay.getByTestId('detail-modal-close').click();
    await expect(page.getByTestId('detail-modal-restore')).toBeVisible();
    await expect(page).not.toHaveURL(/[?&]qdDetailOpen=1(?:&|$)/);

    await page.getByTestId('detail-modal-restore').click();
    await expect(page).toHaveURL(/[?&]qdDetailOpen=1(?:&|$)/);
    await expect(page.getByTestId('detail-modal-shell')).toBeVisible();
    await expect(page.getByTestId('overlay-word-type-ayahs-view')).toBeVisible();
    await expect(
      page.getByTestId('detail-modal-shell').locator('[data-linking-source-action]'),
    ).toHaveCount(0);

    const blockedUnsafeDetailRequests: string[] = [];
    const detailRequestPattern = '**/api/words/word-types/words/**';
    await page.route(detailRequestPattern, async (route) => {
      blockedUnsafeDetailRequests.push(new URL(route.request().url()).pathname);
      await route.abort('blockedbyclient');
    });

    const unsafeDetailUrl = new URL(validDetailUrl);
    unsafeDetailUrl.searchParams.set('word', '9007199254740993');
    await page.goto(unsafeDetailUrl.toString());
    await expect(rows.first()).toBeVisible();
    await expect(page.getByTestId('word-type-details-panel-entity')).toBeEmpty();
    await page.unroute(detailRequestPattern);
    expect(blockedUnsafeDetailRequests).toEqual([]);
  },
);

test(
  'unique-words explorer: search then open a word shows the drilldown',
  {
    annotation: [
      ...CANONICAL_READ.annotation,
      { type: 'critical' },
      { type: 'journey', description: 'word-explorer.canonical-read' },
    ],
  },
  async ({ page }) => {
    await page.goto('/dashboard/words/unique/tashkeel');

    await expect(page.getByTestId('unique-words-page-title')).toBeVisible();

    await page.getByTestId('unique-words-search-input').fill('الله');
    // The 300 ms debounce is the only barrier between the unfiltered list already on screen and the
    // filtered one; without waiting for the param the row assertion resolves against stale rows.
    await expect(page).toHaveURL(/[?&]search=[^&]/);
    const words = page.getByTestId('unique-words-table-word-button');
    await expect(words.first()).toBeVisible();

    await words.first().click();

    const entity = page.getByTestId('word-drilldown-entity');
    await expect(entity).toBeVisible();
    await expect(entity).not.toBeEmpty();
  },
);

test('the unique-words route redirects to the tashkeel mode', CANONICAL_READ, async ({ page }) => {
  await page.goto('/dashboard/words/unique');

  await expect(page).toHaveURL(/\/dashboard\/words\/unique\/tashkeel/);
});
