import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpHeaders, HttpResponse } from '@angular/common/http';
import { Observable, Subscriber, map, of, throwError } from 'rxjs';

import { AbwabTemplatesApi } from '../data-access/abwab-templates.api';
import { AbwabTemplateDto } from '../../../core/api/generated/models/abwab-template-dto';
import { AbwabTemplateSummaryDto } from '../../../core/api/generated/models/abwab-template-summary-dto';
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

// The two reads observe the whole response so the facade can store the ETag beside the value. The
// stubs stay envelope-shaped and are wrapped headerless, leaving every assertion below unconditional.
function setup(getTemplate: (id: number) => Observable<ApiResponse<AbwabTemplateDto>>): AbwabTemplatesFacade {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      AbwabTemplatesFacade,
      {
        provide: AbwabTemplatesApi,
        useValue: {
          getTemplates: () => of(new HttpResponse({ body: { isSuccess: true, message: 'تم', data: [] } })),
          getTemplate: (id: number) => getTemplate(id).pipe(map((envelope) => new HttpResponse({ body: envelope }))),
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

// F-36, templates half — same defect as the snapshot facade: `listRequest`/`selectedRequest`
// unsubscribes under `shareReplay(1)` (refCount: false) cancelled nothing, so an older response
// landing late overwrote the newer list/template and the ETag the next conditional read sends.
describe('AbwabTemplatesFacade — F-36: out-of-order responses cannot overwrite newer state', () => {
  function setupRace() {
    getTestBed().resetTestingModule();
    const pendingList: Subscriber<HttpResponse<ApiResponse<readonly AbwabTemplateSummaryDto[]>>>[] = [];
    const pendingSelected: Subscriber<HttpResponse<ApiResponse<AbwabTemplateDto>>>[] = [];
    const sentListEtags: (string | null)[] = [];
    const sentSelectedEtags: (string | null)[] = [];
    TestBed.configureTestingModule({
      providers: [
        AbwabTemplatesFacade,
        {
          provide: AbwabTemplatesApi,
          useValue: {
            getTemplates: (etag: string | null) =>
              new Observable<HttpResponse<ApiResponse<readonly AbwabTemplateSummaryDto[]>>>((subscriber) => {
                sentListEtags.push(etag);
                pendingList.push(subscriber);
              }),
            getTemplate: (_id: number, etag: string | null) =>
              new Observable<HttpResponse<ApiResponse<AbwabTemplateDto>>>((subscriber) => {
                sentSelectedEtags.push(etag);
                pendingSelected.push(subscriber);
              }),
          },
        },
      ],
    });
    const resolveList = (index: number, name: string, etag: string) => {
      pendingList[index].next(
        new HttpResponse({
          body: { isSuccess: true, message: 'تم', data: [{ id: 1, name, nodeCount: 1 }] },
          headers: new HttpHeaders({ ETag: etag }),
        }),
      );
      pendingList[index].complete();
    };
    const resolveSelected = (index: number, name: string, etag: string) => {
      pendingSelected[index].next(
        new HttpResponse({
          body: templateResponse(1, name),
          headers: new HttpHeaders({ ETag: etag }),
        }),
      );
      pendingSelected[index].complete();
    };
    return {
      facade: TestBed.inject(AbwabTemplatesFacade),
      resolveList,
      resolveSelected,
      sentListEtags,
      sentSelectedEtags,
    };
  }

  it('an older list response landing late does not overwrite the newer list or the validator it sends next', () => {
    const { facade, resolveList, sentListEtags } = setupRace();

    facade.refreshList().subscribe();
    facade.refreshList().subscribe();

    resolveList(1, 'الأحدث', 'W/"list-B"');
    resolveList(0, 'الأقدم', 'W/"list-A"');

    expect(facade.templates().map((template) => template.name)).toEqual(['الأحدث']);
    expect(facade.isLoading()).toBe(false);
    expect(facade.errorMessage()).toBeNull();

    facade.loadList();
    expect(sentListEtags[2]).toBe('W/"list-B"');
  });

  it('an older selected-template response landing late does not overwrite the newer one or its validator', () => {
    const { facade, resolveSelected, sentSelectedEtags } = setupRace();

    facade.select(1);
    facade.refreshSelected().subscribe();
    facade.refreshSelected().subscribe();

    resolveSelected(2, 'قالب أحدث', 'W/"sel-B"');
    resolveSelected(1, 'قالب أقدم', 'W/"sel-A"');

    expect(facade.selectedTemplate()?.name).toBe('قالب أحدث');
    expect(facade.selectedLoading()).toBe(false);
    expect(facade.selectedErrorMessage()).toBeNull();

    facade.refreshSelected();
    expect(sentSelectedEtags[3]).toBe('W/"sel-B"');
  });
});
