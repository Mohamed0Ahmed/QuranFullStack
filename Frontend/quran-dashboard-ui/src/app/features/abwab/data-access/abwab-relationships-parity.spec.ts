import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { environment } from '../../../../environments/environment';
import { AbwabConflictCode, AbwabConflictError } from './abwab-conflict';
import { AbwabRelationshipsHttpAdapter } from './abwab-relationships-http.adapter';
import { AbwabRelationshipsMock } from './abwab-relationships.mock';
import {
  AbwabRelationshipsPort,
  RELATIONSHIP_TYPE_BROADER_NARROWER,
  RELATIONSHIP_TYPE_OPPOSITE,
  RELATIONSHIP_TYPE_SIMILAR,
} from './abwab-relationships.port';

const baseUrl = `${environment.apiBaseUrl}/api/abwab/relationships`;

const categoryA = 'category-a';
const categoryB = 'category-b';
const categoryC = 'category-c';

function successBody<T>(data: T) {
  return { isSuccess: true, message: 'تم', data };
}

function conflictBody(code: string) {
  return { isSuccess: false, message: 'تعارض', errors: [code] };
}

function newMock(overrides: Partial<ConstructorParameters<typeof AbwabRelationshipsMock>[0]> = {}) {
  return new AbwabRelationshipsMock({ categoryIds: [categoryA, categoryB, categoryC], ...overrides });
}

async function seedMutual(mock: AbwabRelationshipsMock): Promise<string> {
  const added = await mock.addRelationship({
    relationshipType: RELATIONSHIP_TYPE_SIMILAR,
    firstCategoryId: categoryA,
    secondCategoryId: categoryB,
    expectedTimelineGeneration: 1,
  });
  return added.relationshipId;
}

// Every scenario is driven through BOTH ports in ONE test, so the two sides can never drift into
// separately-maintained expectations: the mock must RAISE the code the server is stubbed to return.
interface ConflictScenario {
  readonly code: AbwabConflictCode;
  readonly mock: (port: AbwabRelationshipsMock) => Promise<unknown>;
  readonly http: (port: AbwabRelationshipsPort) => Promise<unknown>;
  readonly httpPath: string;
}

