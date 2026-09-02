import type { APIRequestContext, APIResponse, Page } from '@playwright/test';

import { environment } from '../src/environments/environment.development';
import { expectNoBlockingAccessibilityViolations } from './fixtures/accessibility';
import { expect, test } from './fixtures/auth';

const API_ORIGIN = environment.apiBaseUrl;
const PERMISSION_ACCESSIBLE_NAMES: Readonly<Record<string, string>> = {
  'abwab.doors.create': 'إنشاء الأبواب',
};

interface ApiEnvelope<T> {
  isSuccess: boolean;
  message: string | null;
  data: T | null;
}

interface AccessUserPermissions {
  status: string;
  permissionCodes: string[];
}

interface AccessAuditEvent {
  actionType: string;
  actorUserId: number | null;
  targetUserId: number;
  permissionCode: string | null;
}

interface AccessAuditEventPage {
  items: AccessAuditEvent[];
}

interface CurrentUser {
  status: string;
  isOwner: boolean;
  permissions: string[];
}

interface AbwabSection {
  id: number;
}

test(
  'an Owner grant, revoke, and disable control one exact non-Owner Permission',
  {
    annotation: [
      { type: 'critical' },
      { type: 'mutating' },
      { type: 'artifact', description: 'compact-cross-stack-base' },
      { type: 'journey', description: 'permission.lifecycle' },
    ],
  },
  async ({ page, request, permissionLifecyclePersona }, testInfo) => {
    const persona = permissionLifecyclePersona;
    const ownerPage = persona.ownerPage;
    const section = await createSectionPrerequisite(
      request,
      persona.ownerAccessToken,
      `قسم اختبار الصلاحيات ${persona.userId}`,
    );

    await openTargetAccount(ownerPage, persona.email);
    await expectNoBlockingAccessibilityViolations(ownerPage, testInfo);
    await replacePermissionThroughUi(
      ownerPage,
      persona.permission,
      true,
      'Grant the exact Permission through the Owner UI.',
    );

    const grantedPermissions = await readOwnerData<AccessUserPermissions>(
      request,
      persona.ownerAccessToken,
      `/api/access/users/${persona.userId}/permissions`,
    );
    expect(grantedPermissions.status).toBe('active');
    expect(grantedPermissions.permissionCodes).toEqual([persona.permission]);
    const grantAudit = await readAudit(
      request,
      persona.ownerAccessToken,
      persona.userId,
      'PermissionGranted',
      persona.permission,
    );
    expect(grantAudit).toHaveLength(1);
    expect(grantAudit[0]).toMatchObject({
      targetUserId: persona.userId,
      permissionCode: persona.permission,
    });

    await page.goto('/abwab');
    await expect(page.getByRole('button', { name: 'إضافة باب رئيسي' })).toBeVisible();
    const grantedMe = await readPersonaData<CurrentUser>(
      request,
      persona.accessToken,
      '/api/access/me',
    );
    expect(grantedMe).toMatchObject({
      status: 'active',
      isOwner: false,
      permissions: [persona.permission],
    });
    await expectProtectedDoorCreate(
      request,
      persona.accessToken,
      section.id,
      `باب بصلاحية مباشرة ${persona.userId}`,
      201,
    );

    await replacePermissionThroughUi(
      ownerPage,
      persona.permission,
      false,
      'Revoke the exact Permission through the Owner UI.',
    );
    await expect(page.getByRole('button', { name: 'إضافة باب رئيسي' })).toBeVisible();
    await expectProtectedDoorCreate(
      request,
      persona.accessToken,
      section.id,
      `باب بعد إلغاء الصلاحية ${persona.userId}`,
      403,
    );
    const revokedPermissions = await readOwnerData<AccessUserPermissions>(
      request,
      persona.ownerAccessToken,
      `/api/access/users/${persona.userId}/permissions`,
    );
    expect(revokedPermissions.permissionCodes).toEqual([]);
    expect(
      await readAudit(
        request,
        persona.ownerAccessToken,
        persona.userId,
        'PermissionRevoked',
        persona.permission,
      ),
    ).toHaveLength(1);
    await page.reload();
    await expect(page.getByRole('button', { name: 'إضافة باب رئيسي' })).toHaveCount(0);

    await replacePermissionThroughUi(
      ownerPage,
      persona.permission,
      true,
      'Restore the exact Permission before disabling the account.',
    );
    await page.reload();
    await expect(page.getByRole('button', { name: 'إضافة باب رئيسي' })).toBeVisible();
    await expectProtectedDoorCreate(
      request,
      persona.accessToken,
      section.id,
      `باب قبل تعطيل الحساب ${persona.userId}`,
      201,
    );

    await disableThroughUi(ownerPage, persona.userId);
    await expect(page.getByRole('button', { name: 'إضافة باب رئيسي' })).toBeVisible();
    await expectProtectedDoorCreate(
      request,
      persona.accessToken,
      section.id,
      `باب بعد تعطيل الحساب ${persona.userId}`,
      403,
    );
    await page.reload();
    await expect(page.getByRole('button', { name: 'إضافة باب رئيسي' })).toHaveCount(0);

    const disabledPermissions = await readOwnerData<AccessUserPermissions>(
      request,
      persona.ownerAccessToken,
      `/api/access/users/${persona.userId}/permissions`,
    );
    expect(disabledPermissions).toEqual(
      expect.objectContaining({ status: 'disabled', permissionCodes: [] }),
    );
    const lifecycleAudit = await readOwnerData<AccessAuditEventPage>(
      request,
      persona.ownerAccessToken,
      `/api/access/audit-events?targetUserId=${persona.userId}&pageSize=25`,
    );
    expect(
      lifecycleAudit.items
        .filter((event) =>
          ['PermissionGranted', 'PermissionRevoked', 'UserDisabled'].includes(event.actionType),
        )
        .reverse()
        .map((event) => [event.actionType, event.permissionCode]),
    ).toEqual([
      ['PermissionGranted', persona.permission],
      ['PermissionRevoked', persona.permission],
      ['PermissionGranted', persona.permission],
      ['PermissionRevoked', persona.permission],
      ['UserDisabled', null],
    ]);
  },
);

