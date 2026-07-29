import { expect, test } from './fixtures/abwab';

// Archive/restore flow (plan-slice-b2.md T604). The archive view is not section-filtered
// (`abwab-page.component.ts`'s `archivedRoots` reads the whole snapshot, unlike the live
// tree's `visibleRoots`), so every assertion here addresses rows by the id the fixture
// itself created — never by position, text search, or a count of "how many are archived"
// (R21: residue from other runs/workers lives in that view permanently).
//
// The tests below archive their SETUP doors through the API (`abwabSandbox.archiveDoor`)
// because how a door became archived is a precondition, not the thing under test. The first
// test below is the one that drives archive through the UI itself (M18).

const API_BASE = 'https://localhost:5015';

test('archiving a door through the UI removes it from the live tree and reports no error', async ({
  page,
  abwabSandbox,
}) => {
  // The regression this pins: `DELETE api/abwab/doors/{id}` answers `204 No Content`, which
  // HttpClient hands to the write controller as a `null` envelope. Dereferencing it threw,
  // the throw was swallowed as a transport failure, and the UI showed an error while the
  // backend write had already committed. Every Vitest mock returned a well-formed envelope,
  // so only a browser run could catch it.
  const door = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('ui-archive') });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);
  await page.getByTestId(`abwab-tree-row-${door.id}`).click();
  await page.getByTestId('abwab-side-panel-op-archive').click();
  await page.getByTestId('abwab-page-archive-confirm-yes').click();

  await expect(page.getByTestId(`abwab-tree-row-${door.id}`)).toHaveCount(0);
  await expect(page.getByTestId('abwab-announcer')).toHaveText('');

  // …and it really is archived, not merely hidden by a failed refresh.
  await page.getByTestId('abwab-page-archive-toggle').click();
  await expect(page.getByTestId(`abwab-archive-row-${door.id}`)).toBeVisible();
});

test('an archived door renders in the archive view, and restoring returns it to the live tree', async ({
  page,
  abwabSandbox,
}) => {
  const door = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('archive-restore') });
  await abwabSandbox.archiveDoor(door.id);

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);
  await expect(page.getByTestId(`abwab-tree-row-${door.id}`)).toHaveCount(0);

  await page.getByTestId('abwab-page-archive-toggle').click();
  await expect(page.getByTestId('abwab-archive-view')).toBeVisible();
  const archiveRow = page.getByTestId(`abwab-archive-row-${door.id}`);
  await expect(archiveRow).toBeVisible();
  await expect(page.getByTestId(`abwab-archive-restore-${door.id}`)).toBeEnabled();

  await page.getByTestId(`abwab-archive-restore-${door.id}`).click();
  await expect(archiveRow).toHaveCount(0);

  await page.getByTestId('abwab-page-archive-toggle').click();
  await expect(page.getByTestId(`abwab-tree-row-${door.id}`)).toBeVisible();
});

test('a child archived under an archived parent shows a disabled restore until the parent comes back', async ({
  page,
  abwabSandbox,
}) => {
  const parent = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('archive-parent') });
  const child = await abwabSandbox.createDoor({
    name: abwabSandbox.uniqueName('archive-child'),
    parentId: parent.id,
  });

  // Archived in two SEPARATE operations (not one subtree archive) — matching descendants
  // by their own deleted_at means restoring the parent later must not silently pull the
  // child back too (plan.md §13.1).
  await abwabSandbox.archiveDoor(child.id);
  await abwabSandbox.archiveDoor(parent.id);

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);
  await page.getByTestId('abwab-page-archive-toggle').click();
  await expect(page.getByTestId(`abwab-archive-row-${parent.id}`)).toBeVisible();
  await expect(page.getByTestId(`abwab-archive-restore-${parent.id}`)).toBeEnabled();

  await page.getByTestId(`abwab-archive-chevron-${parent.id}`).click();
  await expect(page.getByTestId(`abwab-archive-row-${child.id}`)).toBeVisible();
  await expect(page.getByTestId(`abwab-archive-restore-${child.id}`)).toBeDisabled();
  await expect(page.getByTestId(`abwab-archive-restore-hint-${child.id}`)).toHaveText('استرجع الأب أولًا');

  // Restore the parent — the separately archived child stays archived (§13.1) but its
  // restore control flips to enabled the moment its parent is live again (M21/M22).
  await page.getByTestId(`abwab-archive-restore-${parent.id}`).click();
  await expect(page.getByTestId(`abwab-archive-row-${parent.id}`)).toHaveCount(0);
  await expect(page.getByTestId(`abwab-archive-restore-${child.id}`)).toBeEnabled();
  await expect(page.getByTestId(`abwab-archive-restore-hint-${child.id}`)).toHaveCount(0);

  await page.getByTestId(`abwab-archive-restore-${child.id}`).click();
  await expect(page.getByTestId(`abwab-archive-row-${child.id}`)).toHaveCount(0);

  await page.getByTestId('abwab-page-archive-toggle').click();
  await page.getByTestId(`abwab-tree-chevron-${parent.id}`).click();
  await expect(page.getByTestId(`abwab-tree-row-${child.id}`)).toBeVisible();
});

test('restoring a door whose section was deleted meanwhile announces the detach', async ({
  page,
  abwabSandbox,
  request,
}) => {
  const door = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('detach') });
  await abwabSandbox.archiveDoor(door.id);

  // The section is now empty of live doors, so this delete is legal — and it is the one
  // step the shared fixture does not do on the test's behalf (R20: this flow deliberately
  // removes its own section mid-test, so the fixture's teardown has nothing left to
  // delete and must tolerate the 404 rather than failing).
  const deleteRes = await request.delete(`${API_BASE}/api/abwab/sections/${abwabSandbox.sectionId}`);
  expect(deleteRes.ok(), `section delete should succeed once empty of live doors (${deleteRes.status()})`).toBeTruthy();

  await page.goto(`/abwab?archive=1`);
  await page.getByTestId(`abwab-archive-restore-${door.id}`).click();
  await expect(page.getByTestId('abwab-announcer')).toHaveText('استُرجع الباب خارج قسمه المحذوف');
});
