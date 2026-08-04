import { expect, test } from './fixtures/abwab';

// Archive/restore flow. The archive view is not section-filtered
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

  // M18: the confirm names the live-subtree count derived from the snapshot — one leaf door
  // here, in the singular Arabic form.
  await expect(page.getByTestId('abwab-page-archive-confirm')).toContainText('سيتم أرشفة باب واحد');

  await page.getByTestId('abwab-page-archive-confirm-confirm').click();

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
  await page.getByTestId('abwab-door-restore-confirm-confirm').click();
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
  // child back too.
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
  await page.getByTestId('abwab-door-restore-confirm-confirm').click();
  await expect(page.getByTestId(`abwab-archive-row-${parent.id}`)).toHaveCount(0);
  await expect(page.getByTestId(`abwab-archive-restore-${child.id}`)).toBeEnabled();
  await expect(page.getByTestId(`abwab-archive-restore-hint-${child.id}`)).toHaveCount(0);

  await page.getByTestId(`abwab-archive-restore-${child.id}`).click();
  await page.getByTestId('abwab-door-restore-confirm-confirm').click();
  await expect(page.getByTestId(`abwab-archive-row-${child.id}`)).toHaveCount(0);

  await page.getByTestId('abwab-page-archive-toggle').click();
  await page.getByTestId(`abwab-tree-chevron-${parent.id}`).click();
  await expect(page.getByTestId(`abwab-tree-row-${child.id}`)).toBeVisible();
});

test('restoring a door whose section was deleted meanwhile demands a destination', async ({
  page,
  abwabSandbox,
  request,
}) => {
  const door = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('stranded') });
  await abwabSandbox.archiveDoor(door.id);

  // A replacement section, created BEFORE the original is retired — the restore has nowhere to
  // go otherwise, and the modal would only be able to say so.
  const replacementName = abwabSandbox.uniqueName('replacement-section');
  const replacementRes = await request.post(`${API_BASE}/api/abwab/sections`, { data: { name: replacementName } });
  expect(replacementRes.ok(), `replacement section create failed (${replacementRes.status()})`).toBeTruthy();
  const replacementId = (await replacementRes.json())['data']['id'] as number;

  // The original section is now empty of live doors, so this delete is legal — and it is the one
  // step the shared fixture does not do on the test's behalf (R20: this flow deliberately
  // removes its own section mid-test, so the fixture's teardown has nothing left to
  // delete and must tolerate the 404 rather than failing).
  const deleteRes = await request.delete(`${API_BASE}/api/abwab/sections/${abwabSandbox.sectionId}`);
  expect(deleteRes.ok(), `section delete should succeed once empty of live doors (${deleteRes.status()})`).toBeTruthy();

  await page.goto(`/abwab?archive=1`);
  // The archive view is tabless, so a retired section changes nothing about where the door is
  // listed — only what its restore has to ask for.
  await expect(page.getByTestId(`abwab-archive-row-${door.id}`)).toBeVisible();

  await page.getByTestId(`abwab-archive-restore-${door.id}`).click();
  await expect(page.getByTestId('abwab-door-restore-modal-retired-hint')).toBeVisible();
  await expect(page.getByTestId('abwab-door-restore-confirm-confirm')).toBeDisabled();

  await page.getByTestId('abwab-door-restore-modal-section-select').selectOption(String(replacementId));
  await page.getByTestId('abwab-door-restore-confirm-confirm').click();

  await expect(page.getByTestId('abwab-door-restore-confirm')).toBeHidden();
  await expect(page.getByTestId('abwab-announcer')).toHaveText('استُرجع الباب');

  // It landed in the replacement section, not merely somewhere: the door is in that tab's tree.
  await page.goto(`/abwab?section=${replacementId}`);
  await expect(page.getByTestId(`abwab-tree-row-${door.id}`)).toBeVisible();
});
