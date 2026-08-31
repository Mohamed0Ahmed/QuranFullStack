import { execFileSync } from 'node:child_process';
import { resolve } from 'node:path';
import {
  devices,
  type APIRequestContext,
  type Page,
  type Route,
  type TestInfo,
} from '@playwright/test';

import { environment } from '../src/environments/environment.development';
import { readApiData } from './fixtures/api-envelope';
import { expectNoBlockingAccessibilityViolations } from './fixtures/accessibility';
import { expect, test } from './fixtures/auth';

const API_ORIGIN = environment.apiBaseUrl;
const PREPARE_LINKING = resolve(process.cwd(), 'e2e/prepare-linking.mjs');
const TARGET_DOOR_NAME = 'باب رحلة الربط الناجحة';
const {
  defaultBrowserType: _approvedBrowserType,
  ...APPROVED_LINKING_MOBILE
} = devices['Pixel 7'];

interface AbwabSection {
  id: number;
}

interface AbwabDoor {
  id: number;
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
  resourceKind: string;
  job: { jobId: string } | null;
  durableOutcome: DurableConfirmationOutcome | null;
}

interface ConfirmationRequest {
  preflightToken: string;
  idempotencyKey: string;
}

interface DurableConfirmationOutcome {
  idempotencyKey: string;
  status: string;
  result: {
    doorId: number;
    isNoOp: boolean;
  };
}

interface DoorLinkSnapshot {
  doorId: number;
  records: Array<{
    unitId: number;
    ayahs: Array<{ ayahId: number; selectedWordIds: number[]; descriptions: string[] }>;
  }>;
  ayahs: Array<{ ayahId: number; verseKey: string }>;
}

interface MushafAyahDoors {
  verseKey: string;
  doorIds: number[];
}

type ConfirmationVariant = 'accepted-job' | 'existing-job' | 'durable-outcome';

interface JourneyVariant {
  sourceVerseKey: string;
  sourceWordLocation: string;
  confirmation: ConfirmationVariant;
  targetSuffix: string;
  scanAccessibility: boolean;
}

test(
  'an Owner completes the successful Linking journey and fresh reads prove its projections',
  {
    annotation: [
      { type: 'critical' },
      { type: 'mutating' },
      { type: 'artifact', description: 'compact-cross-stack-base' },
      { type: 'journey', description: 'linking.successful-owner' },
    ],
  },
  async ({ page, request, ownerPersona }, testInfo) => {
    prepareLinking();
    await runSuccessfulLinkingJourney(
      page,
      request,
      ownerPersona.accessToken,
      testInfo,
      false,
      {
        sourceVerseKey: '1:1',
        sourceWordLocation: '1:1:1',
        confirmation: 'accepted-job',
        targetSuffix: 'desktop-accepted',
        scanAccessibility: true,
      },
    );
    await runSuccessfulLinkingJourney(
      page,
      request,
      ownerPersona.accessToken,
      testInfo,
      false,
      {
        sourceVerseKey: '1:2',
        sourceWordLocation: '1:2:1',
        confirmation: 'existing-job',
        targetSuffix: 'desktop-existing',
        scanAccessibility: false,
      },
    );
  },
);

test.describe('approved Linking mobile variant', () => {
  test.use({ ...APPROVED_LINKING_MOBILE });

  test(
    'the Owner Linking journey remains operable on the approved mobile viewport',
    {
      annotation: [
        { type: 'critical' },
        { type: 'mobile' },
        { type: 'mutating' },
        { type: 'artifact', description: 'compact-cross-stack-base' },
        { type: 'journey', description: 'linking.successful-owner-mobile' },
      ],
    },
    async ({ page, request, ownerPersona }, testInfo) => {
      prepareLinking();
      await runSuccessfulLinkingJourney(
        page,
        request,
        ownerPersona.accessToken,
        testInfo,
        true,
        {
          sourceVerseKey: '1:3',
          sourceWordLocation: '1:3:1',
          confirmation: 'durable-outcome',
          targetSuffix: 'mobile-durable',
          scanAccessibility: true,
        },
      );
    },
  );
});

