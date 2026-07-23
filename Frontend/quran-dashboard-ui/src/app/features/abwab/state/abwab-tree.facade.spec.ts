import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { AbwabCoreMock } from '../data-access/abwab-core.mock';
import { AbwabConflictError } from '../data-access/abwab-conflict';
import { ABWAB_CORE_PORT } from '../data-access/abwab-core.port';
import { AbwabTreeFacade } from './abwab-tree.facade';

function createFacade(): AbwabTreeFacade {
  TestBed.configureTestingModule({
    providers: [AbwabTreeFacade, { provide: ABWAB_CORE_PORT, useValue: new AbwabCoreMock() }],
  });
  return TestBed.inject(AbwabTreeFacade);
}

describe('AbwabTreeFacade', () => {
  it('loads the tree snapshot and exposes the seeded permanent default section', async () => {
    const facade = createFacade();
    await facade.load();

    expect(facade.status()).toBe('ready');
    expect(facade.sections()).toHaveLength(1);
    expect(facade.sections()[0].isPermanentDefault).toBe(true);
  });

  it('post-mutation context preservation: selection and expansion survive an edit on the same category', async () => {
    const facade = createFacade();
    await facade.load();
    const snapshot = facade.snapshot()!;

    const created = await facade.addCategory({
      name: 'باب أول',
      description: null,
      representativeQuranExcerpt: null,
      parentCategoryId: null,
      sectionId: null,
      expectedTreeRevision: snapshot.treeRevision,
      expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
    });

    facade.selectCategory(created.categoryId);
    facade.toggleExpanded(created.categoryId);
    expect(facade.selectedCategoryId()).toBe(created.categoryId);
    expect(facade.expandedCategoryIds().has(created.categoryId)).toBe(true);

    const fresh = facade.snapshot()!;
    await facade.editCategory(created.categoryId, {
      name: 'باب أول معدل',
      description: 'وصف',
      representativeQuranExcerpt: null,
      expectedVersion: 1,
      expectedTimelineGeneration: fresh.expectedTimelineGeneration.generation,
    });

    expect(facade.selectedCategoryId()).toBe(created.categoryId);
    expect(facade.expandedCategoryIds().has(created.categoryId)).toBe(true);
    expect(facade.selectedCategory()?.name).toBe('باب أول معدل');
  });

  it('post-mutation context preservation: selection is pruned once the selected category is actually gone', async () => {
    const facade = createFacade();
    await facade.load();
    const snapshot = facade.snapshot()!;

    const created = await facade.addCategory({
      name: 'باب للحذف',
      description: null,
      representativeQuranExcerpt: null,
      parentCategoryId: null,
      sectionId: null,
      expectedTreeRevision: snapshot.treeRevision,
      expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
    });
    facade.selectCategory(created.categoryId);

    const fresh = facade.snapshot()!;
    await facade.subtreeDeleteCategory(created.categoryId, {
      expectedVersion: 1,
      expectedTreeRevision: fresh.treeRevision,
      expectedTimelineGeneration: fresh.expectedTimelineGeneration.generation,
    });

    expect(facade.selectedCategoryId()).toBeNull();
  });

  it('rollback: a stale-tree-revision conflict re-syncs the view to server truth instead of applying the rejected change', async () => {
    const facade = createFacade();
    await facade.load();

    await expect(
      facade.addSection({ name: 'قسم متعارض', expectedTreeRevision: 999, expectedTimelineGeneration: 1 }),
    ).rejects.toBeInstanceOf(AbwabConflictError);

    expect(facade.mutationStatus()).toBe('conflict');
    expect(facade.status()).toBe('ready');
    // The rejected section never made it into the server-truth snapshot the facade re-synced to.
    expect(facade.sections().some((section) => section.name === 'قسم متعارض')).toBe(false);
  });
});
