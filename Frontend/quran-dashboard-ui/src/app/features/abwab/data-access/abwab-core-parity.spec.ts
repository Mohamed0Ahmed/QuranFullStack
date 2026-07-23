import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { environment } from '../../../../environments/environment';
import { AbwabConflictCode, AbwabConflictError } from './abwab-conflict';
import { AbwabCoreMock } from './abwab-core.mock';
import { AbwabCorePort } from './abwab-core.port';
import { AbwabHttpAdapter } from './abwab-http.adapter';

const baseUrl = `${environment.apiBaseUrl}/api/abwab`;

function successBody<T>(data: T) {
  return { isSuccess: true, message: 'تم', data };
}

function conflictBody(code: string) {
  return { isSuccess: false, message: 'تعارض', errors: [code] };
}

// T063: mock ≡ HTTP parity. Both AbwabCoreMock and AbwabHttpAdapter implement the ONE AbwabCorePort
// contract (abwab-core.port.ts); this suite drives the same scenarios through each and asserts
// identical result shapes and identical abwab.* conflict codes, and that NEITHER fabricates
// TimelineGeneration on a mutation result (only reads carry it, straight from the server/mock state).
describe('AbwabCorePort mock <-> HTTP parity', () => {
  let httpTesting: HttpTestingController;
  let http: AbwabCorePort;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), AbwabHttpAdapter],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    http = TestBed.inject(AbwabHttpAdapter);
  });

  afterEach(() => httpTesting.verify());

  it('mock: getTreeSnapshot carries a numeric TimelineGeneration and treeRevision', async () => {
    const mock = new AbwabCoreMock();
    const snapshot = await mock.getTreeSnapshot();
    expect(typeof snapshot.expectedTimelineGeneration.generation).toBe('number');
    expect(typeof snapshot.treeRevision).toBe('number');
  });

  it('http: getTreeSnapshot carries a numeric TimelineGeneration and treeRevision', async () => {
    const pending = http.getTreeSnapshot();
    httpTesting.expectOne(`${baseUrl}/tree`).flush(
      successBody({
        schemaVersion: 1,
        treeRevision: 4,
        expectedTimelineGeneration: { generation: 7 },
        serverTimeUtc: new Date().toISOString(),
        sections: [],
        categories: [],
        allCategoriesProjection: [],
      }),
    );
    const snapshot = await pending;
    expect(snapshot.expectedTimelineGeneration.generation).toBe(7);
    expect(snapshot.treeRevision).toBe(4);
  });

  it('mock: sections and categories carry a per-row Version, and categories carry their active Aliases each with their own Version', async () => {
    const mock = new AbwabCoreMock();
    const snapshot = await mock.getTreeSnapshot();
    const section = await mock.addSection({ name: 'قسم النسخة', expectedTreeRevision: snapshot.treeRevision, expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation });
    const afterSection = await mock.getTreeSnapshot();
    const category = await mock.addCategory({
      name: 'باب النسخة', description: null, representativeQuranExcerpt: null, parentCategoryId: null, sectionId: section.sectionId,
      expectedTreeRevision: afterSection.treeRevision, expectedTimelineGeneration: afterSection.expectedTimelineGeneration.generation,
    });
    const afterCategory = await mock.getTreeSnapshot();
    await mock.addCategoryAlias(category.categoryId, { value: 'اسم بديل', expectedTimelineGeneration: afterCategory.expectedTimelineGeneration.generation });

    const finalSnapshot = await mock.getTreeSnapshot();
    const sectionDto = finalSnapshot.sections.find((s) => s.sectionId === section.sectionId)!;
    const categoryDto = finalSnapshot.categories.find((c) => c.categoryId === category.categoryId)!;

    expect(typeof sectionDto.version).toBe('number');
    expect(sectionDto.version).not.toBe(0);
    expect(typeof categoryDto.version).toBe('number');
    expect(categoryDto.version).not.toBe(0);
    expect(categoryDto.aliases).toHaveLength(1);
    expect(categoryDto.aliases[0]).toMatchObject({ value: 'اسم بديل' });
    expect(typeof categoryDto.aliases[0].version).toBe('number');
  });

  it('http: sections and categories carry a per-row Version, and categories carry their active Aliases (contract shape parity with the mock)', async () => {
    const pending = http.getTreeSnapshot();
    httpTesting.expectOne(`${baseUrl}/tree`).flush(
      successBody({
        schemaVersion: 1,
        treeRevision: 1,
        expectedTimelineGeneration: { generation: 1 },
        serverTimeUtc: new Date().toISOString(),
        sections: [{ sectionId: 'section-1', name: 'قسم', normalizedName: 'قسم', sortOrder: 0, isPermanentDefault: true, version: 42 }],
        categories: [
          {
            categoryId: 'category-1', name: 'باب', normalizedName: 'باب', description: null, representativeQuranExcerpt: null,
            parentCategoryId: null, sectionId: 'section-1', siblingOrder: null, sectionOrder: 0, globalOrder: 0,
            ancestorIds: [], depth: 0, categoryContentRevision: 1, version: 17,
            aliases: [{ categorySearchAliasId: 'alias-1', value: 'اسم بديل', version: 9 }],
            protection: { hasEffectiveManualProtection: false, isActionBlocked: false, manualProtections: [] },
          },
        ],
        allCategoriesProjection: [],
      }),
    );
    const snapshot = await pending;

    expect(snapshot.sections[0].version).toBe(42);
    expect(snapshot.categories[0].version).toBe(17);
    expect(snapshot.categories[0].aliases).toEqual([{ categorySearchAliasId: 'alias-1', value: 'اسم بديل', version: 9 }]);
  });

  // 3rd blocker fix: a DIRECT manual-protection resolution carries the record's own identity +
  // concurrency token, so the caller sources apply/lift/preset ExpectedVersion from this read.
  it('mock: getProtectionProfile carries manualProtectionId/version for a DIRECT resolution, and null for an inherited one', async () => {
    const mock = new AbwabCoreMock();
    const snapshot = await mock.getTreeSnapshot();
    const root = await mock.addCategory({
      name: 'باب اختبار تسريب النسخة', description: null, representativeQuranExcerpt: null, parentCategoryId: null, sectionId: null,
      expectedTreeRevision: snapshot.treeRevision, expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
    });
    const afterRoot = await mock.getTreeSnapshot();
    const child = await mock.addCategory({
      name: 'باب فرعي لاختبار الوراثة', description: null, representativeQuranExcerpt: null, parentCategoryId: root.categoryId, sectionId: null,
      expectedTreeRevision: afterRoot.treeRevision, expectedTimelineGeneration: afterRoot.expectedTimelineGeneration.generation,
    });
    const afterChild = await mock.getTreeSnapshot();
    await mock.applyManualProtection(root.categoryId, 1, { scope: 1, expectedVersion: null, expectedTimelineGeneration: afterChild.expectedTimelineGeneration.generation });

    const rootProfile = await mock.getProtectionProfile(root.categoryId);
    const directResolution = rootProfile.manualProtections.find((m) => m.protectionType === 1)!;
    expect(directResolution.isDirect).toBe(true);
    expect(directResolution.manualProtectionId).not.toBeNull();
    expect(typeof directResolution.version).toBe('number');

    const childProfile = await mock.getProtectionProfile(child.categoryId);
    const inheritedResolution = childProfile.manualProtections.find((m) => m.protectionType === 1)!;
    expect(inheritedResolution.isDirect).toBe(false);
    expect(inheritedResolution.manualProtectionId).toBeNull();
    expect(inheritedResolution.version).toBeNull();
  });

  it('http: getProtectionProfile surfaces manualProtectionId/version (contract shape parity with the mock)', async () => {
    const pending = http.getProtectionProfile('category-1');
    httpTesting.expectOne(`${baseUrl}/protection/category-1`).flush(
      successBody({
        categoryId: 'category-1',
        expectedTimelineGeneration: { generation: 1 },
        serverTimeUtc: new Date().toISOString(),
        ordinaryProtection: { isActive: false, actorSubject: null, lastEditedAtUtc: null, expiresAtUtc: null },
        manualProtections: [
          {
            protectionType: 1, isProtected: true, isDirect: true, scope: 1, sourceCategoryId: 'category-1',
            serverTimeUtc: new Date().toISOString(), actionClassification: 2, manualProtectionId: 'mp-1', version: 3,
          },
        ],
      }),
    );
    const profile = await pending;

    expect(profile.manualProtections[0].manualProtectionId).toBe('mp-1');
    expect(profile.manualProtections[0].version).toBe(3);
  });

  it('neither mock nor HTTP attaches a generation/revision field to a mutation result (addSection)', async () => {
    const mock = new AbwabCoreMock();
    const mockSnapshot = await mock.getTreeSnapshot();
    const mockResult = await mock.addSection({
      name: 'قسم تجريبي',
      expectedTreeRevision: mockSnapshot.treeRevision,
      expectedTimelineGeneration: mockSnapshot.expectedTimelineGeneration.generation,
    });
    expect(Object.keys(mockResult).sort()).toEqual(['sectionId']);

    const pending = http.addSection({ name: 'قسم تجريبي', expectedTreeRevision: 1, expectedTimelineGeneration: 1 });
    httpTesting.expectOne(`${baseUrl}/sections`).flush(successBody({ sectionId: 'section-1' }));
    const httpResult = await pending;
    expect(Object.keys(httpResult).sort()).toEqual(['sectionId']);
  });

  const conflictScenarios: Array<{
    readonly code: AbwabConflictCode;
    readonly mock: (port: AbwabCoreMock) => Promise<unknown>;
    readonly http: (port: AbwabCorePort) => Promise<unknown>;
    readonly httpPath: string;
  }> = [
    {
      code: 'abwab.section_name_conflict',
      mock: async (port) => {
        const snapshot = await port.getTreeSnapshot();
        await port.addSection({ name: 'قسم أ', expectedTreeRevision: snapshot.treeRevision, expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation });
        const after = await port.getTreeSnapshot();
        return port.addSection({ name: 'قسم أ', expectedTreeRevision: after.treeRevision, expectedTimelineGeneration: after.expectedTimelineGeneration.generation });
      },
      http: (port) => port.addSection({ name: 'قسم أ', expectedTreeRevision: 1, expectedTimelineGeneration: 1 }),
      httpPath: '/sections',
    },
    {
      code: 'abwab.timeline_generation_stale',
      mock: (port) => port.addSection({ name: 'قسم ب', expectedTreeRevision: 1, expectedTimelineGeneration: 999 }),
      http: (port) => port.addSection({ name: 'قسم ب', expectedTreeRevision: 1, expectedTimelineGeneration: 999 }),
      httpPath: '/sections',
    },
    {
      code: 'abwab.tree_revision_stale',
      mock: (port) => port.addSection({ name: 'قسم ج', expectedTreeRevision: 999, expectedTimelineGeneration: 1 }),
      http: (port) => port.addSection({ name: 'قسم ج', expectedTreeRevision: 999, expectedTimelineGeneration: 1 }),
      httpPath: '/sections',
    },
    {
      code: 'abwab.category_cycle',
      mock: async (port) => {
        const snapshot = await port.getTreeSnapshot();
        const gen = snapshot.expectedTimelineGeneration.generation;
        const root = await port.addCategory({
          name: 'باب رئيسي', description: null, representativeQuranExcerpt: null, parentCategoryId: null, sectionId: null,
          expectedTreeRevision: (await port.getTreeSnapshot()).treeRevision, expectedTimelineGeneration: gen,
        });
        const child = await port.addCategory({
          name: 'باب فرعي', description: null, representativeQuranExcerpt: null, parentCategoryId: root.categoryId, sectionId: null,
          expectedTreeRevision: (await port.getTreeSnapshot()).treeRevision, expectedTimelineGeneration: gen,
        });
        const before = await port.getTreeSnapshot();
        return port.moveCategories({
          moves: [{ categoryId: root.categoryId, newParentCategoryId: child.categoryId, newSectionId: null, expectedVersion: 1 }],
          expectedTreeRevision: before.treeRevision,
          expectedTimelineGeneration: before.expectedTimelineGeneration.generation,
        });
      },
      http: (port) =>
        port.moveCategories({
          moves: [{ categoryId: 'root', newParentCategoryId: 'child', newSectionId: null, expectedVersion: 1 }],
          expectedTreeRevision: 1,
          expectedTimelineGeneration: 1,
        }),
      httpPath: '/categories/move',
    },
    {
      code: 'abwab.row_stale',
      mock: async (port) => {
        const snapshot = await port.getTreeSnapshot();
        const category = await port.addCategory({
          name: 'باب', description: null, representativeQuranExcerpt: null, parentCategoryId: null, sectionId: null,
          expectedTreeRevision: snapshot.treeRevision, expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
        });
        const after = await port.getTreeSnapshot();
        return port.editCategory(category.categoryId, {
          name: 'باب معدل', description: null, representativeQuranExcerpt: null,
          expectedVersion: 999, expectedTimelineGeneration: after.expectedTimelineGeneration.generation,
        });
      },
      http: (port) => port.editCategory('category-1', { name: 'باب معدل', description: null, representativeQuranExcerpt: null, expectedVersion: 999, expectedTimelineGeneration: 1 }),
      httpPath: '/categories/category-1',
    },
  ];

  for (const scenario of conflictScenarios) {
    it(`mock and HTTP both raise ${scenario.code} for the same scenario`, async () => {
      const mock = new AbwabCoreMock();
      await expect(scenario.mock(mock)).rejects.toMatchObject({ code: scenario.code });

      const pending = scenario.http(http).catch((error) => error);
      const request = httpTesting.expectOne((call) => call.url.startsWith(`${baseUrl}${scenario.httpPath}`));
      request.flush(conflictBody(scenario.code), { status: 409, statusText: 'Conflict' });
      const httpError = await pending;
      expect(httpError).toBeInstanceOf(AbwabConflictError);
      expect((httpError as AbwabConflictError).code).toBe(scenario.code);
    });
  }
});
