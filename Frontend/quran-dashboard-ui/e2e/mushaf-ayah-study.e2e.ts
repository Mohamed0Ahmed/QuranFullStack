import type { Page } from '@playwright/test';

import { expect, test } from './fixtures/app-test';
import { ayahTab, openReader, selectFirstWord } from './fixtures/mushaf';

type AyahStudyGroup = 'sources' | 'similarity';

async function openAyahStudy(page: Page, group: AyahStudyGroup = 'sources'): Promise<void> {
  await openReader(page);
  await selectFirstWord(page);
  await page.getByTestId(`study-context-tab-${group}`).click();
  await expect(page.getByTestId('selected-ayah-section')).toBeVisible();
}

test('selecting a word opens the word analysis tab', async ({ page }) => {
  await openReader(page);
  await selectFirstWord(page);

  await expect(page).toHaveURL(/[?&]word=/);
  await expect(page.getByTestId('study-context-tab-analysis')).toHaveAttribute(
    'aria-selected',
    'true',
  );
  await expect(page.getByTestId('selected-word-section')).toBeVisible();
  await expect(page.getByTestId('selected-ayah-section')).toHaveCount(0);
});

test('selecting another word keeps the translation tab open', async ({ page }) => {
  await openAyahStudy(page);

  await ayahTab(page, 'الترجمة').click();
  const firstAyah = new URL(page.url()).searchParams.get('ayah');
  await page.locator('[data-word-location][data-is-marker="false"]').last().click();

  await expect(page).toHaveURL(/[?&]ayahTab=translation/);
  await expect.poll(() => new URL(page.url()).searchParams.get('ayah')).not.toBe(firstAyah);
  await expect(page.getByTestId('study-context-tab-sources')).toHaveAttribute(
    'aria-selected',
    'true',
  );
  await expect(ayahTab(page, 'الترجمة')).toHaveAttribute('aria-selected', 'true');
  await expect(page.getByTestId('translation-card')).toBeVisible();
});

test('the full-i3rab tab renders the i3rab card', async ({ page }) => {
  await openAyahStudy(page);

  await ayahTab(page, 'الإعراب').click();

  await expect(page).toHaveURL(/[?&]ayahTab=full-i3rab/);
  await expect(page.getByTestId('full-i3rab-card')).toBeVisible();
});

test('the similar-ayahs tab lists similar ayahs or reports none', async ({ page }) => {
  await openAyahStudy(page, 'similarity');

  await expect(page.getByTestId('similar-ayah-count')).toHaveText(/^\d+$/);
  await page.getByTestId('ayah-tab-similar-ayahs').click();

  await expect(
    page.getByTestId('similar-ayahs-list').or(page.getByTestId('similar-ayahs-empty')),
  ).toBeVisible();
});

test('the mutashabihat tab lists groups or reports none', async ({ page }) => {
  await openAyahStudy(page, 'similarity');

  await expect(page.getByTestId('mutashabihat-group-count')).toHaveText(/^\d+$/);
  await page.getByTestId('ayah-tab-mutashabihat').click();

  await expect(
    page.getByTestId('mutashabihat-groups-list').or(page.getByTestId('mutashabihat-empty')),
  ).toBeVisible();
});

test('switching the tafsir source writes the source into the URL', async ({ page }) => {
  await openAyahStudy(page);

  const selector = page.getByTestId('source-selector');
  await expect(selector).toBeVisible();

  // showPicker() only renders the trigger when more than one source exists
  // (source-selector.component.ts); one seeded source renders a static label and nothing to click.
  test.skip(
    await selector.getByTestId('source-single-option').isVisible(),
    'single tafsir source seeded',
  );

  await selector.getByTestId('source-selector-trigger').click();
  await expect(selector.getByTestId('source-selector-panel')).toBeVisible();

  // openPanel() resolves the selected key's language group and jumps straight to the sources
  // view, so the languages step only renders while nothing is selected yet.
  // patchUrlQuery merges unconditionally, so re-picking the selected source would still write
  // the key — only a row that is not the current one proves the switch happened.
  const otherSource = selector
    .locator('[data-testid="source-selector-source-row"][aria-selected="false"]')
    .first();
  await expect(otherSource).toBeVisible();

  await otherSource.click();

  // The picked row is different by construction (aria-selected="false"), so the key landing in
  // the URL is the switch. The trigger label is not a usable witness — it lives inside
  // `@if (showPicker())` and unmounts while the card reloads.
  await expect(page).toHaveURL(/[?&]tafsirSource=/);
});