async function runSuccessfulLinkingJourney(
  page: Page,
  request: APIRequestContext,
  ownerAccessToken: string,
  testInfo: TestInfo,
  mobile: boolean,
  variant: JourneyVariant,
): Promise<void> {
  const targetDoor = await createTargetDoor(request, ownerAccessToken, variant.targetSuffix);

  await page.goto('/dashboard/mushaf');
  const selectionMode = page.getByRole('button', { name: 'تحديد آيات' });
  await expect(selectionMode).toBeVisible();
  await selectionMode.focus();
  await expect(selectionMode).toBeFocused();
  await selectionMode.press('Enter');
  await expect(selectionMode).toHaveAttribute('aria-pressed', 'true');

  const sourceAyah = page.locator(`[data-word-location="${variant.sourceWordLocation}"]`);
  await expect(sourceAyah).toHaveAccessibleName(`تحديد الآية ${variant.sourceVerseKey}`);
  await sourceAyah.focus();
  await expect(sourceAyah).toBeFocused();
  await sourceAyah.press('Enter');
  await expect(sourceAyah).toHaveAttribute('aria-pressed', 'true');

  const directLink = page.getByRole('button', { name: 'ربط مباشر', exact: true });
  await expect(directLink).toBeEnabled();
  await directLink.focus();
  await expect(directLink).toBeFocused();
  await directLink.press('Enter');

  const dialog = page.getByTestId('linking-workspace');
  await expect(dialog).toBeVisible();
  const surfaceEntry = page.getByTestId('linking-workspace-surface-entry');
  await expect(surfaceEntry).toBeFocused();
  await expectDialogInsideViewport(page, dialog);

  const chooseDoor = dialog.getByRole('button', { name: 'اختر الباب', exact: true });
  await expect(chooseDoor).toBeEnabled();
  await chooseDoor.focus();
  await expect(chooseDoor).toBeFocused();
  await chooseDoor.press('Enter');

  const target = dialog.getByTestId(`abwab-tree-row-${targetDoor.id}`);
  await expect(target).toContainText(TARGET_DOOR_NAME);
  await target.focus();
  await expect(target).toBeFocused();
  if (variant.confirmation === 'existing-job') {
    await target.click();
  } else {
    await target.press('Enter');
  }
  await expect(target).toHaveAttribute('aria-selected', 'true');

  const preflightResponsePromise = page.waitForResponse(
    (response) =>
      response.request().method() === 'POST'
      && new URL(response.url()).pathname === '/api/linking/preflights',
  );
  const runPreflight = dialog.getByRole('button', {
    name: 'الفحص المسبق للربط',
    exact: true,
  });
  await expect(runPreflight).toBeEnabled();
  await runPreflight.focus();
  await expect(runPreflight).toBeFocused();
  await runPreflight.press('Enter');

  const acceptedPreflightResponse = await preflightResponsePromise;
  expect(acceptedPreflightResponse.status()).toBe(202);
  const acceptedPreflight = await readApiData<PreparedPreflight>(
    acceptedPreflightResponse,
    'create prepared preflight',
    202,
  );
  const confirm = dialog.getByRole('button', { name: 'تأكيد الربط', exact: true });
  await expect(confirm).toBeEnabled();

  const prepared = await readAuthorizedData<PreparedPreflight>(
    request,
    ownerAccessToken,
    `/api/linking/preflights/${acceptedPreflight.preflightId}`,
    'fresh prepared preflight',
  );
  expect(prepared).toMatchObject({
    status: 'ready',
    isBlocked: false,
    isNoOp: false,
    totalAyahs: 1,
  });
  expect(prepared.preflightToken).toBeTruthy();
  await expect(dialog.getByText('توجد عناصر غير صالحة تمنع التنفيذ.')).toHaveCount(0);
  await expectDialogInsideViewport(page, dialog);
  if (mobile) {
    await expect(dialog.getByRole('list', { name: 'مراحل الربط' })).toBeVisible();
  }

  await confirm.focus();
  await expect(confirm).toBeFocused();
  const removeConfirmationHarness = await installConfirmationVariant(
    page,
    request,
    ownerAccessToken,
    acceptedPreflight.preflightId,
    variant.confirmation,
  );
  const confirmationRequestPromise = page.waitForRequest(
    (candidate) =>
      candidate.method() === 'POST'
      && new URL(candidate.url()).pathname
        === `/api/linking/preflights/${acceptedPreflight.preflightId}/confirmation-jobs`,
  );
  const confirmationResponsePromise = page.waitForResponse(
    (response) =>
      response.request().method() === 'POST'
      && new URL(response.url()).pathname
        === `/api/linking/preflights/${acceptedPreflight.preflightId}/confirmation-jobs`,
  );
  await confirm.press('Enter');

  const confirmationRequest = (await confirmationRequestPromise).postDataJSON() as ConfirmationRequest;
  const acceptedConfirmation = await readApiData<ConfirmationSubmission>(
    await confirmationResponsePromise,
    'accept confirmation job',
    variant.confirmation === 'accepted-job' ? 202 : 200,
  );
  await removeConfirmationHarness();
  if (variant.confirmation === 'durable-outcome') {
    expect(acceptedConfirmation).toMatchObject({
      resourceKind: 'durable_outcome',
      job: null,
      durableOutcome: {
        idempotencyKey: confirmationRequest.idempotencyKey,
        status: 'succeeded',
        result: { doorId: targetDoor.id, isNoOp: false },
      },
    });
  } else {
    expect(acceptedConfirmation.resourceKind).toBe('job');
    expect(acceptedConfirmation.job?.jobId).toBeTruthy();
  }

  await expect(dialog.getByRole('heading', { name: 'تم الربط بنجاح' })).toBeVisible();
  if (variant.scanAccessibility) {
    await expectNoBlockingAccessibilityViolations(page, testInfo);
  }

  const durableOutcome = await readAuthorizedData<DurableConfirmationOutcome>(
    request,
    ownerAccessToken,
    `/api/linking/confirmation-outcomes/${confirmationRequest.idempotencyKey}`,
    'fresh durable confirmation outcome',
  );
  expect(durableOutcome).toMatchObject({
    idempotencyKey: confirmationRequest.idempotencyKey,
    status: 'succeeded',
    result: { doorId: targetDoor.id, isNoOp: false },
  });

  const existingResponse = await request.post(
    `${API_ORIGIN}/api/linking/preflights/${acceptedPreflight.preflightId}/confirmation-jobs`,
    {
      headers: bearerHeaders(ownerAccessToken),
      data: confirmationRequest,
    },
  );
  const existing = await readApiData<ConfirmationSubmission>(
    existingResponse,
    'reread existing confirmation resource',
    200,
  );
  expect(existing).toMatchObject({
    resourceKind: 'durable_outcome',
    job: null,
    durableOutcome: {
      idempotencyKey: confirmationRequest.idempotencyKey,
      status: 'succeeded',
      result: { doorId: targetDoor.id, isNoOp: false },
    },
  });

  const snapshot = await readPublicData<DoorLinkSnapshot>(
    request,
    `/api/abwab/doors/${targetDoor.id}/links/snapshot`,
    'fresh door-link snapshot',
  );
  expect(snapshot.doorId).toBe(targetDoor.id);
  expect(snapshot.records).toHaveLength(1);
  expect(snapshot.records[0]?.ayahs).toEqual([
    { ayahId: snapshot.ayahs[0]?.ayahId, selectedWordIds: [], descriptions: [] },
  ]);
  expect(snapshot.ayahs.map((ayah) => ayah.verseKey)).toEqual([variant.sourceVerseKey]);

  const mushafProjection = await readPublicData<MushafAyahDoors>(
    request,
    `/api/mushaf/ayahs/${variant.sourceVerseKey}/doors`,
    'fresh Mushaf ayah-to-door projection',
  );
  expect(mushafProjection).toEqual({
    verseKey: variant.sourceVerseKey,
    doorIds: [targetDoor.id],
  });

  const successPanel = dialog.getByRole('status').filter({ hasText: 'تم الربط بنجاح' });
  const close = successPanel.getByRole('button', { name: 'إغلاق', exact: true });
  await close.focus();
  await expect(close).toBeFocused();
  await close.press('Enter');
  await expect(dialog).toBeHidden();
}

