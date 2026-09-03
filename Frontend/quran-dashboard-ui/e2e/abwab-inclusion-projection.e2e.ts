import { execFileSync } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import { resolve } from 'node:path';
import type { APIRequestContext, APIResponse } from '@playwright/test';

import { ABWAB_PERMISSION_CODES } from '../src/app/core/auth/permission-codes.generated';
import { environment } from '../src/environments/environment.development';
import { readApiData } from './fixtures/api-envelope';
import { expectNoBlockingAccessibilityViolations } from './fixtures/accessibility';
import { expect, test } from './fixtures/auth';

const API_ORIGIN = environment.apiBaseUrl;
const PREPARE_LINKING = resolve(process.cwd(), 'e2e/prepare-linking.mjs');
const SOURCE_NAME = 'Abwab inclusion source';
const TARGET_NAME = 'Abwab inclusion target';
const INCLUSION_PERMISSION = ABWAB_PERMISSION_CODES.inclusions.create;
const CYCLE_MESSAGE = 'ستؤدي إضافة أبواب المصدر إلى دورة غير صالحة بين الأبواب';
const STALE_NOTICE = 'تغير الباب المستهدف. تم تحديث مصادر الباب، فراجع اختيارك قبل المحاولة مجددًا.';

interface AccessUserDetail {
  id: number;
  status: string;
  version: number;
  permissionCodes: string[];
}

interface AccessUserPermissions {
  status: string;
  version: number;
  permissionCodes: string[];
}

interface CurrentUser {
  status: string;
  isOwner: boolean;
  permissions: string[];
}

interface AbwabSection {
  id: number;
}

interface AbwabDoor {
  id: number;
}

interface AbwabTreeDoor {
  id: number;
  version: number;
  inclusionSourceCount: number;
  inclusionConsumerCount: number;
}

interface AbwabTree {
  version: string | null;
  doors: AbwabTreeDoor[];
}

interface InclusionTopology {
  doorId: number;
  doorVersion: number;
  sources: Array<{ doorId: number; doorName: string }>;
}

interface DoorLinkSnapshot {
  records: unknown[];
  ayahs: Array<{ verseKey: string }>;
}

interface MushafAyahDoors {
  verseKey: string;
  doorIds: number[];
}

interface PreparedPreflight {
  preflightId: string;
  preflightToken: string | null;
  status: string;
  isBlocked: boolean;
  isNoOp: boolean;
  totalAyahs: number | null;
}

interface ConfirmationSubmission {
  job: { jobId: string } | null;
}

