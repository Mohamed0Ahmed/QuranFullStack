import type { Page, Route } from '@playwright/test';

import oracleData from '../../../test-oracles/phrase-search.json';
import {
  WORDS_PHRASES_REPETITIONS_SEGMENT,
  WORDS_PHRASES_SIMILARITY_SEGMENT,
  phraseSearchRoutePath,
} from '../src/app/core/navigation/route-paths';
import { environment } from '../src/environments/environment.development';
import { createAccessibilityAudit } from './fixtures/accessibility';
import { expect, test } from './fixtures/app-test';

const API_ORIGIN = environment.apiBaseUrl;
const INDEX_UNAVAILABLE_MESSAGE = 'فهرس البحث في العبارات غير متاح حاليًا';
const INDEX_CHANGED_MESSAGE = 'تغير فهرس البحث، أعد اختيار النتيجة';

interface PhraseReadyOracle {
  query: {
    raw: string;
  };
}

const oracle = oracleData as PhraseReadyOracle;

test(
  'a visitor recovers from unavailable and stale PhraseSearch index responses',
  {
    annotation: [
      { type: 'critical' },
      { type: 'canonical-read' },
      { type: 'fixture-policy', description: 'canonical-read-only' },
      { type: 'journey', description: 'phrase-search.unavailable-stale' },
    ],
  },
  async ({ page }, testInfo) => {
    const accessibility = createAccessibilityAudit(testInfo);
    try {
      await exerciseUnavailableIndex(page);
      await accessibility.expectNoBlockingViolations(page);

      await exerciseStaleIndex(page);
      await accessibility.expectNoBlockingViolations(page);
    } finally {
      await accessibility.attachObservations();
    }
  },
);

async function exerciseUnavailableIndex(page: Page): Promise<void> {
  const capabilitiesUrl = `${API_ORIGIN}/api/quran/phrase-search/capabilities`;
  const unavailable = async (route: Route): Promise<void> => {
    await route.fulfill({
      status: 503,
      contentType: 'application/json',
      json: phraseIndexFailure(INDEX_UNAVAILABLE_MESSAGE, 'phrase_index_unavailable'),
    });
  };

  await page.route(capabilitiesUrl, unavailable);
  try {
    await page.goto(phraseSearchRoutePath(WORDS_PHRASES_REPETITIONS_SEGMENT));
    await expect(page.getByText(INDEX_UNAVAILABLE_MESSAGE, { exact: true })).toBeVisible();

    const retry = page.getByRole('button', { name: 'إعادة المحاولة', exact: true });
    await retry.focus();
    await expect(retry).toBeFocused();
  } finally {
    await page.unroute(capabilitiesUrl, unavailable);
  }
}

async function exerciseStaleIndex(page: Page): Promise<void> {
  const similaritySearchUrl = `${API_ORIGIN}/api/quran/phrase-search/similarities/search`;
  let staleResponseInjected = false;
  const staleOnce = async (route: Route): Promise<void> => {
    if (staleResponseInjected) {
      await route.continue();
      return;
    }

    staleResponseInjected = true;
    await route.fulfill({
      status: 409,
      contentType: 'application/json',
      json: phraseIndexFailure(INDEX_CHANGED_MESSAGE, 'phrase_index_changed'),
    });
  };

  await page.route(`${similaritySearchUrl}*`, staleOnce);
  try {
    await page.goto(phraseSearchRoutePath(WORDS_PHRASES_SIMILARITY_SEGMENT));
    await page.getByRole('textbox', { name: 'العبارة المرجعية' }).fill(oracle.query.raw);
    await page.getByRole('button', { name: 'بحث', exact: true }).click();

    await expect
      .poll(() => staleResponseInjected, { message: 'the stale similarity response was not requested' })
      .toBe(true);
    await expect(page.getByRole('status').filter({ hasText: INDEX_CHANGED_MESSAGE })).toContainText(
      INDEX_CHANGED_MESSAGE,
    );
  } finally {
    await page.unroute(`${similaritySearchUrl}*`, staleOnce);
  }
}

function phraseIndexFailure(message: string, code: string): object {
  return {
    isSuccess: false,
    message,
    data: null,
    errors: [code],
  };
}
