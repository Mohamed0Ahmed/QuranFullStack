import { expect, test } from './fixtures/abwab';

// Operations flow (plan-slice-b2.md T603): inline reorder (commit/revert), single move
// (into a destination, then back out "as main"), bulk mode (select/move/archive with the
// all-or-nothing confirm), and the row context menu — including the "must stay absent"
// assertion for the relations/protection controls (plan.md §5.1).

test('inline reorder: Enter commits and resequences siblings, Escape reverts', async ({ page, abwabSandbox }) => {
  const doorA = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('reorder-a') });
  const doorB = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('reorder-b') });
  const doorC = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('reorder-c') });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);
  await expect(page.getByTestId(`abwab-tree-order-${doorA.id}`)).toHaveText('1');
  await expect(page.getByTestId(`abwab-tree-order-${doorB.id}`)).toHaveText('2');
  await expect(page.getByTestId(`abwab-tree-order-${doorC.id}`)).toHaveText('3');

  // Move C to position 1 — the whole scope resequences to 1..N.
  await page.getByTestId(`abwab-tree-order-${doorC.id}`).click();
  await page.getByTestId(`abwab-tree-order-input-${doorC.id}`).fill('1');
  await page.getByTestId(`abwab-tree-order-input-${doorC.id}`).press('Enter');

  await expect(page.getByTestId(`abwab-tree-order-${doorC.id}`)).toHaveText('1');
  await expect(page.getByTestId(`abwab-tree-order-${doorA.id}`)).toHaveText('2');
  await expect(page.getByTestId(`abwab-tree-order-${doorB.id}`)).toHaveText('3');

  // Escape reverts — no commit, no resequencing.
  await page.getByTestId(`abwab-tree-order-${doorA.id}`).click();
  await page.getByTestId(`abwab-tree-order-input-${doorA.id}`).fill('99');
  await page.getByTestId(`abwab-tree-order-input-${doorA.id}`).press('Escape');

  await expect(page.getByTestId(`abwab-tree-order-input-${doorA.id}`)).toBeHidden();
  await expect(page.getByTestId(`abwab-tree-order-${doorA.id}`)).toHaveText('2');
});

test('single move: into a destination, then back out as a main door', async ({ page, abwabSandbox }) => {
  const destination = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('move-dest') });
  const moved = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('move-me') });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);

  await page.getByTestId(`abwab-tree-row-${moved.id}`).click();
  await page.getByTestId('abwab-side-panel-op-move').click();
  await expect(page.getByTestId('abwab-move-picker')).toBeVisible();
  await page.getByTestId(`abwab-move-picker-section-${abwabSandbox.sectionId}`).click();
  await page.getByTestId(`abwab-move-picker-dest-${destination.id}`).click();
  await page.getByTestId('abwab-move-picker-confirm').click();
  await expect(page.getByTestId('abwab-move-picker')).toBeHidden();

  await page.getByTestId(`abwab-tree-chevron-${destination.id}`).click();
  await expect(page.getByTestId(`abwab-tree-row-${moved.id}`)).toBeVisible();

  // Move it back out to the top level ("كباب رئيسي") — still scoped to the same section.
  await page.getByTestId(`abwab-tree-row-${moved.id}`).click();
  await page.getByTestId('abwab-side-panel-op-move').click();
  await page.getByTestId(`abwab-move-picker-section-${abwabSandbox.sectionId}`).click();
  await page.getByTestId('abwab-move-picker-dest-asmain').click();
  await page.getByTestId('abwab-move-picker-confirm').click();
  await expect(page.getByTestId('abwab-move-picker')).toBeHidden();

  // Collapse the (now childless) destination and confirm the moved door stands on its own.
  await page.getByTestId(`abwab-tree-chevron-${destination.id}`).click();
  await expect(page.getByTestId(`abwab-tree-row-${moved.id}`)).toBeVisible();
});