test(
  'a least-privilege non-Owner adds an Abwab source and fresh public reads prove its Mushaf projection',
  {
    annotation: [
      { type: 'critical' },
      { type: 'mutating' },
      { type: 'artifact', description: 'compact-cross-stack-base' },
      { type: 'journey', description: 'abwab.inclusion-projection' },
    ],
  },
  async ({ page, request, permissionLifecyclePersona }, testInfo) => {
    prepareLinking();
    const persona = permissionLifecyclePersona;
    const source = await createDoorPrerequisite(request, persona.ownerAccessToken, SOURCE_NAME);
    const target = await createDoorPrerequisite(request, persona.ownerAccessToken, TARGET_NAME);
    await linkSourcePrerequisite(request, persona.ownerAccessToken, source.id);

    await grantExactInclusionPermission(
      request,
      persona.ownerAccessToken,
      persona.userId,
    );
    const identity = await readAuthorizedData<CurrentUser>(
      request,
      persona.accessToken,
      '/api/access/me',
      'least-privilege actor identity',
    );
    expect(identity).toMatchObject({
      status: 'active',
      isOwner: false,
      permissions: [INCLUSION_PERMISSION],
    });

    const before = await readPublicTree(request, 'tree before inclusion');
    const targetBefore = findDoor(before.data, target.id);
    expect(targetBefore.inclusionSourceCount).toBe(0);

    await page.goto(`/abwab?modal=inclusions-${target.id}`);
    const dialog = page.getByRole('dialog', { name: 'إدارة مصادر الباب', exact: true });
    await expect(dialog).toBeVisible();
    const startAdd = dialog.getByRole('button', { name: 'إضافة أبواب مصدر', exact: true });
    await expect(startAdd).toBeEnabled();
    await startAdd.focus();
    await expect(startAdd).toBeFocused();
    await startAdd.press('Enter');

    const sourceDoor = dialog.getByRole('treeitem', { name: new RegExp(SOURCE_NAME) });
    await expect(sourceDoor).toBeVisible();
    await sourceDoor.focus();
    await expect(sourceDoor).toBeFocused();
    await sourceDoor.press('Enter');
    await expect(sourceDoor).toHaveAttribute('aria-selected', 'true');

    const mutationResponse = page.waitForResponse((response) =>
      response.request().method() === 'POST'
      && new URL(response.url()).pathname === `/api/abwab/doors/${target.id}/inclusions`);
    const submit = dialog.getByRole('button', { name: 'إضافة باب مصدر', exact: true });
    await expect(submit).toBeEnabled();
    await submit.focus();
    await expect(submit).toBeFocused();
    await submit.press('Enter');
    expect((await mutationResponse).status()).toBe(201);
    await expect(dialog.getByText(SOURCE_NAME, { exact: true })).toBeVisible();
    await expectNoBlockingAccessibilityViolations(page, testInfo);

    await page.reload();
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText(SOURCE_NAME, { exact: true })).toBeVisible();

    const freshTree = await readPublicTree(request, 'fresh tree after inclusion');
    expect(freshTree.etag).not.toBe(before.etag);
    expect(freshTree.data.version).not.toBe(before.data.version);
    const targetAfter = findDoor(freshTree.data, target.id);
    expect(targetAfter.version).toBeGreaterThan(targetBefore.version);
    expect(targetAfter.inclusionSourceCount).toBe(1);
    expect(findDoor(freshTree.data, source.id).inclusionConsumerCount).toBe(1);

    const conditional = await request.get(`${API_ORIGIN}/api/abwab/tree`, {
      headers: { 'If-None-Match': before.etag },
    });
    expect(conditional.status()).toBe(200);
    expect(conditional.headers()['etag']).toBe(freshTree.etag);
    await conditional.dispose();

    const topology = await readPublicData<InclusionTopology>(
      request,
      `/api/abwab/doors/${target.id}/inclusions`,
      'fresh inclusion detail',
    );
    expect(topology).toMatchObject({
      doorId: target.id,
      doorVersion: targetAfter.version,
      sources: [{ doorId: source.id, doorName: SOURCE_NAME }],
    });

    const snapshot = await readPublicData<DoorLinkSnapshot>(
      request,
      `/api/abwab/doors/${target.id}/links/snapshot`,
      'fresh target link snapshot',
    );
    expect(snapshot.records).toHaveLength(1);
    expect(snapshot.ayahs).toMatchObject([{ verseKey: '1:1' }]);

    const projection = await readPublicData<MushafAyahDoors>(
      request,
      '/api/mushaf/ayahs/1:1/doors',
      'fresh Mushaf projection',
    );
    expect(projection).toEqual({
      verseKey: '1:1',
      doorIds: [source.id, target.id].sort((left, right) => left - right),
    });
  },
);

