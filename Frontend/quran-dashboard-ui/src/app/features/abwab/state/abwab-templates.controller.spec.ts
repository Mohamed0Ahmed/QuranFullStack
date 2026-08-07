import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';

import { AbwabTemplatesController } from './abwab-templates.controller';
import { AbwabTemplatesFacade } from './abwab-templates.facade';
import { AbwabTemplatesApi } from '../data-access/abwab-templates.api';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabTemplateDto } from '../../../core/api/generated/models/abwab-template-dto';
import { AbwabTemplateSummaryDto } from '../../../core/api/generated/models/abwab-template-summary-dto';
import { AbwabDoorDto } from '../../../core/api/generated/models/abwab-door-dto';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { WriteAuthFailureCoordinator } from '../../../core/auth/write-auth-failure.coordinator';
import { PERMISSION_CODES, PermissionCode } from '../../../core/auth/permission-code';

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, message: 'تم', data };
}

function httpError(status: number, message: string): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: { isSuccess: false, message, data: null } });
}

const CREATED_TEMPLATE: AbwabTemplateDto = { id: 1, name: 'قالب الأخلاق', nodes: [] };

interface FakeApi {
  createTemplate?: () => Observable<ApiResponse<AbwabTemplateDto>>;
  deleteTemplate?: () => Observable<ApiResponse<unknown> | null>;
  addNode?: () => Observable<ApiResponse<unknown>>;
  editNode?: () => Observable<ApiResponse<unknown>>;
  reorderNode?: () => Observable<ApiResponse<unknown>>;
  deleteNode?: () => Observable<ApiResponse<unknown>>;
  applyTemplate?: () => Observable<ApiResponse<AbwabDoorDto[]>>;
}

function setup(
  fakeApi: FakeApi,
  options: {
    readonly permissions?: readonly PermissionCode[];
    readonly handleAuthFailure?: (error: unknown) => Promise<{ kind: 'unauthorized' | 'forbidden'; message: string | null } | null>;
  } = {},
) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      AbwabTemplatesController,
      AbwabTemplatesFacade,
      { provide: CurrentUserStore, useValue: { can: (permission: PermissionCode) => (options.permissions ?? PERMISSION_CODES).includes(permission) } },
      { provide: WriteAuthFailureCoordinator, useValue: { handle: options.handleAuthFailure ?? (async () => null) } },
      {
        provide: AbwabTemplatesApi,
        useValue: {
          ...fakeApi,
          getTemplates: () =>
            of(new HttpResponse({ body: ok<AbwabTemplateSummaryDto[]>([]) })),
        },
      },
    ],
  });
  return { controller: TestBed.inject(AbwabTemplatesController) };
}

