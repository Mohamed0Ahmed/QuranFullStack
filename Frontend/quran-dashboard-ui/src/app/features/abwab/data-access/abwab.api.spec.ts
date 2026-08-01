import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { HttpTestingController } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ok, setupApiTestBed, teardownApiTestBed } from '../../words/data-access/testing/api-test-bed';
import { AbwabApi } from './abwab.api';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import { AbwabSectionDto } from '../../../core/api/generated/models/abwab-section-dto';
import { AbwabDoorDto } from '../../../core/api/generated/models/abwab-door-dto';
import { AbwabRestoredDoorDto } from '../../../core/api/generated/models/abwab-restored-door-dto';

const BASE = `${environment.apiBaseUrl}/api/abwab`;

const SAMPLE_DOOR: AbwabDoorDto = {
  id: 1,
  name: 'العلم بالله',
  description: null,
  representativeAyahText: null,
  aliases: [],
  parentId: null,
  sectionId: 1,
  orderValue: 1,
  globalOrderValue: 1,
  version: 1,
};

const SAMPLE_SECTION: AbwabSectionDto = { id: 1, name: 'اللغة العربية', orderValue: 1, version: 1 };

describe('AbwabApi', () => {
  let api: AbwabApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    ({ api, httpMock } = setupApiTestBed(AbwabApi));
  });

  afterEach(() => {
    teardownApiTestBed(httpMock);
  });

  it('getTree sends GET /api/abwab/tree', async () => {
    const promise = firstValueFrom(api.getTree());
    const req = httpMock.expectOne(`${BASE}/tree`);
    expect(req.request.method).toBe('GET');

    const response = ok<AbwabTreeDto>({ doors: [], sections: [], version: null });
    req.flush(response);
    await expect(promise).resolves.toEqual(response);
  });

  it('createSection sends POST /api/abwab/sections with the name', async () => {
    const promise = firstValueFrom(api.createSection({ name: 'اللغة العربية' }));
    const req = httpMock.expectOne(`${BASE}/sections`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'اللغة العربية' });

    const response = ok<AbwabSectionDto>(SAMPLE_SECTION);
    req.flush(response);
    await expect(promise).resolves.toEqual(response);
  });

  it('renameSection sends PUT /api/abwab/sections/{id} with name and version', async () => {
    const promise = firstValueFrom(api.renameSection(1, { name: 'علوم اللغة', version: 3 }));
    const req = httpMock.expectOne(`${BASE}/sections/1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ name: 'علوم اللغة', version: 3 });

    req.flush(ok<AbwabSectionDto>(SAMPLE_SECTION));
    await promise;
  });

  it('deleteSection sends DELETE /api/abwab/sections/{id} with no request body', async () => {
    const promise = firstValueFrom(api.deleteSection(1));
    const req = httpMock.expectOne(`${BASE}/sections/1`);
    expect(req.request.method).toBe('DELETE');
    expect(req.request.body).toBeNull();

    req.flush(ok<unknown>(null));
    await promise;
  });

  it('reorderSection sends POST /api/abwab/sections/{id}/order with position and version', async () => {
    const promise = firstValueFrom(api.reorderSection(1, { position: 3, version: 2 }));
    const req = httpMock.expectOne(`${BASE}/sections/1/order`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ position: 3, version: 2 });

    req.flush(ok<AbwabSectionDto>(SAMPLE_SECTION));
    await promise;
  });

  it('createDoor at root sends the section id in the body', async () => {
    const promise = firstValueFrom(
      api.createDoor({
        name: 'الأسماء والصفات',
        description: null,
        representativeAyahText: null,
        aliases: null,
        parentId: null,
        sectionId: 4,
      }),
    );
    const req = httpMock.expectOne(`${BASE}/doors`);
    expect(req.request.body.sectionId).toBe(4);

    req.flush(ok<AbwabDoorDto>(SAMPLE_DOOR));
    await promise;
  });

  // M33: the wire body must not carry the key at all when parentId is set — a wrong
  // implementation that merely sets `sectionId: undefined` would still pass a looser
  // `body.sectionId === undefined` assertion, so this checks key presence directly.
  it('createDoor under a parent OMITS sectionId from the body entirely (M33)', async () => {
    const promise = firstValueFrom(
      api.createDoor({
        name: 'أسماء الله الحسنى',
        description: null,
        representativeAyahText: null,
        aliases: null,
        parentId: 7,
        sectionId: null,
      }),
    );
    const req = httpMock.expectOne(`${BASE}/doors`);
    expect(req.request.body.parentId).toBe(7);
    expect('sectionId' in req.request.body).toBe(false);

    req.flush(ok<AbwabDoorDto>(SAMPLE_DOOR));
    await promise;
  });

  it('updateDoor sends PUT /api/abwab/doors/{id}', async () => {
    const promise = firstValueFrom(
      api.updateDoor(1, {
        name: 'الألوهية',
        description: 'وصف',
        representativeAyahText: null,
        aliases: ['التوحيد'],
        version: 2,
      }),
    );
    const req = httpMock.expectOne(`${BASE}/doors/1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.aliases).toEqual(['التوحيد']);

    req.flush(ok<AbwabDoorDto>(SAMPLE_DOOR));
    await promise;
  });

  it('moveDoor sends POST /api/abwab/doors/{id}/move', async () => {
    const promise = firstValueFrom(
      api.moveDoor(1, { targetParentId: 9, targetSectionId: null, version: 2 }),
    );
    const req = httpMock.expectOne(`${BASE}/doors/1/move`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ targetParentId: 9, targetSectionId: null, version: 2 });

    req.flush(ok<AbwabDoorDto>(SAMPLE_DOOR));
    await promise;
  });

  it('reorderDoor sends POST /api/abwab/doors/{id}/order', async () => {
    const promise = firstValueFrom(api.reorderDoor(1, { position: 3, scope: 1, version: 2 }));
    const req = httpMock.expectOne(`${BASE}/doors/1/order`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ position: 3, scope: 1, version: 2 });

    req.flush(ok<AbwabDoorDto>(SAMPLE_DOOR));
    await promise;
  });

  it('bulkMoveDoors sends POST /api/abwab/doors/bulk-move', async () => {
    const promise = firstValueFrom(
      api.bulkMoveDoors({
        doors: [{ doorId: 1, version: 2 }],
        targetParentId: null,
        targetSectionId: 4,
      }),
    );
    const req = httpMock.expectOne(`${BASE}/doors/bulk-move`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.doors).toEqual([{ doorId: 1, version: 2 }]);

    req.flush(ok<AbwabDoorDto[]>([SAMPLE_DOOR]));
    await promise;
  });

  it('bulkArchiveDoors sends POST /api/abwab/doors/bulk-archive', async () => {
    const promise = firstValueFrom(api.bulkArchiveDoors({ doors: [{ doorId: 1, version: 2 }] }));
    const req = httpMock.expectOne(`${BASE}/doors/bulk-archive`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ doors: [{ doorId: 1, version: 2 }] });

    req.flush(ok<number[]>([1]));
    await promise;
  });

  it('archiveDoor sends DELETE /api/abwab/doors/{id} WITH the version body', async () => {
    const promise = firstValueFrom(api.archiveDoor(1, { version: 2 }));
    const req = httpMock.expectOne(`${BASE}/doors/1`);
    expect(req.request.method).toBe('DELETE');
    expect(req.request.body).toEqual({ version: 2 });

    req.flush(ok<unknown>(null));
    await promise;
  });

  // Wire-contract pins, NOT regression tests: these assert what the two 204 routes put on
  // the wire (an empty body, which HttpClient parses as `null`) so no future spec flushes a
  // well-formed envelope the backend never sends. They pass regardless of how the write
  // controller handles that null — the regression for that lives in
  // abwab-write.controller.spec.ts's "204 No Content" block.
  it('wire contract: archiveDoor emits null when the backend answers 204 No Content', async () => {
    const promise = firstValueFrom(api.archiveDoor(1, { version: 2 }));
    const req = httpMock.expectOne(`${BASE}/doors/1`);

    req.flush(null, { status: 204, statusText: 'No Content' });
    expect(await promise).toBeNull();
  });

  it('wire contract: deleteSection emits null when the backend answers 204 No Content', async () => {
    const promise = firstValueFrom(api.deleteSection(1));
    const req = httpMock.expectOne(`${BASE}/sections/1`);

    req.flush(null, { status: 204, statusText: 'No Content' });
    expect(await promise).toBeNull();
  });

  it('restoreDoor sends POST /api/abwab/doors/{id}/restore', async () => {
    const promise = firstValueFrom(api.restoreDoor(1, { version: 2 }));
    const req = httpMock.expectOne(`${BASE}/doors/1/restore`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ version: 2 });

    const response = ok<AbwabRestoredDoorDto>({ door: SAMPLE_DOOR, detachedFromArchivedSection: false });
    req.flush(response);
    await expect(promise).resolves.toEqual(response);
  });
});