test('bulk mode: select, bulk move, then the all-or-nothing bulk archive confirm', async ({ page, abwabSandbox }) => {
  const target = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('bulk-target') });
  const bulkA = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('bulk-a') });
  const bulkB = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('bulk-b') });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);

  await page.getByTestId('abwab-side-panel-bulk-toggle').click();
  await page.getByTestId(`abwab-tree-checkbox-${bulkA.id}`).click();
  await page.getByTestId(`abwab-tree-checkbox-${bulkB.id}`).click();
  await expect(page.getByTestId('abwab-side-panel-bulk-count')).toHaveText('2');
  await expect(page.getByTestId('abwab-side-panel-bulk-names')).toContainText(bulkA.name);
  await expect(page.getByTestId('abwab-side-panel-bulk-names')).toContainText(bulkB.name);

  await page.getByTestId('abwab-side-panel-bulk-move').click();
  await expect(page.getByTestId('abwab-move-picker')).toBeVisible();
  await page.getByTestId(`abwab-move-picker-section-${abwabSandbox.sectionId}`).click();
  await page.getByTestId(`abwab-move-picker-dest-${target.id}`).click();
  await page.getByTestId('abwab-move-picker-confirm').click();
  await expect(page.getByTestId('abwab-move-picker')).toBeHidden();

  await page.getByTestId(`abwab-tree-chevron-${target.id}`).click();
  await expect(page.getByTestId(`abwab-tree-row-${bulkA.id}`)).toBeVisible();
  await expect(page.getByTestId(`abwab-tree-row-${bulkB.id}`)).toBeVisible();

  // The move refetches and rebinds by id (§4.6) — both moved doors stay bulk-selected,
  // so the archive confirm below counts the same union of two live doors.
  await expect(page.getByTestId('abwab-side-panel-bulk-count')).toHaveText('2');
  await page.getByTestId('abwab-side-panel-bulk-archive').click();
  // Two doors is the Arabic dual, so the confirm reads «بابين» and carries no digit — the
  // union count is what is being asserted, in the form a reader actually sees.
  await expect(page.getByTestId('abwab-page-bulk-archive-confirm')).toContainText('سيتم أرشفة بابين');
  await page.getByTestId('abwab-page-bulk-archive-confirm-yes').click();

  await expect(page.getByTestId(`abwab-tree-row-${bulkA.id}`)).toHaveCount(0);
  await expect(page.getByTestId(`abwab-tree-row-${bulkB.id}`)).toHaveCount(0);

  // The rebind drops archived ids as well as missing ones (Slice D). Without this the set
  // kept both now-archived doors with freshly rebound versions, and the next submit 404'd.
  await expect(page.getByTestId('abwab-side-panel-bulk-count')).toHaveText('0');
});

test('row context menu offers exactly edit / add-child / move / relations / archive', async ({ page, abwabSandbox }) => {
  const door = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('ctx-menu') });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);
  await page.getByTestId(`abwab-tree-row-${door.id}`).click({ button: 'right' });

  const menu = page.getByTestId('abwab-page-context-menu');
  await expect(menu).toBeVisible();
  await expect(menu.getByTestId('abwab-page-ctx-edit')).toBeVisible();
  await expect(menu.getByTestId('abwab-page-ctx-add-child')).toBeVisible();
  await expect(menu.getByTestId('abwab-page-ctx-move')).toBeVisible();
  await expect(menu.getByTestId('abwab-page-ctx-relations')).toBeVisible();
  await expect(menu.getByTestId('abwab-page-ctx-archive')).toBeVisible();

  // Zero dead controls (plan.md §5.1) — pinned at the browser level, not just in specs. Relations
  // stopped being one when `abwab-relations` gave them a real read; protection has not.
  await expect(page.getByTestId('abwab-side-panel-op-protect')).toHaveCount(0);

  await page.getByTestId('abwab-page-ctx-backdrop').click();
  await expect(menu).toBeHidden();
});

test('the row hover actions: ⋯ opens the same menu without right-click, ＋ opens the child modal', async ({
  page,
  abwabSandbox,
}) => {
  // The design contract puts both on every row (abwab-tree-concept.html:114, 436-443); ⋯ is
  // the only mouse path to the row menu that is not right-click, and ＋ is the third
  // add-child path alongside the side panel and the menu (plan-slice-b.md §6.2).
  const door = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('row-actions') });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);

  // Both actions are hover-revealed per the contract, so they are genuinely not actionable
  // until the row is hovered — hovering first is the flow a user performs, not a workaround.
  const row = page.getByTestId(`abwab-tree-row-${door.id}`);
  await row.hover();
  await expect(page.getByTestId(`abwab-tree-more-${door.id}`)).toBeVisible();

  await page.getByTestId(`abwab-tree-more-${door.id}`).click();
  await expect(page.getByTestId('abwab-page-context-menu')).toBeVisible();
  await page.getByTestId('abwab-page-ctx-backdrop').click();
  await expect(page.getByTestId('abwab-page-context-menu')).toBeHidden();

  // The row is selected now, so the contract keeps its actions visible without a hover
  // (`.row.selected .actions` at abwab-tree-concept.html:114).
  await expect(page.getByTestId(`abwab-tree-add-child-${door.id}`)).toBeVisible();
  await page.getByTestId(`abwab-tree-add-child-${door.id}`).click();
  await expect(page.getByTestId('abwab-door-modal-context')).toHaveText(`سيُضاف تحت: «${door.name}»`);
});