const conflictScenarios: readonly ConflictScenario[] = [
  {
    code: 'abwab.relationship_duplicate',
    mock: async (port) => {
      await seedMutual(port);
      return port.addRelationship({
        relationshipType: RELATIONSHIP_TYPE_SIMILAR,
        firstCategoryId: categoryB,
        secondCategoryId: categoryA,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) =>
      port.addRelationship({
        relationshipType: RELATIONSHIP_TYPE_SIMILAR,
        firstCategoryId: categoryB,
        secondCategoryId: categoryA,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '',
  },
  {
    code: 'abwab.relationship_cycle',
    mock: async (port) => {
      await port.addRelationship({
        relationshipType: RELATIONSHIP_TYPE_BROADER_NARROWER,
        firstCategoryId: categoryA,
        secondCategoryId: categoryB,
        expectedTimelineGeneration: 1,
      });
      await port.addRelationship({
        relationshipType: RELATIONSHIP_TYPE_BROADER_NARROWER,
        firstCategoryId: categoryB,
        secondCategoryId: categoryC,
        expectedTimelineGeneration: 1,
      });
      return port.addRelationship({
        relationshipType: RELATIONSHIP_TYPE_BROADER_NARROWER,
        firstCategoryId: categoryC,
        secondCategoryId: categoryA,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) =>
      port.addRelationship({
        relationshipType: RELATIONSHIP_TYPE_BROADER_NARROWER,
        firstCategoryId: categoryC,
        secondCategoryId: categoryA,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '',
  },
  {
    code: 'abwab.manual_protection',
    mock: (port) =>
      port.addRelationship({
        relationshipType: RELATIONSHIP_TYPE_OPPOSITE,
        firstCategoryId: categoryA,
        secondCategoryId: categoryB,
        expectedTimelineGeneration: 1,
      }),
    http: (port) =>
      port.addRelationship({
        relationshipType: RELATIONSHIP_TYPE_OPPOSITE,
        firstCategoryId: categoryA,
        secondCategoryId: categoryB,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '',
  },
  {
    code: 'abwab.category_unavailable',
    mock: (port) => {
      port.setCategoryDeleted(categoryB, true);
      return port.addRelationship({
        relationshipType: RELATIONSHIP_TYPE_SIMILAR,
        firstCategoryId: categoryA,
        secondCategoryId: categoryB,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) =>
      port.addRelationship({
        relationshipType: RELATIONSHIP_TYPE_SIMILAR,
        firstCategoryId: categoryA,
        secondCategoryId: categoryB,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '',
  },
  {
    code: 'abwab.row_stale',
    mock: async (port) => {
      const relationshipId = await seedMutual(port);
      return port.editRelationship(relationshipId, {
        relationshipType: RELATIONSHIP_TYPE_OPPOSITE,
        firstCategoryId: categoryA,
        secondCategoryId: categoryB,
        expectedVersion: 999,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) =>
      port.editRelationship('relationship-1', {
        relationshipType: RELATIONSHIP_TYPE_OPPOSITE,
        firstCategoryId: categoryA,
        secondCategoryId: categoryB,
        expectedVersion: 999,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '/relationship-1',
  },
  {
    code: 'abwab.timeline_generation_stale',
    mock: async (port) => {
      const relationshipId = await seedMutual(port);
      return port.deleteRelationship(relationshipId, { expectedVersion: 1, expectedTimelineGeneration: 99 });
    },
    http: (port) => port.deleteRelationship('relationship-1', { expectedVersion: 1, expectedTimelineGeneration: 99 }),
    httpPath: '/relationship-1',
  },
  {
    code: 'abwab.relationship_duplicate',
    mock: async (port) => {
      const relationshipId = await seedMutual(port);
      const listed = await port.getRelationships(categoryA);
      await port.deleteRelationship(relationshipId, {
        expectedVersion: listed.relationships[0].version,
        expectedTimelineGeneration: 1,
      });
      await seedMutual(port);
      const deleted = await port.getRelationships(categoryA, true);
      const soft = deleted.relationships.find((row) => row.categoryRelationshipId === relationshipId)!;
      return port.restoreRelationship(relationshipId, {
        expectedVersion: soft.version,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) => port.restoreRelationship('relationship-1', { expectedVersion: 2, expectedTimelineGeneration: 1 }),
    httpPath: '/relationship-1/restore',
  },
];

describe('AbwabRelationshipsPort mock <-> HTTP parity', () => {
  let httpTesting: HttpTestingController;
  let http: AbwabRelationshipsPort;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), AbwabRelationshipsHttpAdapter],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    http = TestBed.inject(AbwabRelationshipsHttpAdapter);
  });

  afterEach(() => httpTesting.verify());

  for (const scenario of conflictScenarios) {
    it(`mock and HTTP both raise ${scenario.code} for the same scenario (${scenario.httpPath || 'add'})`, async () => {
      const mock = newMock(
        scenario.code === 'abwab.manual_protection' ? { relationshipProtectedCategoryIds: [categoryB] } : {},
      );

      await expect(scenario.mock(mock)).rejects.toMatchObject({ code: scenario.code });

      const pending = scenario.http(http).catch((error: unknown) => error);
      httpTesting
        .expectOne((call) => call.url.startsWith(`${baseUrl}${scenario.httpPath}`))
        .flush(conflictBody(scenario.code), { status: 409, statusText: 'Conflict' });

      const httpError = await pending;
      expect(httpError).toBeInstanceOf(AbwabConflictError);
      expect((httpError as AbwabConflictError).code).toBe(scenario.code);
    });
  }

  it('a read carries the same key set from both ports, with the server TimelineGeneration and the per-row Version', async () => {
    const mock = newMock();
    await seedMutual(mock);
    const mockList = await mock.getRelationships(categoryA);

    const pending = http.getRelationships(categoryA);
    httpTesting.expectOne(`${baseUrl}/${categoryA}?includeDeleted=false`).flush(
      successBody({
        schemaVersion: 1,
        categoryId: categoryA,
        expectedTimelineGeneration: { generation: 11 },
        serverTimeUtc: new Date().toISOString(),
        relationships: [
          {
            categoryRelationshipId: 'relationship-1',
            relationshipType: RELATIONSHIP_TYPE_SIMILAR,
            isDirectional: false,
            from: { categoryId: categoryA, name: 'باب أ', path: [], isDeleted: false },
            to: { categoryId: categoryB, name: 'باب ب', path: [], isDeleted: false },
            isDeleted: false,
            version: 21,
          },
        ],
      }),
    );
    const httpList = await pending;

    expect(Object.keys(httpList).sort()).toEqual(Object.keys(mockList).sort());
    expect(Object.keys(httpList.relationships[0]).sort()).toEqual(Object.keys(mockList.relationships[0]).sort());
    expect(Object.keys(httpList.relationships[0].from).sort()).toEqual(Object.keys(mockList.relationships[0].from).sort());
    expect(typeof httpList.expectedTimelineGeneration.generation).toBe(typeof mockList.expectedTimelineGeneration.generation);
    expect(typeof httpList.relationships[0].version).toBe(typeof mockList.relationships[0].version);
  });

  it('a mutation result never synthesizes a TimelineGeneration on either port', async () => {
    const mock = newMock();
    const mockResult = await mock.addRelationship({
      relationshipType: RELATIONSHIP_TYPE_SIMILAR,
      firstCategoryId: categoryA,
      secondCategoryId: categoryB,
      expectedTimelineGeneration: 1,
    });

    const pending = http.addRelationship({
      relationshipType: RELATIONSHIP_TYPE_SIMILAR,
      firstCategoryId: categoryA,
      secondCategoryId: categoryB,
      expectedTimelineGeneration: 1,
    });
    httpTesting.expectOne(baseUrl).flush(successBody({ relationshipId: 'relationship-1' }));

    expect(Object.keys(await pending)).toEqual(Object.keys(mockResult));
    expect(Object.keys(mockResult)).toEqual(['relationshipId']);
  });

  it('a successful edit resolves to void on both ports and drives the same PUT route', async () => {
    const mock = newMock();
    const relationshipId = await seedMutual(mock);
    const listed = await mock.getRelationships(categoryA);
    await expect(
      mock.editRelationship(relationshipId, {
        relationshipType: RELATIONSHIP_TYPE_OPPOSITE,
        firstCategoryId: categoryA,
        secondCategoryId: categoryB,
        expectedVersion: listed.relationships[0].version,
        expectedTimelineGeneration: 1,
      }),
    ).resolves.toBeUndefined();
    expect((await mock.getRelationships(categoryA)).relationships[0].relationshipType).toBe(RELATIONSHIP_TYPE_OPPOSITE);

    const pendingEdit = http.editRelationship('relationship-1', {
      relationshipType: RELATIONSHIP_TYPE_OPPOSITE,
      firstCategoryId: categoryA,
      secondCategoryId: categoryB,
      expectedVersion: 1,
      expectedTimelineGeneration: 1,
    });
    const request = httpTesting.expectOne(`${baseUrl}/relationship-1`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body.relationshipType).toBe(RELATIONSHIP_TYPE_OPPOSITE);
    request.flush(successBody(null));
    await expect(pendingEdit).resolves.toBeUndefined();
  });

  it('the mock mirrors dormancy: a relationship whose endpoint is deleted leaves the actionable projection and returns untouched', async () => {
    const mock = newMock();
    const relationshipId = await seedMutual(mock);

    mock.setCategoryDeleted(categoryB, true);
    expect((await mock.getRelationships(categoryA)).relationships).toHaveLength(0);

    mock.setCategoryDeleted(categoryB, false);
    const restored = (await mock.getRelationships(categoryA)).relationships;
    expect(restored).toHaveLength(1);
    expect(restored[0].categoryRelationshipId).toBe(relationshipId);
  });

  it('delete then restore drive the same routes on both ports', async () => {
    const mock = newMock();
    const relationshipId = await seedMutual(mock);
    const listed = await mock.getRelationships(categoryA);
    await mock.deleteRelationship(relationshipId, {
      expectedVersion: listed.relationships[0].version,
      expectedTimelineGeneration: listed.expectedTimelineGeneration.generation,
    });

    const deletedList = await mock.getRelationships(categoryA, true);
    expect(deletedList.relationships[0].isDeleted).toBe(true);

    await mock.restoreRelationship(relationshipId, {
      expectedVersion: deletedList.relationships[0].version,
      expectedTimelineGeneration: deletedList.expectedTimelineGeneration.generation,
    });
    expect((await mock.getRelationships(categoryA)).relationships).toHaveLength(1);

    const httpDelete = http.deleteRelationship(relationshipId, { expectedVersion: 1, expectedTimelineGeneration: 1 });
    const deleteRequest = httpTesting.expectOne(`${baseUrl}/${relationshipId}`);
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(successBody(null));
    await expect(httpDelete).resolves.toBeUndefined();

    const httpRestore = http.restoreRelationship(relationshipId, { expectedVersion: 1, expectedTimelineGeneration: 1 });
    const restoreRequest = httpTesting.expectOne(`${baseUrl}/${relationshipId}/restore`);
    expect(restoreRequest.request.method).toBe('POST');
    restoreRequest.flush(successBody(null));
    await expect(httpRestore).resolves.toBeUndefined();
  });
});