async function openTargetAccount(ownerPage: Page, email: string): Promise<void> {
  await ownerPage.goto('/settings/access');
  await expect(ownerPage.getByRole('heading', { name: 'إدارة الوصول', level: 1 })).toBeVisible();
  const search = ownerPage.getByRole('searchbox', { name: 'الاسم أو البريد' });
  await search.focus();
  await expect(search).toBeFocused();
  await search.fill(email);
  await search.press('Enter');
  await ownerPage.getByRole('button').filter({ hasText: email }).click();
  await expect(ownerPage.getByRole('checkbox', { name: 'إنشاء الأبواب' })).toBeVisible();
}

async function replacePermissionThroughUi(
  ownerPage: Page,
  permission: string,
  granted: boolean,
  reason: string,
): Promise<void> {
  const accessibleName = PERMISSION_ACCESSIBLE_NAMES[permission];
  if (accessibleName === undefined) {
    throw new Error(`No accessible Permission label is registered for ${permission}.`);
  }
  const checkbox = ownerPage.getByRole('checkbox', { name: accessibleName });
  await expect(checkbox).toBeVisible();
  if (granted) {
    await checkbox.check();
  } else {
    await checkbox.uncheck();
  }
  await ownerPage.getByRole('button', { name: 'مراجعة تعديل الصلاحيات' }).click();
  await expect(ownerPage.getByRole('region', { name: 'تأكيد الإجراء' })).toBeVisible();
  await ownerPage.getByRole('textbox', { name: 'سبب الإجراء (اختياري)' }).fill(reason);
  const responsePromise = ownerPage.waitForResponse(
    (response) =>
      response.request().method() === 'PUT'
      && response.url().includes('/api/access/users/')
      && response.url().endsWith('/permissions'),
  );
  await ownerPage.getByRole('button', { name: 'تأكيد', exact: true }).click();
  expect((await responsePromise).status()).toBe(200);
  await expect(ownerPage.getByText('تم حفظ التغيير.', { exact: true })).toBeVisible();
  if (granted) {
    await expect(checkbox).toBeChecked();
  } else {
    await expect(checkbox).not.toBeChecked();
  }
}