async function installConfirmationVariant(
  page: Page,
  request: APIRequestContext,
  ownerAccessToken: string,
  preflightId: string,
  variant: ConfirmationVariant,
): Promise<() => Promise<void>> {
  if (variant === 'accepted-job') {
    return async () => undefined;
  }

  const confirmationUrl = `${API_ORIGIN}/api/linking/preflights/${preflightId}/confirmation-jobs`;
  const handler = async (route: Route): Promise<void> => {
    const confirmationRequest = route.request().postDataJSON() as ConfirmationRequest;
    const accepted = await readApiData<ConfirmationSubmission>(
      await route.fetch(),
      'create confirmation job behind browser harness',
      202,
    );
    expect(accepted.resourceKind).toBe('job');
    expect(accepted.job?.jobId).toBeTruthy();

    if (variant === 'existing-job') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        json: { isSuccess: true, message: null, data: accepted },
      });
      return;
    }

    await pollAuthorizedData<{ status: string }>(
      request,
      ownerAccessToken,
      `/api/linking/confirmation-jobs/${accepted.job!.jobId}`,
      'wait for confirmation before durable browser response',
      accepted.job!.jobId,
      (job) => job.status.toLowerCase() === 'succeeded',
    );
    const durableResponse = await request.post(confirmationUrl, {
      headers: bearerHeaders(ownerAccessToken),
      data: confirmationRequest,
    });
    const durable = await readApiData<ConfirmationSubmission>(
      durableResponse,
      'load durable confirmation for browser response',
      200,
    );
    expect(durable.resourceKind).toBe('durable_outcome');
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      json: { isSuccess: true, message: null, data: durable },
    });
  };

  await page.route(confirmationUrl, handler);
  return () => page.unroute(confirmationUrl, handler);
}