test(
  'a revoked inclusion Permission removes stale write controls without readable or durable state drift',
  {
    annotation: [
      { type: 'critical' },
      { type: 'mutating' },
      { type: 'artifact', description: 'compact-cross-stack-base' },
      { type: 'journey', description: 'abwab.inclusion-revoked-permission' },
    ],
  },
  async ({ page, request, permissionLifecyclePersona }, testInfo) => {
    prepareLinking();
    const persona = permissionLifecyclePersona;
    const source = await createDoorPrerequisite(request, persona.ownerAccessToken, SOURCE_NAME);
    const target = await createDoorPrerequisite(request, persona.ownerAccessToken, TARGET_NAME);
    await linkSourcePrerequisite(request, persona.ownerAccessToken, source.id);
    await grantExactInclusionPermission(request, persona.ownerAccessToken, persona.userId);

    const beforeTree = await readPublicTree(request, 'tree before revoked inclusion');
    const beforeTopology = await readPublicData<InclusionTopology>(
      request,
      `/api/abwab/doors/${target.id}/inclusions`,
      'topology before revoked inclusion',
    );
    const beforeSnapshot = await readPublicData<DoorLinkSnapshot>(
      request,
      `/api/abwab/doors/${target.id}/links/snapshot`,
      'target links before revoked inclusion',
    );
    const beforeProjection = await readPublicData<MushafAyahDoors>(
      request,
      '/api/mushaf/ayahs/1:1/doors',
      'Mushaf projection before revoked inclusion',
    );

    await page.goto(`/abwab?modal=inclusions-${target.id}`);
    const dialog = page.getByRole('dialog', { name: 'إدارة مصادر الباب', exact: true });
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: 'إضافة أبواب مصدر', exact: true }).click();
    const sourceDoor = dialog.getByRole('treeitem', { name: new RegExp(SOURCE_NAME) });
    await sourceDoor.press('Enter');
    await expect(sourceDoor).toHaveAttribute('aria-selected', 'true');

    const intercepted = deferred<void>();
    const release = deferred<void>();
    await page.route(`**/api/abwab/doors/${target.id}/inclusions`, async (route) => {
      if (route.request().method() !== 'POST') {
        await route.continue();
        return;
      }
      intercepted.resolve();
      await release.promise;
      await route.continue();
    });
    const mutationResponse = page.waitForResponse((response) =>
      response.request().method() === 'POST'
      && new URL(response.url()).pathname === `/api/abwab/doors/${target.id}/inclusions`);
    await dialog.getByRole('button', { name: 'إضافة باب مصدر', exact: true }).click();
    await intercepted.promise;
    await revokeInclusionPermission(request, persona.ownerAccessToken, persona.userId);
    release.resolve();
    expect((await mutationResponse).status()).toBe(403);

    await expect(dialog).toBeVisible();
    await expect(page).toHaveURL(new RegExp(`/abwab\\?modal=inclusions-${target.id}$`));
    await expect(dialog.getByText(TARGET_NAME, { exact: true })).toBeVisible();
    await expect(dialog.getByRole('button', { name: 'إضافة أبواب مصدر', exact: true })).toHaveCount(0);
    await expect(dialog.getByRole('button', { name: 'إغلاق', exact: true })).toBeFocused();

    expect(await readPublicTree(request, 'tree after revoked inclusion')).toEqual(beforeTree);
    expect(await readPublicData<InclusionTopology>(
      request,
      `/api/abwab/doors/${target.id}/inclusions`,
      'topology after revoked inclusion',
    )).toEqual(beforeTopology);
    expect(await readPublicData<DoorLinkSnapshot>(
      request,
      `/api/abwab/doors/${target.id}/links/snapshot`,
      'target links after revoked inclusion',
    )).toEqual(beforeSnapshot);
    expect(await readPublicData<MushafAyahDoors>(
      request,
      '/api/mushaf/ayahs/1:1/doors',
      'Mushaf projection after revoked inclusion',
    )).toEqual(beforeProjection);
    await expectNoBlockingAccessibilityViolations(page, testInfo);
  },
);

