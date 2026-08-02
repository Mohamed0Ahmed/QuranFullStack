import { expect, test } from './fixtures/abwab';

// URL state and a11y flow (plan-slice-b2.md T605, extended by slice E for audit item 11):
// the seven query keys round-trip through reload and Back/Forward, invalid values fail
// closed to their defaults, cards drill-down writes `card`, search matches by alias (not
// just name), a restorable overlay survives a reload and its retained state is reachable by
// Back, and the tree's ARIA/roving-tabindex/RTL keyboard model is exercised directly in the
// browser.

test('section, view, door, and q survive a reload and Back/Forward', async ({ page, abwabSandbox }) => {
  const door = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('url-state') });

  await page.goto(
    `/abwab?section=${abwabSandbox.sectionId}&view=cards&door=${door.id}&q=${encodeURIComponent(door.name)}`,
  );
  await expect(page.getByTestId(`abwab-card-${door.id}`)).toBeVisible();
  await expect(page.getByTestId('abwab-toolbar-search')).toHaveValue(door.name);
  await expect(page.getByTestId('abwab-side-panel-active-door')).toContainText(door.name);

  await page.reload();
  await expect(page.getByTestId(`abwab-card-${door.id}`)).toBeVisible();
  await expect(page.getByTestId('abwab-toolbar-search')).toHaveValue(door.name);
  await expect(page.getByTestId('abwab-side-panel-active-door')).toContainText(door.name);

  await page.getByTestId('abwab-toolbar-view-tree').click();
  await expect(page.getByTestId(`abwab-tree-row-${door.id}`)).toBeVisible();
  await expect(page).toHaveURL(/view=tree/);

  await page.goBack();
  await expect(page).toHaveURL(/view=cards/);
  await expect(page.getByTestId(`abwab-card-${door.id}`)).toBeVisible();

  await page.goForward();
  await expect(page).toHaveURL(/view=tree/);
  await expect(page.getByTestId(`abwab-tree-row-${door.id}`)).toBeVisible();
});

test('archive survives a reload and Back/Forward', async ({ page, abwabSandbox }) => {
  const door = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('url-archive') });
  await abwabSandbox.archiveDoor(door.id);

  await page.goto('/abwab?archive=1');
  await expect(page.getByTestId(`abwab-archive-row-${door.id}`)).toBeVisible();

  await page.reload();
  await expect(page.getByTestId(`abwab-archive-row-${door.id}`)).toBeVisible();

  await page.getByTestId('abwab-page-archive-toggle').click();
  await expect(page).not.toHaveURL(/archive=1/);
  await expect(page.getByTestId(`abwab-archive-row-${door.id}`)).toHaveCount(0);

  await page.goBack();
  await expect(page).toHaveURL(/archive=1/);
  await expect(page.getByTestId(`abwab-archive-row-${door.id}`)).toBeVisible();
});

test('invalid query values fail closed to their defaults', async ({ page, abwabSandbox }) => {
  await page.goto(`/abwab?section=${abwabSandbox.sectionId}&view=banana`);
  await expect(page.getByTestId('abwab-cards')).toHaveCount(0); // falls back to tree, not cards

  await page.goto('/abwab?section=0');
  await expect(page.getByTestId('abwab-toolbar-tab-all')).toHaveAttribute('aria-selected', 'true');

  await page.goto('/abwab?door=-1');
  await expect(page.getByTestId('abwab-side-panel-empty')).toBeVisible();
  await expect(page.getByTestId('abwab-side-panel-active-door')).toHaveCount(0);
});

