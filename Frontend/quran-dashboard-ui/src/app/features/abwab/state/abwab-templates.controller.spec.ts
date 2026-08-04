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
  applyTemplate?: () => Observable<ApiResponse<AbwabDoorDto[]>>;
}

function setup(fakeApi: FakeApi) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      AbwabTemplatesController,
      AbwabTemplatesFacade,
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
