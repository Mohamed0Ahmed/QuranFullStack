import { expect, test } from './fixtures/abwab';

/** Slice J task 2.5 / acceptance criteria 2 and 3: the `--wide` step measures 52rem and the
 * modals that did NOT opt in keep their own widths. Measured rather than eyeballed — the
 * ladder in UI_STYLE_SYSTEM.md §17 is a set of numbers, so a number is the right assertion. */
const REM = 16;
const VIEWPORTS = [
  { name: '1024', width: 1024, height: 900 },
  { name: '1184', width: 1184, height: 900 },
  { name: '1440', width: 1440, height: 900 },
];

// Theme is deliberately not a variant here: no theme token participates in modal geometry, so a
// dark run would assert nothing the light run does not. The theme-dependent surface is the
// danger tone, checked in both themes below.
for (const viewport of VIEWPORTS) {
  test(`--wide measures 52rem at ${viewport.name}px`, async ({ page, abwabSandbox }) => {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    const door = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('width-probe') });

    await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);
    await page.getByTestId(`abwab-tree-row-${door.id}`).click();

    // The move picker is one of the three adopters and is reachable in two clicks.
    await page.getByTestId('abwab-side-panel-op-move').click();
    const picker = page.getByTestId('abwab-move-picker');
    await expect(picker).toBeVisible();

    const box = (await picker.boundingBox())!;
    expect(Math.round(box.width)).toBe(Math.min(viewport.width, 52 * REM));

    expect(await picker.evaluate((el) => getComputedStyle(el).direction)).toBe('rtl');

    // Centring is measured against the backdrop, which is what actually centres the modal.
    // The window is the wrong frame: RTL puts the scrollbar on the left, so window-relative
    // gutters differ by the scrollbar width even when the modal is perfectly centred.
    const backdrop = (await page.getByTestId('abwab-move-picker-backdrop').boundingBox())!;
    const startGutter = box.x - backdrop.x;
    const endGutter = backdrop.x + backdrop.width - (box.x + box.width);
    expect(Math.abs(startGutter - endGutter)).toBeLessThanOrEqual(2);
    // …and the gutters are real, not a full-bleed modal.
    expect(startGutter).toBeGreaterThan(60);
  });
}

for (const theme of ['light', 'dark'] as const) {
  test(`the danger confirm renders its destructive role in ${theme}`, async ({ page, abwabSandbox }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    const door = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('confirm-probe') });

    await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);
    await page.emulateMedia({ colorScheme: theme });
    await page.getByTestId(`abwab-tree-row-${door.id}`).click();
    await page.getByTestId('abwab-side-panel-op-archive').click();

    const dialog = page.getByTestId('abwab-page-archive-confirm');
    await expect(dialog).toBeVisible();
    // The confirm dialog is criterion 3's control case: it composes `.qd-modal` and must be
    // untouched by `--wide` living in the same stylesheet.
    expect(Math.round((await dialog.boundingBox())!.width)).toBe(28 * REM);

    // Task 3.13 — the danger tone reached the button rather than falling back to primary.
    const confirm = page.getByTestId('abwab-page-archive-confirm-confirm');
    await expect(confirm).toHaveClass(/qd-confirm-dialog__confirm--danger/);
    expect(await confirm.evaluate((el) => getComputedStyle(el).backgroundColor)).not.toBe('rgba(0, 0, 0, 0)');
  });
}

// The words explorers render their detail surface as an inline panel at desktop and as this
// modal below that breakpoint, so the hold-out is only measurable at a narrow viewport.
test('a words detail modal keeps its 42rem hold-out', async ({ page }) => {
  await page.setViewportSize({ width: 900, height: 900 });
  await page.goto('/dashboard/words/roots');
  await page.getByTestId('roots-search-input').fill('كتب');

  const firstRow = page.getByTestId('roots-table-root-button').first();
  await firstRow.waitFor();
  await firstRow.click();

  const modal = page.locator('.qd-modal.explorer-detail-modal').first();
  await expect(modal).toBeVisible();
  // 42rem exceeds this viewport, so the rendered box clamps to `min(100%, 42rem)`; the computed
  // width is what proves the 52rem modifier did not reach it.
  expect(await modal.evaluate((el) => getComputedStyle(el).width)).toBe('672px');
});
