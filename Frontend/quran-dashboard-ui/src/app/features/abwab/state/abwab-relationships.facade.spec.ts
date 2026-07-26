import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { AbwabCoreMock } from '../data-access/abwab-core.mock';
import { ABWAB_CORE_PORT } from '../data-access/abwab-core.port';
import { AbwabConflictError } from '../data-access/abwab-conflict';
import { ABWAB_RELATIONSHIPS_CACHE_PROVIDER } from '../data-access/abwab-relationships-cache';
import { AbwabRelationshipsMock } from '../data-access/abwab-relationships.mock';
import {
  ABWAB_RELATIONSHIPS_PORT,
  AddRelationshipResult,
  RELATIONSHIP_TYPE_SIMILAR,
} from '../data-access/abwab-relationships.port';
import { AddRelationshipRequest } from '../../../core/api/generated/models';
import { AbwabRelationshipsFacade } from './abwab-relationships.facade';

const categoryA = 'category-a';
const categoryB = 'category-b';
const INVALID_REQUEST_MESSAGE = 'طلب غير صالح.';

class RejectingRelationshipsPort extends AbwabRelationshipsMock {
  override addRelationship(_request: AddRelationshipRequest): Promise<AddRelationshipResult> {
    return Promise.reject(new Error(INVALID_REQUEST_MESSAGE));
  }
}

function createFacade(relationshipsPort: AbwabRelationshipsMock = new AbwabRelationshipsMock({ categoryIds: [categoryA, categoryB] })) {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      AbwabRelationshipsFacade,
      ABWAB_RELATIONSHIPS_CACHE_PROVIDER,
      { provide: ABWAB_RELATIONSHIPS_PORT, useValue: relationshipsPort },
      { provide: ABWAB_CORE_PORT, useValue: new AbwabCoreMock() },
    ],
  });
  return { facade: TestBed.inject(AbwabRelationshipsFacade), port: relationshipsPort };
}

async function seedRelationship(port: AbwabRelationshipsMock): Promise<string> {
  const generation = (await port.getRelationships(categoryA)).expectedTimelineGeneration.generation;
  const added = await port.addRelationship({
    relationshipType: RELATIONSHIP_TYPE_SIMILAR,
    firstCategoryId: categoryA,
    secondCategoryId: categoryB,
    expectedTimelineGeneration: generation,
  });
  return added.relationshipId;
}

describe('AbwabRelationshipsFacade', () => {
  it('loads the relationship projection for the routed category and exposes the server TimelineGeneration', async () => {
    const { facade, port } = createFacade();
    await seedRelationship(port);

    await facade.load(categoryA);

    expect(facade.status()).toBe('ready');
    expect(facade.categoryId()).toBe(categoryA);
    expect(facade.relationships()).toHaveLength(1);
    expect(facade.expectedTimelineGeneration()).toBe(1);
  });

  it('an empty projection reports the distinct empty status rather than a silent blank ready state', async () => {
    const { facade } = createFacade();

    await facade.load(categoryA);

    expect(facade.status()).toBe('empty');
    expect(facade.relationships()).toHaveLength(0);
  });

  it('post-mutation context preservation: the selected relationship survives a mutation reload', async () => {
    const { facade, port } = createFacade();
    const seeded = await seedRelationship(port);
    await facade.load(categoryA);
    facade.selectRelationship(seeded);

    const other = await facade.addRelationship({
      relationshipType: RELATIONSHIP_TYPE_SIMILAR,
      firstCategoryId: categoryA,
      secondCategoryId: 'category-c',
      expectedTimelineGeneration: facade.expectedTimelineGeneration()!,
    });

    expect(other.relationshipId).toBeDefined();
    expect(facade.selectedRelationshipId()).toBe(seeded);
    expect(facade.selectedRelationship()?.categoryRelationshipId).toBe(seeded);
  });

  it('post-mutation context preservation: a selection that genuinely left the projection is pruned', async () => {
    const { facade, port } = createFacade();
    const seeded = await seedRelationship(port);
    await facade.load(categoryA);
    facade.selectRelationship(seeded);

    await facade.deleteRelationship(seeded, {
      expectedVersion: facade.relationships()[0].version,
      expectedTimelineGeneration: facade.expectedTimelineGeneration()!,
    });

    expect(facade.selectedRelationshipId()).toBeNull();
  });

  it('rollback: a duplicate conflict re-syncs to server truth and never leaves a phantom row behind', async () => {
    const { facade, port } = createFacade();
    await seedRelationship(port);
    await facade.load(categoryA);

    await expect(
      facade.addRelationship({
        relationshipType: RELATIONSHIP_TYPE_SIMILAR,
        firstCategoryId: categoryB,
        secondCategoryId: categoryA,
        expectedTimelineGeneration: facade.expectedTimelineGeneration()!,
      }),
    ).rejects.toBeInstanceOf(AbwabConflictError);

    expect(facade.mutationStatus()).toBe('conflict');
    expect(facade.status()).toBe('ready');
    expect(facade.relationships()).toHaveLength(1);
  });

  it('a NON-conflict mutation failure is still exposed through mutationError so the page can render it', async () => {
    const { facade } = createFacade(new RejectingRelationshipsPort({ categoryIds: [categoryA, categoryB] }));
    await facade.load(categoryA);

    await expect(
      facade.addRelationship({
        relationshipType: RELATIONSHIP_TYPE_SIMILAR,
        firstCategoryId: categoryA,
        secondCategoryId: categoryB,
        expectedTimelineGeneration: facade.expectedTimelineGeneration()!,
      }),
    ).rejects.toThrow(INVALID_REQUEST_MESSAGE);

    expect(facade.mutationStatus()).toBe('error');
    expect(facade.mutationError()).not.toBeNull();
    expect(facade.mutationError()).not.toBeInstanceOf(AbwabConflictError);
    expect(facade.mutationError()!.message).toBe(INVALID_REQUEST_MESSAGE);
  });

  it('the endpoint picker offers the other doors from the 029 projection and never the routed door itself', async () => {
    TestBed.resetTestingModule();
    const core = new AbwabCoreMock();
    const snapshot = await core.getTreeSnapshot();
    const routed = await core.addCategory({
      name: 'باب الصبر',
      description: null,
      representativeQuranExcerpt: null,
      parentCategoryId: null,
      sectionId: null,
      expectedTreeRevision: snapshot.treeRevision,
      expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
    });
    const afterFirst = await core.getTreeSnapshot();
    const other = await core.addCategory({
      name: 'باب الشكر',
      description: null,
      representativeQuranExcerpt: null,
      parentCategoryId: null,
      sectionId: null,
      expectedTreeRevision: afterFirst.treeRevision,
      expectedTimelineGeneration: afterFirst.expectedTimelineGeneration.generation,
    });

    TestBed.configureTestingModule({
      providers: [
        AbwabRelationshipsFacade,
        ABWAB_RELATIONSHIPS_CACHE_PROVIDER,
        { provide: ABWAB_RELATIONSHIPS_PORT, useValue: new AbwabRelationshipsMock() },
        { provide: ABWAB_CORE_PORT, useValue: core },
      ],
    });
    const facade = TestBed.inject(AbwabRelationshipsFacade);

    await facade.load(routed.categoryId);

    const candidateIds = facade.endpointCandidates().map((category) => category.categoryId);
    expect(candidateIds).toContain(other.categoryId);
    expect(candidateIds).not.toContain(routed.categoryId);
    expect(facade.routedCategoryName()).toBe('باب الصبر');
  });
});