async function disableThroughUi(ownerPage: Page, userId: number): Promise<void> {
  await ownerPage.getByRole('button', { name: 'تعطيل الحساب' }).click();
  await expect(
    ownerPage.getByText('سيؤدي التعطيل إلى إزالة جميع الصلاحيات المباشرة.'),
  ).toBeVisible();
  await ownerPage
    .getByRole('textbox', { name: 'سبب الإجراء (اختياري)' })
    .fill('Disable the account through the Owner UI.');
  const responsePromise = ownerPage.waitForResponse(
    (response) =>
      response.request().method() === 'POST'
      && response.url().endsWith(`/api/access/users/${userId}/disable`),
  );
  await ownerPage.getByRole('button', { name: 'تأكيد', exact: true }).click();
  expect((await responsePromise).status()).toBe(200);
  await expect(ownerPage.getByText('الحساب معطّل ولا يحمل صلاحيات مباشرة')).toBeVisible();
}

async function createSectionPrerequisite(
  request: APIRequestContext,
  ownerAccessToken: string,
  name: string,
): Promise<AbwabSection> {
  const response = await request.post(`${API_ORIGIN}/api/abwab/sections`, {
    headers: bearerHeaders(ownerAccessToken),
    data: { name },
  });
  return readData<AbwabSection>(response, 'create Abwab section prerequisite', 201);
}

async function expectProtectedDoorCreate(
  request: APIRequestContext,
  accessToken: string,
  sectionId: number,
  name: string,
  expectedStatus: 201 | 403,
): Promise<void> {
  const response = await request.post(`${API_ORIGIN}/api/abwab/doors`, {
    headers: bearerHeaders(accessToken),
    data: {
      sectionId,
      parentId: null,
      name,
      description: null,
      representativeAyahText: null,
      aliases: [],
    },
  });
  expect(response.status()).toBe(expectedStatus);
  const envelope = (await response.json()) as ApiEnvelope<unknown>;
  await response.dispose();
  expect(envelope.isSuccess).toBe(expectedStatus === 201);
}

function readOwnerData<T>(
  request: APIRequestContext,
  ownerAccessToken: string,
  path: string,
): Promise<T> {
  return readAuthorizedData<T>(request, ownerAccessToken, path, 'Owner read');
}

function readPersonaData<T>(
  request: APIRequestContext,
  accessToken: string,
  path: string,
): Promise<T> {
  return readAuthorizedData<T>(request, accessToken, path, 'persona read');
}

async function readAuthorizedData<T>(
  request: APIRequestContext,
  accessToken: string,
  path: string,
  operation: string,
): Promise<T> {
  const response = await request.get(`${API_ORIGIN}${path}`, {
    headers: bearerHeaders(accessToken),
  });
  return readData<T>(response, operation, 200);
}

async function readAudit(
  request: APIRequestContext,
  ownerAccessToken: string,
  userId: number,
  actionType: string,
  permissionCode: string,
): Promise<AccessAuditEvent[]> {
  const query = new URLSearchParams({
    targetUserId: String(userId),
    actionType,
    permissionCode,
    pageSize: '25',
  });
  const page = await readOwnerData<AccessAuditEventPage>(
    request,
    ownerAccessToken,
    `/api/access/audit-events?${query}`,
  );
  return page.items;
}

async function readData<T>(
  response: APIResponse,
  operation: string,
  expectedStatus: number,
): Promise<T> {
  const status = response.status();
  const body = await response.text();
  await response.dispose();
  if (status !== expectedStatus) {
    throw new Error(`${operation} returned HTTP ${status}: ${body}`);
  }

  const envelope = JSON.parse(body) as ApiEnvelope<T>;
  if (!envelope.isSuccess || envelope.data === null) {
    throw new Error(`${operation} returned an unsuccessful API envelope: ${envelope.message ?? body}`);
  }
  return envelope.data;
}

function bearerHeaders(accessToken: string): Record<string, string> {
  return { Authorization: `Bearer ${accessToken}` };
}
