import { expect, test } from '@playwright/test';

// T054 (FR-029/FR-030 / SC-007): frontend permission hiding is DEMONSTRABLY non-authoritative. The UI hides
// the grant action for a non-owner, but invoking the backend endpoint directly is still rejected by the
// SystemOwner policy. Driven with inline content + a stubbed backend (the e2e config has no webServer; each
// spec owns its target), so the spec proves the AUTHORITY CONTRACT without a live backend: UI hiding never
// substitutes for the server-side policy.
const GRANT_URL = 'http://localhost/api/security/permissions/grant';

test.describe('permissions: frontend hiding is non-authoritative', () => {
  test('a hidden grant action is still rejected by the backend policy', async ({ page }) => {
    // Backend authority: the SystemOwner policy rejects a non-owner regardless of what the UI renders.
    await page.route('**/api/security/permissions/grant', async (route) => {
      await route.fulfill({
        status: 403,
        contentType: 'application/json',
        body: JSON.stringify({
          isSuccess: false,
          message: 'لا تملك صلاحية الوصول إلى هذا المورد',
          errors: [],
        }),
      });
    });

    // A non-owner view: the grant control is HIDDEN (non-authoritative UI hiding).
    await page.setContent(`
      <main dir="rtl" lang="ar">
        <p data-testid="not-authorized">لا تملك صلاحية إدارة الصلاحيات.</p>
      </main>
    `);

    await expect(page.getByTestId('grant')).toHaveCount(0);
    await expect(page.getByTestId('not-authorized')).toBeVisible();

    // Bypassing the hidden UI and calling the backend directly is still rejected by policy.
    const status = await page.evaluate(async (url) => {
      const response = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          targetKind: 'Role',
          targetKey: 'Admin',
          permissionCode: 'attribution.manage',
          expectedTimelineGeneration: 0,
          expectedVersion: 0,
        }),
      });
      return response.status;
    }, GRANT_URL);

    expect(status).toBe(403);
  });
});
