import type { Page } from '@playwright/test';

import { expect, test } from './fixtures/abwab';

// Relations, end to end (TESTING_DEBT row 5, fired by slice K's rewrite of the modal's read path).
// Two things only a browser proves: dormancy crossing the read, the write, the count and the row
// flag in one pass; and the client-side list cache actually not asking twice.
//
// Requests are counted with a PASSIVE `page.on('request')` listener rather than `page.route`:
// a "zero requests were issued" claim must not be made by a mechanism that re-issues the request
// itself, and nothing here needs to modify or fulfil anything. Network idle is not a substitute —
// it cannot tell "asked and got a fast answer" from "never asked".
function countRelationReads(page: Page): () => number {
  let reads = 0;
  page.on('request', (request) => {
    if (request.method() === 'GET' && /\/api\/abwab\/doors\/\d+\/relations$/.test(request.url())) {
      reads += 1;
    }
  });
  return () => reads;
}

async function openRelationsFor(page: Page, doorId: number): Promise<void> {
  await page.getByTestId(`abwab-tree-row-${doorId}`).click();
  await page.getByTestId('abwab-side-panel-op-relations').click();
  await expect(page.getByTestId('abwab-relations-modal')).toBeVisible();
}

const EMPTY_FLAG = /abwab-tree__flag--empty/;

test('a relation shows on both doors, goes dormant when one endpoint is archived, and returns on restore', async ({
  page,
  abwabSandbox,
}) => {
  const anchor = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('rel-anchor') });
  const partner = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('rel-partner') });

  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);
  await openRelationsFor(page, anchor.id);

  // Slice K: a door the snapshot says has no relations answers immediately, from the snapshot.
  await expect(page.getByTestId('abwab-relations-modal-empty')).toBeVisible();

  await page.getByTestId(`abwab-relations-modal-pick-${partner.id}`).click();
  await page.getByTestId('abwab-relations-modal-add').click();
  await expect(page.getByTestId('abwab-relations-modal-group-similarity')).toContainText(partner.name);
  await page.getByTestId('abwab-relations-modal-close').click();

  // The relation is mutual, so it is on both rows' flags and in both doors' lists — the whole
  // reason the read is a symmetric union rather than a column.
  await expect(page.getByTestId(`abwab-tree-flag-rel-${anchor.id}`)).not.toHaveClass(EMPTY_FLAG);
  await expect(page.getByTestId(`abwab-tree-flag-rel-${partner.id}`)).not.toHaveClass(EMPTY_FLAG);

  await openRelationsFor(page, partner.id);
  await expect(page.getByTestId('abwab-relations-modal-group-similarity')).toContainText(anchor.name);
  await page.getByTestId('abwab-relations-modal-close').click();

  // Dormancy: a relation whose other endpoint is archived counts 0 and is not listed, without
  // anything having deleted it. The archived door leaves the live tree altogether, so the flag
  // that can still be asserted on is the SURVIVING door's.
  await abwabSandbox.archiveDoor(partner.id);
  await page.reload();

  await expect(page.getByTestId(`abwab-tree-flag-rel-${anchor.id}`)).toHaveClass(EMPTY_FLAG);
  await openRelationsFor(page, anchor.id);
  await expect(page.getByTestId('abwab-relations-modal-empty')).toBeVisible();
  await page.getByTestId('abwab-relations-modal-close').click();

  // Restored through the UI, because the restore is a real write here: it bumps the tree
  // generation, which is what must evict the client's cached «no relations» answer.
  await page.getByTestId('abwab-page-archive-toggle').click();
  await page.getByTestId(`abwab-archive-restore-${partner.id}`).click();
  await page.getByTestId('qd-confirm-dialog-confirm').click();
  await page.getByTestId('abwab-page-archive-toggle').click();

  await expect(page.getByTestId(`abwab-tree-flag-rel-${anchor.id}`)).not.toHaveClass(EMPTY_FLAG);
  await openRelationsFor(page, anchor.id);
  await expect(page.getByTestId('abwab-relations-modal-group-similarity')).toContainText(partner.name);
});

test('a door is asked about once, a zero-relation door is never asked about, and a rename still lands', async ({
  page,
  abwabSandbox,
}) => {
  const anchor = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('cache-anchor') });
  const partner = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('cache-partner') });
  const lonely = await abwabSandbox.createDoor({ name: abwabSandbox.uniqueName('cache-lonely') });
  await abwabSandbox.addRelation(anchor.id, [partner.id]);

  const relationReads = countRelationReads(page);
  await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);

  await openRelationsFor(page, anchor.id);
  await expect(page.getByTestId('abwab-relations-modal-group-similarity')).toContainText(partner.name);
  expect(relationReads()).toBe(1);
  await page.getByTestId('abwab-relations-modal-close').click();

  // The cache's whole claim. `request` events fire when a request STARTS, so a rendered list is
  // proof enough that any second read would already have been counted.
  await openRelationsFor(page, anchor.id);
  await expect(page.getByTestId('abwab-relations-modal-group-similarity')).toContainText(partner.name);
  expect(relationReads()).toBe(1);
  await page.getByTestId('abwab-relations-modal-close').click();

  // And the count discriminator: a door the snapshot says has none is answered without asking.
  await openRelationsFor(page, lonely.id);
  await expect(page.getByTestId('abwab-relations-modal-empty')).toBeVisible();
  expect(relationReads()).toBe(1);
  await page.getByTestId('abwab-relations-modal-close').click();

  // A rename behind the app's back is the case a per-door or count-based cache key would miss:
  // the partner's name changes inside the anchor's list while no count moves anywhere. A fresh
  // page holds no cache at all, so a SECOND read is expected here — what is being pinned is that
  // the name on screen is the current one.
  const renamed = abwabSandbox.uniqueName('cache-partner-renamed');
  await abwabSandbox.renameDoor(partner.id, renamed);
  await page.reload();

  await openRelationsFor(page, anchor.id);
  await expect(page.getByTestId('abwab-relations-modal-group-similarity')).toContainText(renamed);
  expect(relationReads()).toBe(2);
});