test('invalid modal values fail closed, and a door-dependent kind needs a live door', async ({
  page,
  abwabSandbox,
}) => {
  const door = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('modal-fail-closed') });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}&door=${door.id}&modal=banana`);
  await expect(page.getByTestId('abwab-door-modal')).toHaveCount(0);
  await expect(page.getByTestId('abwab-page-modal-restore')).toHaveCount(0);

  // A door-dependent kind with no `door` at all has no subject to restore.
  await page.goto(`/abwab?section=${abwabSandbox.sectionId}&modal=edit`);
  await expect(page.getByTestId('abwab-door-modal')).toHaveCount(0);

  // The key is never rewritten — it stays in the URL, inert, exactly as a dead `door=` does.
  await expect(page).toHaveURL(/modal=edit/);
});

test('modal=<kind> survives a reload, and the retained state is reachable by Back', async ({
  page,
  abwabSandbox,
}) => {
  const door = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('modal-state') });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}&door=${door.id}`);
  await page.getByTestId(`abwab-tree-row-${door.id}`).click();
  await page.getByTestId('abwab-side-panel-op-edit').click();
  await expect(page.getByTestId('abwab-door-modal')).toBeVisible();
  await expect(page).toHaveURL(/modal=edit/);

  await page.reload();
  await expect(page.getByTestId('abwab-door-modal')).toBeVisible();

  // Escape retains: the modal closes, the key keeps it restorable, and focus lands on the
  // control that says so.
  await page.keyboard.press('Escape');
  await expect(page.getByTestId('abwab-door-modal')).toHaveCount(0);
  await expect(page).toHaveURL(/modal=edit-closed/);
  await expect(page.getByTestId('abwab-page-modal-restore')).toBeFocused();

  // Restore is a push, so Back returns to the closed state rather than skipping past it.
  await page.getByTestId('abwab-page-modal-restore').press('Enter');
  await expect(page.getByTestId('abwab-door-modal')).toBeVisible();
  // Visibility alone would not prove the trap re-armed — focus could still be sitting on the
  // control the restore destroyed. Slice C's `cdkTrapFocusAutoCapture` does the re-capture;
  // this asserts it landed, without reimplementing it.
  await expect(page.getByTestId('abwab-door-modal-name')).toBeFocused();

  await page.goBack();
  await expect(page.getByTestId('abwab-door-modal')).toHaveCount(0);
  await expect(page.getByTestId('abwab-page-modal-restore')).toBeVisible();

  await page.goForward();
  await expect(page.getByTestId('abwab-door-modal')).toBeVisible();
});

test('the restore control’s X discards the key entirely', async ({ page, abwabSandbox }) => {
  const door = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('modal-discard') });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}&door=${door.id}&modal=edit-closed`);
  await expect(page.getByTestId('abwab-page-modal-restore')).toBeVisible();

  await page.getByTestId('abwab-page-modal-discard').click();

  await expect(page).not.toHaveURL(/modal=/);
  await expect(page.getByTestId('abwab-page-modal-restore')).toHaveCount(0);
  await expect(page.getByTestId('abwab-door-modal')).toHaveCount(0);
  // The X removes itself, so focus is handed to the next header control rather than dropped
  // on `<body>` — a keyboard user keeps their place in the row.
  await expect(page.getByTestId('abwab-page-archive-toggle')).toBeFocused();
});

test('cards drill-down writes card=<id>, and the breadcrumb walks back', async ({ page, abwabSandbox }) => {
  const parent = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('cards-parent') });
  const child = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('cards-child'), parentId: parent.id });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}&view=cards`);
  await expect(page.getByTestId(`abwab-card-${parent.id}`)).toBeVisible();

  await page.getByTestId(`abwab-card-${parent.id}`).click();
  await expect(page).toHaveURL(new RegExp(`card=${parent.id}(&|$)`));
  await expect(page.getByTestId(`abwab-card-${child.id}`)).toBeVisible();
  await expect(page.getByTestId('abwab-cards-crumb').filter({ hasText: parent.name })).toBeVisible();

  await page.getByTestId('abwab-cards-crumb').first().click();
  await expect(page).not.toHaveURL(/card=/);
  await expect(page.getByTestId(`abwab-card-${parent.id}`)).toBeVisible();
});