async function pollAuthorizedData<T>(
  request: APIRequestContext,
  ownerAccessToken: string,
  path: string,
  operation: string,
  jobId: string,
  completed: (data: T) => boolean,
): Promise<T> {
  const deadline = Date.now() + 15_000;
  let lastState = 'not-observed';
  while (Date.now() < deadline) {
    const data = await readAuthorizedData<T>(request, ownerAccessToken, path, operation);
    lastState = describeBusinessState(data);
    if (completed(data)) {
      return data;
    }
    await new Promise((resolveDelay) => setTimeout(resolveDelay, 50));
  }
  throw new Error(
    `${operation} timed out; jobId=${jobId}; lastState=${lastState}; `
    + 'sanitizedLogs=sealed application.log; response body omitted.',
  );
}

function describeBusinessState(data: unknown): string {
  if (typeof data !== 'object' || data === null || !('status' in data)) {
    return 'unknown';
  }
  const status = String(data.status).slice(0, 32);
  const stage = 'stage' in data ? String(data.stage).slice(0, 32) : 'unknown';
  return `status:${status},stage:${stage}`;
}

function prepareLinking(): void {
  execFileSync(process.execPath, [PREPARE_LINKING], {
    cwd: process.cwd(),
    stdio: 'inherit',
  });
}

async function createTargetDoor(
  request: APIRequestContext,
  ownerAccessToken: string,
  variant: string,
): Promise<AbwabDoor> {
  const sectionResponse = await request.post(`${API_ORIGIN}/api/abwab/sections`, {
    headers: bearerHeaders(ownerAccessToken),
    data: { name: `قسم رحلة الربط الناجحة ${variant}` },
  });
  const section = await readApiData<AbwabSection>(
    sectionResponse,
    'create Linking target section prerequisite',
    201,
  );

  const doorResponse = await request.post(`${API_ORIGIN}/api/abwab/doors`, {
    headers: bearerHeaders(ownerAccessToken),
    data: {
      sectionId: section.id,
      parentId: null,
      name: TARGET_DOOR_NAME,
      description: null,
      representativeAyahText: null,
      aliases: [],
    },
  });
  return readApiData<AbwabDoor>(doorResponse, 'create Linking target door prerequisite', 201);
}

async function expectDialogInsideViewport(page: Page, dialog: ReturnType<Page['locator']>): Promise<void> {
  const viewport = page.viewportSize();
  const box = await dialog.boundingBox();
  expect(viewport).not.toBeNull();
  expect(box).not.toBeNull();
  expect(box!.x).toBeGreaterThanOrEqual(0);
  expect(box!.y).toBeGreaterThanOrEqual(0);
  expect(box!.x + box!.width).toBeLessThanOrEqual(viewport!.width);
  expect(box!.y + box!.height).toBeLessThanOrEqual(viewport!.height);
}

function readAuthorizedData<T>(
  request: APIRequestContext,
  ownerAccessToken: string,
  path: string,
  operation: string,
): Promise<T> {
  return readRequestData<T>(request, path, operation, bearerHeaders(ownerAccessToken));
}

function readPublicData<T>(
  request: APIRequestContext,
  path: string,
  operation: string,
): Promise<T> {
  return readRequestData<T>(request, path, operation, {});
}

async function readRequestData<T>(
  request: APIRequestContext,
  path: string,
  operation: string,
  headers: Record<string, string>,
): Promise<T> {
  const response = await request.get(`${API_ORIGIN}${path}`, { headers });
  return readApiData<T>(response, operation, 200);
}

function bearerHeaders(accessToken: string): Record<string, string> {
  return { Authorization: `Bearer ${accessToken}` };
}
