import { test, expect } from './fixtures/auth';

test('an authenticated direct grant reveals its exact Abwab write affordance', async ({
  page,
  authenticatedPersona: _authenticatedPersona,
}) => {
  await page.goto('/abwab');

  await expect(page.getByTestId('abwab-page-add-root')).toBeVisible();
});
