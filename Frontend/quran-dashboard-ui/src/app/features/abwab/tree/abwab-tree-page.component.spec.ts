import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { AbwabCoreMock } from '../data-access/abwab-core.mock';
import { ABWAB_CORE_PORT } from '../data-access/abwab-core.port';
import { AbwabMutationStatus } from '../state/abwab-tree.facade';
import { AbwabTreePageComponent } from './abwab-tree-page.component';

function createPage() {
  TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting(), { provide: ABWAB_CORE_PORT, useValue: new AbwabCoreMock() }],
  });
  const fixture = TestBed.createComponent(AbwabTreePageComponent);
  fixture.detectChanges();
  return fixture;
}

async function waitForMutationSettled(status: () => AbwabMutationStatus): Promise<void> {
  for (let attempt = 0; attempt < 50; attempt += 1) {
    if (status() !== 'pending') {
      return;
    }
    await new Promise((resolve) => setTimeout(resolve, 5));
  }
  throw new Error('Mutation did not settle within the polling budget.');
}

// Blocker-fix proof (coordinator): expectedVersion must be SOURCED from the read snapshot, never
// manufactured. These tests drive the page's mutation methods (the ONLY place that builds the
// mutation request from the currently-selected row) against the real AbwabCoreMock and assert every
// edit/alias-edit succeeds on the FIRST try — a manufactured expectedVersion:0 would instead 409
// with abwab.row_stale on this exact path (proven separately in abwab-core-parity.spec.ts).
describe('AbwabTreePageComponent — snapshot-sourced expectedVersion (no manufactured value)', () => {
  it('editing a category on the first attempt succeeds using category.version from the snapshot', async () => {
    const fixture = createPage();
    const page = fixture.componentInstance;
    await page['facade'].load();

    const snapshot = page['facade'].snapshot()!;
    const created = await page['facade'].addCategory({
      name: 'باب اختبار',
      description: null,
      representativeQuranExcerpt: null,
      parentCategoryId: null,
      sectionId: null,
      expectedTreeRevision: snapshot.treeRevision,
      expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
    });
    page['facade'].selectCategory(created.categoryId);

    page['saveCategory']({ name: 'باب اختبار معدّل', description: 'وصف', representativeQuranExcerpt: null, newAliases: [] });
    await waitForMutationSettled(() => page['facade'].mutationStatus());

    expect(page['facade'].mutationStatus()).toBe('success');
    expect(page['facade'].selectedCategory()?.name).toBe('باب اختبار معدّل');
  });

  it('editing an existing alias on the first attempt succeeds using the ALIAS OWN version from the snapshot', async () => {
    const fixture = createPage();
    const page = fixture.componentInstance;
    await page['facade'].load();

    const snapshot = page['facade'].snapshot()!;
    const created = await page['facade'].addCategory({
      name: 'باب اختبار الاسم البديل',
      description: null,
      representativeQuranExcerpt: null,
      parentCategoryId: null,
      sectionId: null,
      expectedTreeRevision: snapshot.treeRevision,
      expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
    });
    const afterCreate = page['facade'].snapshot()!;
    const alias = await page['facade'].addCategoryAlias(created.categoryId, {
      value: 'اسم بديل',
      expectedTimelineGeneration: afterCreate.expectedTimelineGeneration.generation,
    });
    page['facade'].selectCategory(created.categoryId);

    const aliasDto = page['facade'].selectedCategory()!.aliases.find((a) => a.categorySearchAliasId === alias.aliasId)!;
    page['editExistingAlias']({ aliasId: alias.aliasId, value: 'اسم بديل معدّل', expectedVersion: aliasDto.version });
    await waitForMutationSettled(() => page['facade'].mutationStatus());

    expect(page['facade'].mutationStatus()).toBe('success');
    expect(page['facade'].selectedCategory()?.aliases.find((a) => a.categorySearchAliasId === alias.aliasId)?.value).toBe('اسم بديل معدّل');
  });

  it('lifting a manual protection on the first attempt succeeds using the RECORD OWN version from the protection read (3rd blocker fix)', async () => {
    const fixture = createPage();
    const page = fixture.componentInstance;
    await page['facade'].load();

    const snapshot = page['facade'].snapshot()!;
    const created = await page['facade'].addCategory({
      name: 'باب اختبار رفع الحماية',
      description: null,
      representativeQuranExcerpt: null,
      parentCategoryId: null,
      sectionId: null,
      expectedTreeRevision: snapshot.treeRevision,
      expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
    });
    page['facade'].selectCategory(created.categoryId);

    await page['facade'].applyManualProtection(created.categoryId, 3, {
      scope: 0,
      expectedVersion: null,
      expectedTimelineGeneration: page['facade'].snapshot()!.expectedTimelineGeneration.generation,
    });
    page['protectionProfile'].set(await page['facade'].loadProtectionProfile(created.categoryId));

    page['liftProtection']({ protectionType: 3 });
    await waitForMutationSettled(() => page['facade'].mutationStatus());

    expect(page['facade'].mutationStatus()).toBe('success');
    const afterLift = await page['facade'].loadProtectionProfile(created.categoryId);
    expect(afterLift.manualProtections.find((m) => m.protectionType === 3)?.isProtected).toBe(false);
  });

  it('the full five-type preset scope change succeeds on the first attempt: the ALREADY-ACTIVE type sources its own version, missing types need none (3rd blocker fix)', async () => {
    const fixture = createPage();
    const page = fixture.componentInstance;
    await page['facade'].load();

    const snapshot = page['facade'].snapshot()!;
    const created = await page['facade'].addCategory({
      name: 'باب اختبار الحماية الكاملة',
      description: null,
      representativeQuranExcerpt: null,
      parentCategoryId: null,
      sectionId: null,
      expectedTreeRevision: snapshot.treeRevision,
      expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
    });
    page['facade'].selectCategory(created.categoryId);

    // One type (CategoryData) is already active at CategoryOnly scope; the preset must move IT to
    // Subtree using its own version, and insert the other four missing types with no version needed.
    await page['facade'].applyManualProtection(created.categoryId, 0, {
      scope: 0,
      expectedVersion: null,
      expectedTimelineGeneration: page['facade'].snapshot()!.expectedTimelineGeneration.generation,
    });
    page['protectionProfile'].set(await page['facade'].loadProtectionProfile(created.categoryId));

    page['applyFullPreset']({ scope: 1 });
    await waitForMutationSettled(() => page['facade'].mutationStatus());

    expect(page['facade'].mutationStatus()).toBe('success');
    const afterPreset = await page['facade'].loadProtectionProfile(created.categoryId);
    expect(afterPreset.manualProtections).toHaveLength(5);
    expect(afterPreset.manualProtections.every((m) => m.isProtected && m.scope === 1)).toBe(true);
  });
});