test(
  'semantic inclusion conflict remains distinct from evidence-proven stale target feedback',
  {
    annotation: [
      { type: 'critical' },
      { type: 'mutating' },
      { type: 'artifact', description: 'compact-cross-stack-base' },
      { type: 'journey', description: 'abwab.inclusion-conflict-evidence' },
    ],
  },
  async ({ page, request, permissionLifecyclePersona }, testInfo) => {
    const persona = permissionLifecyclePersona;
    const cycleSource = await createDoorPrerequisite(request, persona.ownerAccessToken, 'Abwab cycle source');
    const staleSource = await createDoorPrerequisite(request, persona.ownerAccessToken, 'Abwab stale source');
    const target = await createDoorPrerequisite(request, persona.ownerAccessToken, TARGET_NAME);
    const cycleSourceVersion = findDoor(
      (await readPublicTree(request, 'tree before reverse inclusion')).data,
      cycleSource.id,
    ).version;
    await addInclusionPrerequisite(
      request,
      persona.ownerAccessToken,
      cycleSource.id,
      cycleSourceVersion,
      target.id,
    );
    await grantExactInclusionPermission(request, persona.ownerAccessToken, persona.userId);

    await page.goto(`/abwab?modal=inclusions-${target.id}`);
    const dialog = page.getByRole('dialog', { name: 'إدارة مصادر الباب', exact: true });
    await expect(dialog).toBeVisible();
    const startAdd = dialog.getByRole('button', { name: 'إضافة أبواب مصدر', exact: true });
    await startAdd.focus();
    await expect(startAdd).toBeFocused();
    await startAdd.press('Enter');
    await dialog.getByRole('treeitem', { name: /Abwab cycle source/ }).press('Enter');
    const cycleResponse = page.waitForResponse((response) =>
      response.request().method() === 'POST'
      && new URL(response.url()).pathname === `/api/abwab/doors/${target.id}/inclusions`);
    await dialog.getByRole('button', { name: 'إضافة باب مصدر', exact: true }).click();
    expect((await cycleResponse).status()).toBe(409);
    await expect(dialog.getByTestId('abwab-inclusions-modal-write-error')).toContainText(CYCLE_MESSAGE);
    await expect(dialog.getByTestId('abwab-inclusions-modal-notice')).not.toContainText(STALE_NOTICE);

    await page.reload();
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: 'إضافة أبواب مصدر', exact: true }).click();
    await dialog.getByRole('treeitem', { name: /Abwab stale source/ }).press('Enter');

    const intercepted = deferred<void>();
    const release = deferred<void>();
    await page.route(`**/api/abwab/doors/${target.id}/inclusions`, async (route) => {
      if (route.request().method() !== 'POST') {
        await route.continue();
        return;
      }
      intercepted.resolve();
      await release.promise;
      await route.continue();
    });
    const staleResponse = page.waitForResponse((response) =>
      response.request().method() === 'POST'
      && new URL(response.url()).pathname === `/api/abwab/doors/${target.id}/inclusions`);
    await dialog.getByRole('button', { name: 'إضافة باب مصدر', exact: true }).click();
    await intercepted.promise;
    const targetVersion = findDoor(
      (await readPublicTree(request, 'tree before external target edit')).data,
      target.id,
    ).version;
    await renameDoorPrerequisite(
      request,
      persona.ownerAccessToken,
      target.id,
      targetVersion,
      `${TARGET_NAME} externally changed`,
    );
    release.resolve();
    expect((await staleResponse).status()).toBe(409);

    await expect(dialog.getByTestId('abwab-inclusions-modal-notice')).toContainText(STALE_NOTICE);
    await expect(dialog.getByTestId('abwab-inclusions-modal-write-error')).toHaveCount(0);
    await expectNoBlockingAccessibilityViolations(page, testInfo);
  },
);

function prepareLinking(): void {
  execFileSync(process.execPath, [PREPARE_LINKING], {
    cwd: process.cwd(),
    stdio: 'inherit',
  });
}

async function createDoorPrerequisite(
  request: APIRequestContext,
  ownerAccessToken: string,
  name: string,
): Promise<AbwabDoor> {
  const section = await readAuthorizedData<AbwabSection>(
    request,
    ownerAccessToken,
    '/api/abwab/sections',
    `create ${name} section prerequisite`,
    { name: `Section ${name}` },
  );
  return readAuthorizedData<AbwabDoor>(
    request,
    ownerAccessToken,
    '/api/abwab/doors',
    `create ${name} door prerequisite`,
    {
      sectionId: section.id,
      parentId: null,
      name,
      description: null,
      representativeAyahText: null,
      aliases: [],
    },
  );
}

