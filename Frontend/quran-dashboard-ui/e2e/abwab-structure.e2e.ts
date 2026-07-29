import { abwabTestIdNumber, abwabTreeRowByName, expect, test } from './fixtures/abwab';

// Structure flow (plan-slice-b2.md T602): sections (rename, delete-blocked-by-live-door),
// root/child door creation through the modal including alias chips, editing, and the
// dirty guard. Each test owns its own sandbox section (e2e/fixtures/abwab.ts) — no shared
// state between tests, no global counts (R18).

test('sections modal: rename updates the tab, and delete is blocked by a live door', async ({
  page,
  abwabSandbox,
}) => {
  await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('door-for-delete-block') });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);
  await expect(page.getByTestId('abwab-page')).toBeVisible();

  await page.getByTestId('abwab-page-manage-sections').click();
  await expect(page.getByTestId('abwab-sections-modal')).toBeVisible();

  const renamedName = abwabSandbox.uniqueName('renamed');
  await page.getByTestId(`abwab-sections-modal-rename-${abwabSandbox.sectionId}`).click();
  await page.getByTestId(`abwab-sections-modal-rename-input-${abwabSandbox.sectionId}`).fill(renamedName);
  await page.getByTestId(`abwab-sections-modal-rename-save-${abwabSandbox.sectionId}`).click();
  await expect(page.getByTestId(`abwab-sections-modal-row-${abwabSandbox.sectionId}`)).toContainText(renamedName);

  // The section still holds the live door created above — the backend's own conflict
  // copy is what actually renders (abwab.api.ts write controller prefers it over the
  // plan's locked fallback string; features/abwab/README.md "Gotchas" records this).
  await page.getByTestId(`abwab-sections-modal-delete-${abwabSandbox.sectionId}`).click();
  await expect(page.getByTestId('abwab-sections-modal-error')).toHaveText(
    'لا يمكن حذف القسم لاحتوائه على أبواب حالية',
  );

  await page.getByTestId('abwab-sections-modal-close').click();
  await expect(page.getByTestId(`abwab-toolbar-tab-${abwabSandbox.sectionId}`)).toContainText(renamedName);
});

test('root and child door creation via the modal, including alias chips', async ({ page, abwabSandbox }) => {
  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);
  await expect(page.getByTestId('abwab-page')).toBeVisible();

  const rootName = abwabSandbox.uniqueName('root');
  await page.getByTestId('abwab-page-add-root').click();
  await expect(page.getByTestId('abwab-door-modal')).toBeVisible();
  await expect(page.getByTestId('abwab-door-modal-context')).toHaveText('سيُضاف كباب رئيسي');

  await page.getByTestId('abwab-door-modal-name').fill(rootName);

  const aliasInput = page.getByTestId('abwab-door-modal-alias-input');
  await aliasInput.fill('alias-to-remove');
  await aliasInput.press('Enter');
  const removableChip = page.getByTestId('qd-chip').filter({ hasText: 'alias-to-remove' });
  await expect(removableChip).toBeVisible();
  await removableChip.getByTestId('qd-chip-remove').click();
  await expect(removableChip).toBeHidden();

  await aliasInput.fill('alias-kept');
  await aliasInput.press('Enter');
  await expect(page.getByTestId('qd-chip').filter({ hasText: 'alias-kept' })).toBeVisible();

  await page.getByTestId('abwab-door-modal-save').click();
  await expect(page.getByTestId('abwab-door-modal')).toBeHidden();

  const rootRow = abwabTreeRowByName(page, rootName);
  await expect(rootRow).toBeVisible();
  await rootRow.click();
  await expect(page.getByTestId('abwab-side-panel-active-door')).toContainText(rootName);

  const childName = abwabSandbox.uniqueName('child');
  await page.getByTestId('abwab-side-panel-op-add-child').click();
  await expect(page.getByTestId('abwab-door-modal-context')).toHaveText(`سيُضاف تحت: «${rootName}»`);
  await page.getByTestId('abwab-door-modal-name').fill(childName);
  await page.getByTestId('abwab-door-modal-save').click();
  await expect(page.getByTestId('abwab-door-modal')).toBeHidden();

  const rootId = await abwabTestIdNumber(rootRow);
  await page.getByTestId(`abwab-tree-chevron-${rootId}`).click();
  await expect(abwabTreeRowByName(page, childName)).toBeVisible();
});

test('editing a door, and the dirty guard blocks an unsaved close', async ({ page, abwabSandbox }) => {
  const originalName = abwabSandbox.uniqueName('edit-me');
  const door = await abwabSandbox.createDoor({ name: originalName });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);
  await page.getByTestId(`abwab-tree-row-${door.id}`).click();
  await expect(page.getByTestId('abwab-side-panel-active-door')).toContainText(originalName);

  await page.getByTestId('abwab-side-panel-op-edit').click();
  await expect(page.getByTestId('abwab-door-modal-context')).toHaveText(`تعديل «${originalName}»`);

  await page.getByTestId('abwab-door-modal-name').fill(`${originalName}-typo`);
  await page.getByTestId('abwab-door-modal-cancel').click();
  await expect(page.getByTestId('abwab-door-modal-discard-confirm')).toBeVisible();

  // "-no" keeps editing: the confirm goes away and the save/cancel footer is back, not
  // just "the modal is still visible" (the confirm block replaces that footer in the DOM).
  await page.getByTestId('abwab-door-modal-discard-confirm-no').click();
  await expect(page.getByTestId('abwab-door-modal-discard-confirm')).toBeHidden();
  await expect(page.getByTestId('abwab-door-modal-save')).toBeVisible();

  await page.getByTestId('abwab-door-modal-cancel').click();
  await page.getByTestId('abwab-door-modal-discard-confirm-yes').click();
  await expect(page.getByTestId('abwab-door-modal')).toBeHidden();

  await page.getByTestId(`abwab-tree-row-${door.id}`).click();
  await page.getByTestId('abwab-side-panel-op-edit').click();
  const renamedName = abwabSandbox.uniqueName('edited');
  await page.getByTestId('abwab-door-modal-name').fill(renamedName);
  await page.getByTestId('abwab-door-modal-save').click();
  await expect(page.getByTestId('abwab-door-modal')).toBeHidden();
  await expect(page.getByTestId(`abwab-tree-row-${door.id}`)).toContainText(renamedName);
});
