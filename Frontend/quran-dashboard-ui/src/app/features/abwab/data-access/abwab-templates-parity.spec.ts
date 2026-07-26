import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { environment } from '../../../../environments/environment';
import { AbwabConflictCode, AbwabConflictError } from './abwab-conflict';
import { AbwabTemplatesHttpAdapter } from './abwab-templates-http.adapter';
import { AbwabTemplatesMock } from './abwab-templates.mock';
import { AbwabTemplatesPort } from './abwab-templates.port';

const baseUrl = `${environment.apiBaseUrl}/api/abwab/templates`;

const targetCategoryId = 'category-target';
const protectedCategoryId = 'category-protected';

function successBody<T>(data: T) {
  return { isSuccess: true, message: 'تم', data };
}

function conflictBody(code: string) {
  return { isSuccess: false, message: 'تعارض', errors: [code] };
}

function newMock(overrides: Partial<ConstructorParameters<typeof AbwabTemplatesMock>[0]> = {}) {
  return new AbwabTemplatesMock({ targetCategoryIds: [targetCategoryId, protectedCategoryId], ...overrides });
}

async function seedTemplateWithRoot(mock: AbwabTemplatesMock): Promise<{ doorTemplateId: string; templateNodeId: string }> {
  const { doorTemplateId } = await mock.addTemplate({ name: 'قالب', description: null, expectedTimelineGeneration: 1 });
  const { templateNodeId } = await mock.addNode(doorTemplateId, {
    parentTemplateNodeId: null,
    name: 'جذر',
    representativeQuranExcerpt: null,
    description: null,
    expectedTemplateRevision: 0,
    expectedTimelineGeneration: 1,
  });
  return { doorTemplateId, templateNodeId };
}

// Every scenario is driven through BOTH ports in ONE test, so the two sides can never drift into
// separately-maintained expectations: the mock must RAISE the code the server is stubbed to return.
interface ConflictScenario {
  readonly code: AbwabConflictCode;
  readonly note: string;
  readonly mock: (port: AbwabTemplatesMock) => Promise<unknown>;
  readonly http: (port: AbwabTemplatesPort) => Promise<unknown>;
  readonly httpPath: string;
}