describe('AbwabTemplatesController', () => {
  describe('Phase 9 last-dispatch permission guard', () => {
    const fields = { name: 'عقدة', description: '', representativeAyahText: '', aliases: [] };
    const deniedCases: readonly {
      readonly name: string;
      readonly request: (controller: AbwabTemplatesController) => Observable<unknown>;
      readonly fake: (call: ReturnType<typeof vi.fn>) => FakeApi;
    }[] = [
      { name: 'template create', request: (controller) => controller.createTemplate('قالب'), fake: (call) => ({ createTemplate: call }) },
      { name: 'template delete', request: (controller) => controller.deleteTemplate(1), fake: (call) => ({ deleteTemplate: call }) },
      { name: 'template node create', request: (controller) => controller.addNode(1, 2, fields), fake: (call) => ({ addNode: call }) },
      { name: 'template node edit', request: (controller) => controller.editNode(2, fields), fake: (call) => ({ editNode: call }) },
      { name: 'template node reorder', request: (controller) => controller.reorderNode(2, 3), fake: (call) => ({ reorderNode: call }) },
      { name: 'template node delete', request: (controller) => controller.deleteNode(2), fake: (call) => ({ deleteNode: call }) },
      { name: 'template apply', request: (controller) => controller.applyTemplate(1, [2]), fake: (call) => ({ applyTemplate: call }) },
    ];

    it.each(deniedCases)('does not dispatch $name for a read-only visitor', ({ request, fake }) => {
      const apiCall = vi.fn();
      const { controller } = setup(fake(apiCall), { permissions: [] });
      let outcome: unknown;

      request(controller).subscribe((value) => (outcome = value));

      expect(apiCall).not.toHaveBeenCalled();
      expect(outcome).toEqual({ kind: 'forbidden', message: ABWAB_LABELS.writePermissionDenied });
    });

    it('refreshes through the auth coordinator on a 403 without retrying template apply', async () => {
      const applyTemplate = vi.fn().mockReturnValue(throwError(() => httpError(403, 'ممنوع')));
      const handleAuthFailure = vi.fn().mockResolvedValue({ kind: 'forbidden', message: 'ممنوع' });
      const { controller } = setup(
        { applyTemplate },
        { permissions: ['abwab.templates.apply'], handleAuthFailure },
      );
      let outcome: unknown;

      controller.applyTemplate(1, [2]).subscribe((value) => (outcome = value));
      await Promise.resolve();
      await Promise.resolve();

      expect(applyTemplate).toHaveBeenCalledTimes(1);
      expect(handleAuthFailure).toHaveBeenCalledTimes(1);
      expect(outcome).toEqual({ kind: 'forbidden', message: 'ممنوع' });
    });
  });

  // F-51, applied to the templates half. Same contract as AbwabWriteController: a failure
  // reaches exactly ONE live region, and both sides are pinned so neither a double
  // announcement nor a silenced failure can pass.
  describe('F-51 — a template write failure reaches exactly one live region', () => {
    it('drops the announcer for a failed apply, whose copy modal reserves its alert region (abwab-template-copy-modal.component.html)', () => {
      const { controller } = setup({
        applyTemplate: () => throwError(() => httpError(409, 'يوجد باب بنفس الاسم تحت الهدف')),
      });

      let outcome: unknown;
      controller.applyTemplate(1, [2, 3]).subscribe((result) => (outcome = result));

      expect(outcome).toEqual({ kind: 'conflict', message: 'يوجد باب بنفس الاسم تحت الهدف' });
      expect(controller.announcement()).toBeNull();
    });

    it('drops the announcer for an apply the backend refuses inside the envelope, for the same reserved region', () => {
      const { controller } = setup({
        applyTemplate: () => of({ isSuccess: false, message: 'القالب فارغ', data: null }),
      });

      let outcome: unknown;
      controller.applyTemplate(1, [2]).subscribe((result) => (outcome = result));

      expect(outcome).toEqual({ kind: 'invalid', message: 'القالب فارغ' });
      expect(controller.announcement()).toBeNull();
    });

    it('keeps the announcer for a failed template delete, whose confirm inserts its alert rather than reserving it', () => {
      const { controller } = setup({
        deleteTemplate: () => throwError(() => httpError(409, 'تعذر حذف القالب')),
      });

      controller.deleteTemplate(1).subscribe();

      expect(controller.announcement()).toBe('تعذر حذف القالب');
    });

    it('keeps the announcer for a failed create, whose failure the templates page renders nowhere at all', () => {
      const { controller } = setup({
        createTemplate: () => throwError(() => httpError(400, 'اسم القالب مطلوب')),
      });

      controller.createTemplate('قالب').subscribe();

      expect(controller.announcement()).toBe('اسم القالب مطلوب');
    });
  });

  describe('success announcements', () => {
    it('announces a successful apply with the counted target phrase', () => {
      const { controller } = setup({
        applyTemplate: () => of(ok<AbwabDoorDto[]>([])),
      });

      controller.applyTemplate(1, [2, 3, 4]).subscribe();

      expect(controller.announcement()).toBe(ABWAB_LABELS.templateAppliedAnnouncement(3));
    });

    it('announces a successful create', () => {
      const { controller } = setup({
        createTemplate: () => of(ok(CREATED_TEMPLATE)),
      });

      controller.createTemplate('قالب الأخلاق').subscribe();

      expect(controller.announcement()).toBe(ABWAB_LABELS.templateCreatedAnnouncement);
    });
  });
});