async function linkSourcePrerequisite(
  request: APIRequestContext,
  ownerAccessToken: string,
  sourceDoorId: number,
): Promise<void> {
  const resolved = await readAuthorizedData<{ linkingDataRevision: number }>(
    request,
    ownerAccessToken,
    '/api/linking/sources/resolve-page',
    'resolve source-link prerequisite',
    {
      descriptor: manualSourceDescriptor(),
      expectedLinkingDataRevision: null,
      expectedSourceViewIdentity: null,
      view: {
        segment: 'all',
        inclusionMode: null,
        ayahOverrideIds: [],
        typeCodes: [],
      },
      page: 1,
      pageSize: 100,
    },
    200,
  );
  const accepted = await readAuthorizedData<PreparedPreflight>(
    request,
    ownerAccessToken,
    '/api/linking/preflights',
    'create source-link preflight prerequisite',
    {
      preparationKey: randomUUID(),
      doorId: sourceDoorId,
      expectedLinkingDataRevision: resolved.linkingDataRevision,
      sources: [{
        orderValue: 1,
        workspaceSource: null,
        inlineSource: {
          descriptor: manualSourceDescriptor(),
          configuration: {
            inclusionMode: 'all_except',
            ayahOverrideIds: [],
            selectedWords: [],
            automaticWordMatchesEnabled: null,
            manualLinkShape: 'independent',
            descriptions: [],
          },
        },
      }],
    },
    202,
  );
  const prepared = await pollAuthorizedData<PreparedPreflight>(
    request,
    ownerAccessToken,
    `/api/linking/preflights/${accepted.preflightId}`,
    'wait for source-link preflight',
    accepted.preflightId,
    (candidate) => candidate.status === 'ready',
  );
  expect(prepared).toMatchObject({ isBlocked: false, isNoOp: false, totalAyahs: 1 });
  expect(prepared.preflightToken).toBeTruthy();

  const submission = await readAuthorizedData<ConfirmationSubmission>(
    request,
    ownerAccessToken,
    `/api/linking/preflights/${accepted.preflightId}/confirmation-jobs`,
    'confirm source-link prerequisite',
    { preflightToken: prepared.preflightToken, idempotencyKey: randomUUID() },
    202,
  );
  expect(submission.job?.jobId).toBeTruthy();
  await pollAuthorizedData<{ status: string }>(
    request,
    ownerAccessToken,
    `/api/linking/confirmation-jobs/${submission.job!.jobId}`,
    'wait for source-link confirmation',
    submission.job!.jobId,
    (candidate) => candidate.status === 'succeeded',
  );
}

async function grantExactInclusionPermission(
  request: APIRequestContext,
  ownerAccessToken: string,
  userId: number,
): Promise<void> {
  const detail = await readPublicData<AccessUserDetail>(
    request,
    `/api/access/users/${userId}`,
    'load least-privilege actor',
    bearerHeaders(ownerAccessToken),
  );
  const response = await request.put(`${API_ORIGIN}/api/access/users/${userId}/permissions`, {
    headers: bearerHeaders(ownerAccessToken),
    data: {
      expectedVersion: detail.version,
      permissionCodes: [INCLUSION_PERMISSION],
      reason: 'E2E Abwab inclusion projection prerequisite.',
    },
  });
  const permissions = await readApiData<AccessUserPermissions>(
    response,
    'grant exact inclusion permission',
    200,
  );
  expect(permissions).toMatchObject({
    status: 'active',
    permissionCodes: [INCLUSION_PERMISSION],
  });
}

async function revokeInclusionPermission(
  request: APIRequestContext,
  ownerAccessToken: string,
  userId: number,
): Promise<void> {
  const detail = await readPublicData<AccessUserDetail>(
    request,
    `/api/access/users/${userId}`,
    'load actor before inclusion Permission revocation',
    bearerHeaders(ownerAccessToken),
  );
  const response = await request.put(`${API_ORIGIN}/api/access/users/${userId}/permissions`, {
    headers: bearerHeaders(ownerAccessToken),
    data: {
      expectedVersion: detail.version,
      permissionCodes: [],
      reason: 'E2E revoked Abwab inclusion Permission.',
    },
  });
  const permissions = await readApiData<AccessUserPermissions>(
    response,
    'revoke inclusion permission',
    200,
  );
  expect(permissions.permissionCodes).toEqual([]);
}