test('search matches by alias, not just by name', async ({ page, abwabSandbox }) => {
  const aliasValue = abwabSandbox.uniqueName('special-alias');
  const doorWithAlias = await abwabSandbox.createDoor({
    name: abwabSandbox.uniqueName('alias-target'),
    aliases: [aliasValue],
  });
  const otherDoor = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('unrelated') });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}&q=${encodeURIComponent(aliasValue)}`);

  // ux-slice-l: the tree marks rather than filters, so the assertion moved from "the non-match
  // is gone" to "the non-match is still there, unmarked" — which is a stronger check of the
  // same thing (a false alias match would now mark the wrong row instead of merely keeping it).
  const matched = page.getByTestId(`abwab-tree-row-${doorWithAlias.id}`);
  const unmatched = page.getByTestId(`abwab-tree-row-${otherDoor.id}`);
  await expect(matched).toBeVisible();
  await expect(matched).toHaveClass(/abwab-tree__row--match/);
  await expect(unmatched).toBeVisible();
  await expect(unmatched).not.toHaveClass(/abwab-tree__row--match/);
  await expect(page.getByTestId('abwab-toolbar-search-count')).toHaveText('نتيجة واحدة');
});

test('tree a11y: roles, roving tabindex, RTL-mirrored arrows, Enter, and Shift+F10', async ({ page, abwabSandbox }) => {
  const rootA = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('a11y-root-a') });
  const rootAChild = await abwabSandbox.createDoor({
    name: abwabSandbox.uniqueName('a11y-root-a-child'),
    parentId: rootA.id,
  });
  const rootB = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('a11y-root-b') });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);

  const tree = page.getByTestId('abwab-tree');
  await expect(tree).toHaveAttribute('role', 'tree');
  expect(await tree.getAttribute('aria-label')).toBeTruthy();

  const rowA = page.getByTestId(`abwab-tree-row-${rootA.id}`);
  const rowAChild = page.getByTestId(`abwab-tree-row-${rootAChild.id}`);
  const rowB = page.getByTestId(`abwab-tree-row-${rootB.id}`);

  await expect(rowA).toHaveAttribute('aria-level', '1');
  await expect(rowA).toHaveAttribute('aria-expanded', 'false');
  await expect(page.locator('[data-testid^="abwab-tree-row-"][tabindex="0"]')).toHaveCount(1);
  await expect(rowA).toHaveAttribute('tabindex', '0');
  await expect(rowB).toHaveAttribute('tabindex', '-1');

  // ArrowDown/ArrowUp walk visible rows only — the collapsed child is skipped.
  await rowA.press('ArrowDown');
  await expect(rowB).toHaveAttribute('tabindex', '0');
  await expect(rowB).toBeFocused();
  await rowB.press('ArrowUp');
  await expect(rowA).toHaveAttribute('tabindex', '0');

  // RTL: ArrowLeft expands, then steps into the now-visible first child.
  await rowA.press('ArrowLeft');
  await expect(rowA).toHaveAttribute('aria-expanded', 'true');
  await expect(rowAChild).toBeVisible();
  await rowA.press('ArrowLeft');
  await expect(rowAChild).toHaveAttribute('tabindex', '0');
  await expect(rowAChild).toBeFocused();

  // RTL: ArrowRight steps out to the parent, then collapses it.
  await rowAChild.press('ArrowRight');
  await expect(rowA).toHaveAttribute('tabindex', '0');
  await rowA.press('ArrowRight');
  await expect(rowA).toHaveAttribute('aria-expanded', 'false');
  await expect(rowAChild).toHaveCount(0);

  // Enter selects the roving row.
  await rowA.press('Enter');
  await expect(page.getByTestId('abwab-side-panel-active-door')).toContainText(rootA.name);

  // Shift+F10 is the keyboard path to the row context menu.
  await rowA.press('Shift+F10');
  const menu = page.getByTestId('abwab-page-context-menu');
  await expect(menu).toBeVisible();

  // Slice L: the menu extends toward the anchor's inline-start, and the keyboard path anchors at
  // the focused row's inline-start edge — under RTL, the row's RIGHT edge. jsdom reports
  // zero-sized rects, so this placement contract can only be asserted in a real layout engine.
  const menuBox = (await menu.boundingBox())!;
  const rowBox = (await rowA.boundingBox())!;
  const viewport = page.viewportSize()!;
  expect(menuBox.x + menuBox.width).toBeLessThanOrEqual(rowBox.x + rowBox.width + 2);
  expect(menuBox.x).toBeGreaterThanOrEqual(0);
  expect(menuBox.x + menuBox.width).toBeLessThanOrEqual(viewport.width);
  expect(menuBox.y + menuBox.height).toBeLessThanOrEqual(viewport.height);
});
