import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { AbwabTemplatesApi } from '../data-access/abwab-templates.api';
import { AbwabTemplateDto } from '../../../core/api/generated/models/abwab-template-dto';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabTemplatesFacade } from './abwab-templates.facade';

function templateResponse(id: number, name: string): ApiResponse<AbwabTemplateDto> {
  return {
    isSuccess: true,
    message: 'تم',
    data: {
      id,
      name,
      nodes: [
        {
          id: id * 100,
          parentNodeId: null,
          name,
          description: null,
          representativeAyahText: null,
          aliases: [],
          orderValue: 1,
        },
      ],
    },
  };
}

function setup(getTemplate: (id: number) => Observable<ApiResponse<AbwabTemplateDto>>): AbwabTemplatesFacade {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      AbwabTemplatesFacade,
      {
        provide: AbwabTemplatesApi,
        useValue: {
          getTemplates: () => of({ isSuccess: true, message: 'تم', data: [] }),
          getTemplate,
        },
      },
    ],
  });
  return TestBed.inject(AbwabTemplatesFacade);
}

describe('AbwabTemplatesFacade', () => {
  it('select() puts the fetched template on the view model', () => {
    const facade = setup((id) => of(templateResponse(id, 'قالب أ')));
    facade.select(1);

    expect(facade.selectedTemplateId()).toBe(1);
    expect(facade.selectedTemplate()?.id).toBe(1);
    expect(facade.selectedTemplate()?.name).toBe('قالب أ');
  });

  it('a failed switch reports the error and shows no template, never the previous one', () => {
    const facade = setup((id) =>
      id === 1 ? of(templateResponse(1, 'قالب أ')) : throwError(() => new Error('gone')),
    );
    facade.select(1);
    facade.select(2);

    expect(facade.selectedTemplateId()).toBe(2);
    expect(facade.selectedErrorMessage()).toBe(ABWAB_LABELS.templateLoadError);
    expect(facade.selectedTemplate()).toBeNull();
  });

  describe('selectedLoading — the in-flight window is stated, not silent', () => {
    it('is true while the fetch is in flight, with no template and no error to show', () => {
      let emit: ((response: ApiResponse<AbwabTemplateDto>) => void) | null = null;
      const facade = setup(
        () =>
          new Observable<ApiResponse<AbwabTemplateDto>>((subscriber) => {
            emit = (response) => {
              subscriber.next(response);
              subscriber.complete();
            };
          }),
      );

      facade.select(1);
      expect(facade.selectedLoading()).toBe(true);
      expect(facade.selectedTemplate()).toBeNull();
      expect(facade.selectedErrorMessage()).toBeNull();

      emit!(templateResponse(1, 'قالب أ'));
      expect(facade.selectedLoading()).toBe(false);
      expect(facade.selectedTemplate()?.id).toBe(1);
    });

    it('clears on failure, so the error is what the page shows', () => {
      const facade = setup(() => throwError(() => new Error('offline')));
      facade.select(1);

      expect(facade.selectedLoading()).toBe(false);
      expect(facade.selectedErrorMessage()).toBe(ABWAB_LABELS.templateLoadError);
    });

    it('is false with nothing selected', () => {
      const facade = setup((id) => of(templateResponse(id, 'قالب أ')));

      expect(facade.selectedLoading()).toBe(false);

      facade.select(1);
      facade.clearSelection();
      expect(facade.selectedLoading()).toBe(false);
    });
  });

  it('a failed refresh of the same template keeps it on screen', () => {
    let calls = 0;
    const facade = setup((id) => {
      calls += 1;
      return calls === 1 ? of(templateResponse(id, 'قالب أ')) : throwError(() => new Error('offline'));
    });
    facade.select(1);
    facade.refreshSelected().subscribe();

    expect(facade.selectedErrorMessage()).toBe(ABWAB_LABELS.templateLoadError);
    expect(facade.selectedTemplate()?.id).toBe(1);
  });
});