async function addInclusionPrerequisite(
  request: APIRequestContext,
  ownerAccessToken: string,
  targetDoorId: number,
  expectedTargetDoorVersion: number,
  sourceDoorId: number,
): Promise<void> {
  await readAuthorizedData(
    request,
    ownerAccessToken,
    `/api/abwab/doors/${targetDoorId}/inclusions`,
    'create reverse inclusion prerequisite',
    { expectedTargetDoorVersion, sourceDoorIds: [sourceDoorId] },
  );
}

async function renameDoorPrerequisite(
  request: APIRequestContext,
  ownerAccessToken: string,
  doorId: number,
  version: number,
  name: string,
): Promise<void> {
  const response = await request.put(`${API_ORIGIN}/api/abwab/doors/${doorId}`, {
    headers: bearerHeaders(ownerAccessToken),
    data: {
      name,
      description: null,
      representativeAyahText: null,
      aliases: [],
      version,
    },
  });
  await readApiData<AbwabDoor>(response, 'externally update target door', 200);
}

async function readPublicTree(
  request: APIRequestContext,
  operation: string,
): Promise<{ etag: string; data: AbwabTree }> {
  const response = await request.get(`${API_ORIGIN}/api/abwab/tree`);
  const etag = response.headers()['etag'];
  expect(etag, `${operation} requires a tree ETag`).toBeTruthy();
  return { etag: etag!, data: await readApiData<AbwabTree>(response, operation, 200) };
}

function findDoor(tree: AbwabTree, doorId: number): AbwabTreeDoor {
  const door = tree.doors.find((candidate) => candidate.id === doorId);
  if (door === undefined) {
    throw new Error(`Public tree omitted expected door ${doorId}.`);
  }
  return door;
}

function manualSourceDescriptor(): object {
  return {
    kind: 'manual-mushaf-ayahs',
    label: 'First Fatiha ayah',
    manualAyahs: [{ verseKey: '1:1', pageNumber: 1, displayHint: '1:1' }],
    contextKey: null,
  };
}

async function readPublicData<T>(
  request: APIRequestContext,
  path: string,
  operation: string,
  headers: Record<string, string> = {},
): Promise<T> {
  const response = await request.get(`${API_ORIGIN}${path}`, { headers });
  return readApiData<T>(response, operation, 200);
}

async function readAuthorizedData<T>(
  request: APIRequestContext,
  accessToken: string,
  path: string,
  operation: string,
  data?: object,
  expectedStatus = 201,
): Promise<T> {
  const response = data === undefined
    ? await request.get(`${API_ORIGIN}${path}`, { headers: bearerHeaders(accessToken) })
    : await request.post(`${API_ORIGIN}${path}`, {
      headers: bearerHeaders(accessToken),
      data,
    });
  return readApiData<T>(response, operation, data === undefined ? 200 : expectedStatus);
}

async function pollAuthorizedData<T>(
  request: APIRequestContext,
  accessToken: string,
  path: string,
  operation: string,
  resourceId: string,
  completed: (data: T) => boolean,
): Promise<T> {
  const deadline = Date.now() + 15_000;
  let lastState = 'not-observed';
  while (Date.now() < deadline) {
    const data = await readAuthorizedData<T>(request, accessToken, path, operation);
    lastState = describeBusinessState(data);
    if (completed(data)) {
      return data;
    }
    await new Promise((resolveDelay) => setTimeout(resolveDelay, 50));
  }
  throw new Error(`${operation} timed out; resourceId=${resourceId}; lastState=${lastState}.`);
}

function describeBusinessState(data: unknown): string {
  if (typeof data !== 'object' || data === null || !('status' in data)) {
    return 'unknown';
  }
  return `status:${String(data.status).slice(0, 32)}`;
}

function bearerHeaders(accessToken: string): Record<string, string> {
  return { Authorization: `Bearer ${accessToken}` };
}

function deferred<T>(): {
  readonly promise: Promise<T>;
  readonly resolve: (value: T | PromiseLike<T>) => void;
} {
  let resolvePromise!: (value: T | PromiseLike<T>) => void;
  const promise = new Promise<T>((resolve) => {
    resolvePromise = resolve;
  });
  return { promise, resolve: resolvePromise };
}
