import { expect, test } from './fixtures/app-test';

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

for (const explorer of SEARCHABLE_EXPLORERS) {
  test(`${explorer.name} explorer: search then open a row shows the details panel`, async ({
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

test('word-types explorer: picking a subtype lists rows and a count chip opens the details panel', async ({
  page,
}) => {
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

  const entity = page.getByTestId('word-type-details-panel-entity');
  await expect(entity).toBeVisible();
  await expect(entity).not.toBeEmpty();
});

test('unique-words explorer: search then open a word shows the drilldown', async ({ page }) => {
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
});

test('the unique-words route redirects to the tashkeel mode', async ({ page }) => {
  await page.goto('/dashboard/words/unique');

  await expect(page).toHaveURL(/\/dashboard\/words\/unique\/tashkeel/);
});