const conflictScenarios: readonly ConflictScenario[] = [
  {
    code: 'abwab.template_cycle',
    note: 'reparenting a node under itself',
    mock: async (port) => {
      const { templateNodeId } = await seedTemplateWithRoot(port);
      return port.reparentNode(templateNodeId, {
        newParentTemplateNodeId: templateNodeId,
        expectedVersion: 1,
        expectedTemplateRevision: 1,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) =>
      port.reparentNode('template-entity-2', {
        newParentTemplateNodeId: 'template-entity-2',
        expectedVersion: 1,
        expectedTemplateRevision: 1,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '/nodes/template-entity-2/reparent',
  },
  {
    code: 'abwab.template_revision_stale',
    note: 'a structural write with a stale TemplateRevision',
    mock: async (port) => {
      const { doorTemplateId } = await seedTemplateWithRoot(port);
      return port.addNode(doorTemplateId, {
        parentTemplateNodeId: null,
        name: 'جذر آخر',
        representativeQuranExcerpt: null,
        description: null,
        expectedTemplateRevision: 99,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) =>
      port.addNode('template-entity-1', {
        parentTemplateNodeId: null,
        name: 'جذر آخر',
        representativeQuranExcerpt: null,
        description: null,
        expectedTemplateRevision: 99,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '/template-entity-1/nodes',
  },
  {
    code: 'abwab.row_stale',
    note: 'editing a node with a stale Version',
    mock: async (port) => {
      const { templateNodeId } = await seedTemplateWithRoot(port);
      return port.editNode(templateNodeId, {
        name: 'اسم آخر',
        representativeQuranExcerpt: null,
        description: null,
        expectedVersion: 999,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) =>
      port.editNode('template-entity-2', {
        name: 'اسم آخر',
        representativeQuranExcerpt: null,
        description: null,
        expectedVersion: 999,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '/nodes/template-entity-2',
  },
  {
    code: 'abwab.row_stale',
    note: 'adding a node under a soft-deleted parent',
    mock: async (port) => {
      const { doorTemplateId, templateNodeId } = await seedTemplateWithRoot(port);
      await port.removeNode(templateNodeId, {
        expectedVersion: 1,
        expectedTemplateRevision: 1,
        expectedTimelineGeneration: 1,
      });
      return port.addNode(doorTemplateId, {
        parentTemplateNodeId: templateNodeId,
        name: 'ابن يتيم',
        representativeQuranExcerpt: null,
        description: null,
        expectedTemplateRevision: 2,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) =>
      port.addNode('template-entity-1', {
        parentTemplateNodeId: 'template-entity-2',
        name: 'ابن يتيم',
        representativeQuranExcerpt: null,
        description: null,
        expectedTemplateRevision: 2,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '/template-entity-1/nodes',
  },
  {
    code: 'abwab.row_stale',
    note: "adding a node under a parent belonging to a DIFFERENT template",
    mock: async (port) => {
      const { templateNodeId } = await seedTemplateWithRoot(port);
      const { doorTemplateId: otherTemplateId } = await port.addTemplate({
        name: 'قالب آخر',
        description: null,
        expectedTimelineGeneration: 1,
      });
      return port.addNode(otherTemplateId, {
        parentTemplateNodeId: templateNodeId,
        name: 'ابن غريب',
        representativeQuranExcerpt: null,
        description: null,
        expectedTemplateRevision: 0,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) =>
      port.addNode('template-entity-3', {
        parentTemplateNodeId: 'template-entity-2',
        name: 'ابن غريب',
        representativeQuranExcerpt: null,
        description: null,
        expectedTemplateRevision: 0,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '/template-entity-3/nodes',
  },
  {
    code: 'abwab.timeline_generation_stale',
    note: 'any write carrying a stale generation',
    mock: (port) => port.addTemplate({ name: 'قالب', description: null, expectedTimelineGeneration: 99 }),
    http: (port) => port.addTemplate({ name: 'قالب', description: null, expectedTimelineGeneration: 99 }),
    httpPath: '',
  },
  {
    code: 'abwab.manual_protection',
    note: 'applying to an InternalStructure-protected target',
    mock: async (port) => {
      const { doorTemplateId } = await seedTemplateWithRoot(port);
      return port.applyTemplate(doorTemplateId, {
        targetCategoryId: protectedCategoryId,
        expectedTemplateRevision: 1,
        expectedTreeRevision: 1,
        expectedTargetVersion: 1,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) =>
      port.applyTemplate('template-entity-1', {
        targetCategoryId: protectedCategoryId,
        expectedTemplateRevision: 1,
        expectedTreeRevision: 1,
        expectedTargetVersion: 1,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '/template-entity-1/apply',
  },
  {
    code: 'abwab.category_unavailable',
    note: 'applying to a missing target',
    mock: async (port) => {
      const { doorTemplateId } = await seedTemplateWithRoot(port);
      return port.applyTemplate(doorTemplateId, {
        targetCategoryId: 'category-missing',
        expectedTemplateRevision: 1,
        expectedTreeRevision: 1,
        expectedTargetVersion: 1,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) =>
      port.applyTemplate('template-entity-1', {
        targetCategoryId: 'category-missing',
        expectedTemplateRevision: 1,
        expectedTreeRevision: 1,
        expectedTargetVersion: 1,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '/template-entity-1/apply',
  },
  {
    code: 'abwab.tree_revision_stale',
    note: 'applying with a stale TreeRevision',
    mock: async (port) => {
      const { doorTemplateId } = await seedTemplateWithRoot(port);
      return port.applyTemplate(doorTemplateId, {
        targetCategoryId,
        expectedTemplateRevision: 1,
        expectedTreeRevision: 99,
        expectedTargetVersion: 1,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) =>
      port.applyTemplate('template-entity-1', {
        targetCategoryId,
        expectedTemplateRevision: 1,
        expectedTreeRevision: 99,
        expectedTargetVersion: 1,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '/template-entity-1/apply',
  },
  {
    code: 'abwab.category_name_conflict',
    note: 'a template root colliding with an existing child of the target',
    mock: async (port) => {
      const { doorTemplateId } = await seedTemplateWithRoot(port);
      return port.applyTemplate(doorTemplateId, {
        targetCategoryId,
        expectedTemplateRevision: 1,
        expectedTreeRevision: 1,
        expectedTargetVersion: 1,
        expectedTimelineGeneration: 1,
      });
    },
    http: (port) =>
      port.applyTemplate('template-entity-1', {
        targetCategoryId,
        expectedTemplateRevision: 1,
        expectedTreeRevision: 1,
        expectedTargetVersion: 1,
        expectedTimelineGeneration: 1,
      }),
    httpPath: '/template-entity-1/apply',
  },
];

describe('AbwabTemplatesPort mock <-> HTTP parity', () => {
  let httpTesting: HttpTestingController;
  let http: AbwabTemplatesPort;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), AbwabTemplatesHttpAdapter],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    http = TestBed.inject(AbwabTemplatesHttpAdapter);
  });

  afterEach(() => httpTesting.verify());

  for (const scenario of conflictScenarios) {
    it(`mock and HTTP both raise ${scenario.code} for the same scenario (${scenario.note})`, async () => {
      const mock = newMock(
        scenario.code === 'abwab.manual_protection'
          ? { internalStructureProtectedCategoryIds: [protectedCategoryId] }
          : scenario.code === 'abwab.category_name_conflict'
            ? { takenChildNamesByCategoryId: { [targetCategoryId]: ['جذر'] } }
            : {},
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

  it('a template list read carries the same key set from both ports, down to the summary row', async () => {
    const mock = newMock();
    await seedTemplateWithRoot(mock);
    const mockList = await mock.getTemplates();

    const pending = http.getTemplates();
    httpTesting.expectOne(`${baseUrl}?includeDeleted=false`).flush(
      successBody({
        schemaVersion: 1,
        templates: [
          {
            doorTemplateId: 'template-entity-1',
            name: 'قالب',
            description: null,
            templateRevision: 1,
            isDeleted: false,
            version: 3,
            nodeCount: 1,
          },
        ],
        expectedTimelineGeneration: { generation: 12 },
        serverTimeUtc: new Date().toISOString(),
      }),
    );
    const httpList = await pending;

    expect(Object.keys(httpList).sort()).toEqual(Object.keys(mockList).sort());
    expect(Object.keys(httpList.templates[0]).sort()).toEqual(Object.keys(mockList.templates[0]).sort());
    expect(typeof httpList.expectedTimelineGeneration.generation).toBe(typeof mockList.expectedTimelineGeneration.generation);
  });

  // The mock does not simulate the append-only audit engine, so `entries` is empty on that side; the
  // ENVELOPE is still driven through both ports so the two cannot drift on the read's shape.
  it('a template history read carries the same envelope key set from both ports', async () => {
    const mock = newMock();
    const { doorTemplateId } = await seedTemplateWithRoot(mock);
    const mockHistory = await mock.getHistory(doorTemplateId);

    const pending = http.getHistory('template-entity-1');
    httpTesting.expectOne(`${baseUrl}/template-entity-1/history`).flush(
      successBody({
        schemaVersion: 1,
        doorTemplateId: 'template-entity-1',
        entries: [
          { auditSequence: 4, actorSubject: 'مشرف', actedAtUtc: new Date().toISOString(), payload: '{"type":"template.history.created"}' },
        ],
        hasMore: true,
        expectedTimelineGeneration: { generation: 12 },
        serverTimeUtc: new Date().toISOString(),
      }),
    );
    const httpHistory = await pending;

    expect(Object.keys(httpHistory).sort()).toEqual(Object.keys(mockHistory).sort());
    expect(Array.isArray(mockHistory.entries)).toBe(true);
    expect(Object.keys(httpHistory.entries[0]).sort()).toEqual(['actedAtUtc', 'actorSubject', 'auditSequence', 'payload']);
    // The server caps the history projection, so both ports must carry the truncation flag through —
    // a client that dropped it would render a capped page as the template's complete record.
    expect(httpHistory.hasMore).toBe(true);
    expect(mockHistory.hasMore).toBe(false);
  });

  it('a template detail read carries the same key set from both ports, with the server TimelineGeneration', async () => {
    const mock = newMock();
    const { doorTemplateId } = await seedTemplateWithRoot(mock);
    const mockDetail = await mock.getTemplate(doorTemplateId);

    const pending = http.getTemplate('template-entity-1');
    httpTesting.expectOne(`${baseUrl}/template-entity-1?includeDeleted=false`).flush(
      successBody({
        schemaVersion: 1,
        template: {
          doorTemplateId: 'template-entity-1',
          name: 'قالب',
          description: null,
          templateRevision: 1,
          isDeleted: false,
          version: 3,
          nodeCount: 1,
        },
        nodes: [
          {
            templateNodeId: 'template-entity-2',
            parentTemplateNodeId: null,
            name: 'جذر',
            representativeQuranExcerpt: null,
            description: null,
            siblingOrder: 0,
            isDeleted: false,
            version: 4,
            aliases: [],
          },
        ],
        expectedTimelineGeneration: { generation: 12 },
        treeRevision: 7,
        serverTimeUtc: new Date().toISOString(),
      }),
    );
    const httpDetail = await pending;

    expect(Object.keys(httpDetail).sort()).toEqual(Object.keys(mockDetail).sort());
    expect(Object.keys(httpDetail.template).sort()).toEqual(Object.keys(mockDetail.template).sort());
    expect(Object.keys(httpDetail.nodes[0]).sort()).toEqual(Object.keys(mockDetail.nodes[0]).sort());
    expect(typeof httpDetail.expectedTimelineGeneration.generation).toBe(typeof mockDetail.expectedTimelineGeneration.generation);
  });

  it('a mutation result never synthesizes a TimelineGeneration on either port', async () => {
    const mock = newMock();
    const mockResult = await mock.addTemplate({ name: 'قالب', description: null, expectedTimelineGeneration: 1 });

    const pending = http.addTemplate({ name: 'قالب', description: null, expectedTimelineGeneration: 1 });
    httpTesting.expectOne(baseUrl).flush(successBody({ doorTemplateId: 'template-entity-1' }));

    expect(Object.keys(await pending)).toEqual(Object.keys(mockResult));
    expect(Object.keys(mockResult)).toEqual(['doorTemplateId']);
  });

  it('a successful application resolves to void on both ports and drives the same POST route', async () => {
    const mock = newMock();
    const { doorTemplateId } = await seedTemplateWithRoot(mock);
    await expect(
      mock.applyTemplate(doorTemplateId, {
        targetCategoryId,
        expectedTemplateRevision: 1,
        expectedTreeRevision: 1,
        expectedTargetVersion: 1,
        expectedTimelineGeneration: 1,
      }),
    ).resolves.toBeUndefined();

    const pendingApply = http.applyTemplate('template-entity-1', {
      targetCategoryId,
      expectedTemplateRevision: 1,
      expectedTreeRevision: 1,
      expectedTargetVersion: 1,
      expectedTimelineGeneration: 1,
    });
    const request = httpTesting.expectOne(`${baseUrl}/template-entity-1/apply`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body.targetCategoryId).toBe(targetCategoryId);
    request.flush(successBody(null));
    await expect(pendingApply).resolves.toBeUndefined();
  });

  it('alias remove then restore is tracked soft delete on the mock and drives the same routes over HTTP', async () => {
    const mock = newMock();
    const { doorTemplateId, templateNodeId } = await seedTemplateWithRoot(mock);
    const { templateNodeSearchAliasId } = await mock.addNodeAlias(templateNodeId, {
      value: 'مرادف',
      expectedTimelineGeneration: 1,
    });

    await mock.removeNodeAlias(templateNodeSearchAliasId, { expectedVersion: 1, expectedTimelineGeneration: 1 });
    expect((await mock.getTemplate(doorTemplateId, true)).nodes[0].aliases[0].isDeleted).toBe(true);

    await mock.restoreNodeAlias(templateNodeSearchAliasId, { expectedVersion: 2, expectedTimelineGeneration: 1 });
    expect((await mock.getTemplate(doorTemplateId)).nodes[0].aliases).toHaveLength(1);

    const httpRemove = http.removeNodeAlias('alias-1', { expectedVersion: 1, expectedTimelineGeneration: 1 });
    const removeRequest = httpTesting.expectOne(`${baseUrl}/aliases/alias-1`);
    expect(removeRequest.request.method).toBe('DELETE');
    removeRequest.flush(successBody(null));
    await expect(httpRemove).resolves.toBeUndefined();

    const httpRestore = http.restoreNodeAlias('alias-1', { expectedVersion: 2, expectedTimelineGeneration: 1 });
    const restoreRequest = httpTesting.expectOne(`${baseUrl}/aliases/alias-1/restore`);
    expect(restoreRequest.request.method).toBe('POST');
    restoreRequest.flush(successBody(null));
    await expect(httpRestore).resolves.toBeUndefined();
  });

  it('an explicit reorder rewrites every sibling order on the mock and drives the reorder route over HTTP', async () => {
    const mock = newMock();
    const { doorTemplateId } = await seedTemplateWithRoot(mock);
    const second = await mock.addNode(doorTemplateId, {
      parentTemplateNodeId: null,
      name: 'جذر ثانٍ',
      representativeQuranExcerpt: null,
      description: null,
      expectedTemplateRevision: 1,
      expectedTimelineGeneration: 1,
    });
    const firstNodeId = (await mock.getTemplate(doorTemplateId)).nodes[0].templateNodeId;

    await mock.reorderNodes(doorTemplateId, {
      parentTemplateNodeId: null,
      orderedTemplateNodeIds: [second.templateNodeId, firstNodeId],
      expectedTemplateRevision: 2,
      expectedTimelineGeneration: 1,
    });

    expect((await mock.getTemplate(doorTemplateId)).nodes.map((node) => node.templateNodeId)).toEqual([
      second.templateNodeId,
      firstNodeId,
    ]);

    const pending = http.reorderNodes('template-entity-1', {
      parentTemplateNodeId: null,
      orderedTemplateNodeIds: [second.templateNodeId, firstNodeId],
      expectedTemplateRevision: 2,
      expectedTimelineGeneration: 1,
    });
    const request = httpTesting.expectOne(`${baseUrl}/template-entity-1/nodes/reorder`);
    expect(request.request.method).toBe('POST');
    request.flush(successBody(null));
    await expect(pending).resolves.toBeUndefined();
  });
});
