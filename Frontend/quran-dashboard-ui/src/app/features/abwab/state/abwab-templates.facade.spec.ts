import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { AbwabCoreMock } from '../data-access/abwab-core.mock';
import { ABWAB_CORE_PORT } from '../data-access/abwab-core.port';
import { AbwabConflictError } from '../data-access/abwab-conflict';
import { AbwabTemplatesCache, ABWAB_TEMPLATES_CACHE_PROVIDER } from '../data-access/abwab-templates-cache';
import { AbwabTemplatesMock } from '../data-access/abwab-templates.mock';
import { ABWAB_TEMPLATES_PORT, AddDoorTemplateResult } from '../data-access/abwab-templates.port';
import { AddDoorTemplateRequest, DoorTemplateListDto } from '../../../core/api/generated/models';
import { AbwabTemplatesFacade } from './abwab-templates.facade';

const targetCategoryId = 'category-target';
const INVALID_REQUEST_MESSAGE = 'طلب غير صالح.';

class RejectingTemplatesPort extends AbwabTemplatesMock {
  override addTemplate(_request: AddDoorTemplateRequest): Promise<AddDoorTemplateResult> {
    return Promise.reject(new Error(INVALID_REQUEST_MESSAGE));
  }
}

class FailingCorePort extends AbwabCoreMock {
  override getTreeSnapshot(): never {
    throw new Error('tree unavailable');
  }
}

function createFacade(
  templatesPort: AbwabTemplatesMock = new AbwabTemplatesMock({ targetCategoryIds: [targetCategoryId] }),
  corePort: AbwabCoreMock = new AbwabCoreMock(),
) {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      AbwabTemplatesFacade,
      ABWAB_TEMPLATES_CACHE_PROVIDER,
      { provide: ABWAB_TEMPLATES_PORT, useValue: templatesPort },
      { provide: ABWAB_CORE_PORT, useValue: corePort },
    ],
  });
  return {
    facade: TestBed.inject(AbwabTemplatesFacade),
    port: templatesPort,
    cache: TestBed.inject(AbwabTemplatesCache),
  };
}

async function seedTemplateWithRoot(port: AbwabTemplatesMock): Promise<{ doorTemplateId: string; templateNodeId: string }> {
  const { doorTemplateId } = await port.addTemplate({ name: 'قالب', description: null, expectedTimelineGeneration: 1 });
  const { templateNodeId } = await port.addNode(doorTemplateId, {
    parentTemplateNodeId: null,
    name: 'جذر',
    representativeQuranExcerpt: null,
    description: null,
    expectedTemplateRevision: 0,
    expectedTimelineGeneration: 1,
  });
  return { doorTemplateId, templateNodeId };
}

describe('AbwabTemplatesFacade', () => {
  it('loads the template projection and exposes the server TimelineGeneration', async () => {
    const { facade, port } = createFacade();
    await seedTemplateWithRoot(port);

    await facade.load();

    expect(facade.status()).toBe('ready');
    expect(facade.templates()).toHaveLength(1);
    expect(facade.expectedTimelineGeneration()).toBe(1);
  });

  it('an empty projection reports the distinct empty status rather than a silent blank ready state', async () => {
    const { facade } = createFacade();

    await facade.load();

    expect(facade.status()).toBe('empty');
    expect(facade.templates()).toHaveLength(0);
  });

  it('selecting a template loads its detail, exposing TemplateRevision and TreeRevision for the writers', async () => {
    const { facade, port } = createFacade();
    const { doorTemplateId } = await seedTemplateWithRoot(port);
    await facade.load();

    await facade.selectTemplate(doorTemplateId);

    expect(facade.selectedTemplateId()).toBe(doorTemplateId);
    expect(facade.nodes()).toHaveLength(1);
    expect(facade.templateRevision()).toBe(1);
    expect(facade.treeRevision()).toBe(1);
  });

  // The §14.1 rule lives in ONE place — runMutation() — and applies to BOTH outcomes.
  it('cache rule on SUCCESS: the mutation invalidates the cached projections and reloads from the server', async () => {
    const { facade, port, cache } = createFacade();
    const { doorTemplateId } = await seedTemplateWithRoot(port);
    await facade.load();
    await facade.selectTemplate(doorTemplateId);

    await cache.storeList(false, { ...(await port.getTemplates()), templates: [] } as DoorTemplateListDto);

    await facade.addNode(doorTemplateId, {
      parentTemplateNodeId: null,
      name: 'جذر ثانٍ',
      representativeQuranExcerpt: null,
      description: null,
      expectedTemplateRevision: facade.templateRevision()!,
      expectedTimelineGeneration: facade.expectedTimelineGeneration()!,
    });

    expect(facade.mutationStatus()).toBe('success');
    expect(facade.nodes()).toHaveLength(2);
    // The stale hand-planted cache entry was invalidated, then rewritten from the server reload.
    expect((await cache.getCachedList(false))!.templates).toHaveLength(1);
  });

  it('cache rule on CONFLICT: the failed mutation still invalidates and reloads, re-syncing to server truth', async () => {
    const { facade, port, cache } = createFacade();
    const { doorTemplateId, templateNodeId } = await seedTemplateWithRoot(port);
    await facade.load();
    await facade.selectTemplate(doorTemplateId);

    await cache.storeDetail('another-template', false, await port.getTemplate(doorTemplateId));

    await expect(
      facade.reparentNode(doorTemplateId, templateNodeId, {
        newParentTemplateNodeId: templateNodeId,
        expectedVersion: facade.nodes()[0].version,
        expectedTemplateRevision: facade.templateRevision()!,
        expectedTimelineGeneration: facade.expectedTimelineGeneration()!,
      }),
    ).rejects.toBeInstanceOf(AbwabConflictError);

    expect(facade.mutationStatus()).toBe('conflict');
    expect(facade.status()).toBe('ready');
    expect(facade.nodes()).toHaveLength(1);
    expect(facade.nodes()[0].parentTemplateNodeId).toBeNull();
    expect(await cache.getCachedDetail(doorTemplateId, false)).toBeDefined();
    // Invalidation is scoped to the mutated template, never a blanket cache wipe.
    expect(await cache.getCachedDetail('another-template', false)).toBeDefined();
  });

  it('mutationStatus walks idle → success and idle → conflict, never skipping straight back to idle', async () => {
    const { facade, port } = createFacade();
    const { doorTemplateId } = await seedTemplateWithRoot(port);
    await facade.load();
    await facade.selectTemplate(doorTemplateId);
    expect(facade.mutationStatus()).toBe('idle');

    await facade.editTemplate(doorTemplateId, {
      name: 'قالب معدّل',
      description: null,
      expectedVersion: facade.templates()[0].version,
      expectedTimelineGeneration: facade.expectedTimelineGeneration()!,
    });
    expect(facade.mutationStatus()).toBe('success');
    expect(facade.mutationError()).toBeNull();

    await expect(
      facade.editTemplate(doorTemplateId, {
        name: 'قالب آخر',
        description: null,
        expectedVersion: 999,
        expectedTimelineGeneration: facade.expectedTimelineGeneration()!,
      }),
    ).rejects.toBeInstanceOf(AbwabConflictError);
    expect(facade.mutationStatus()).toBe('conflict');
    expect(facade.mutationError()).toBeInstanceOf(AbwabConflictError);
  });

  it('a NON-conflict mutation failure is still exposed through mutationError so the page can render it', async () => {
    const { facade } = createFacade(new RejectingTemplatesPort({ targetCategoryIds: [targetCategoryId] }));
    await facade.load();

    await expect(
      facade.addTemplate({ name: 'قالب', description: null, expectedTimelineGeneration: 1 }),
    ).rejects.toThrow(INVALID_REQUEST_MESSAGE);

    expect(facade.mutationStatus()).toBe('error');
    expect(facade.mutationError()).not.toBeNull();
    expect(facade.mutationError()).not.toBeInstanceOf(AbwabConflictError);
    expect(facade.mutationError()!.message).toBe(INVALID_REQUEST_MESSAGE);
  });

  it('post-mutation context preservation: the selected template survives a mutation reload', async () => {
    const { facade, port } = createFacade();
    const { doorTemplateId } = await seedTemplateWithRoot(port);
    await facade.load();
    await facade.selectTemplate(doorTemplateId);

    await facade.addTemplate({ name: 'قالب آخر', description: null, expectedTimelineGeneration: 1 });

    expect(facade.selectedTemplateId()).toBe(doorTemplateId);
    expect(facade.nodes()).toHaveLength(1);
  });

  it('post-mutation context preservation: a selection that genuinely left the projection is pruned', async () => {
    const { facade, port } = createFacade();
    const { doorTemplateId } = await seedTemplateWithRoot(port);
    await facade.load();
    await facade.selectTemplate(doorTemplateId);

    await facade.deleteTemplate(doorTemplateId, {
      expectedVersion: facade.templates()[0].version,
      expectedTimelineGeneration: facade.expectedTimelineGeneration()!,
    });

    expect(facade.selectedTemplateId()).toBeNull();
    expect(facade.detail()).toBeNull();
  });

  // A transport failure is NOT a template that vanished: the operator's selection survives it and a
  // retry is offered, while an abwab.row_stale detail read genuinely prunes the selection.
  it('a transport failure on the detail read keeps the selection and surfaces a retryable error', async () => {
    const port = new AbwabTemplatesMock({ targetCategoryIds: [targetCategoryId] });
    const { facade } = createFacade(port);
    const { doorTemplateId } = await seedTemplateWithRoot(port);
    await facade.load();
    await facade.selectTemplate(doorTemplateId);

    const readable = port.getTemplate.bind(port);
    port.getTemplate = () => Promise.reject(new Error('network down'));
    await facade.retryDetail();

    expect(facade.selectedTemplateId()).toBe(doorTemplateId);
    expect(facade.detailErrorMessage()).not.toBeNull();

    port.getTemplate = readable;
    await facade.retryDetail();

    expect(facade.detailErrorMessage()).toBeNull();
    expect(facade.detail()).not.toBeNull();
  });

  // A template deleted by SOMEONE ELSE leaves the projection without this client ever issuing the
  // mutation. Its absence from the fresh list is the only signal — a read never returns a conflict —
  // so the reload must prune the selection rather than leave a stale detail panel open.
  it('a template another actor deleted is pruned by the next reload, with no error banner', async () => {
    const port = new AbwabTemplatesMock({ targetCategoryIds: [targetCategoryId] });
    const { facade } = createFacade(port);
    const { doorTemplateId } = await seedTemplateWithRoot(port);
    await facade.load();
    await facade.selectTemplate(doorTemplateId);
    expect(facade.detail()).not.toBeNull();

    await port.deleteTemplate(doorTemplateId, {
      expectedVersion: facade.templates()[0].version,
      expectedTimelineGeneration: facade.expectedTimelineGeneration()!,
    });
    await facade.reload();

    expect(facade.selectedTemplateId()).toBeNull();
    expect(facade.detail()).toBeNull();
    expect(facade.detailErrorMessage()).toBeNull();
  });

  it('the apply target picker is best-effort: a failing 029 tree read never moves the list out of its own state', async () => {
    const { facade, port } = createFacade(
      new AbwabTemplatesMock({ targetCategoryIds: [targetCategoryId] }),
      new FailingCorePort(),
    );
    await seedTemplateWithRoot(port);

    await facade.load();

    expect(facade.status()).toBe('ready');
    expect(facade.templates()).toHaveLength(1);
    expect(facade.targetCandidates()).toHaveLength(0);
  });

  it('the apply target picker offers the doors from the 029 projection', async () => {
    const core = new AbwabCoreMock();
    const snapshot = await core.getTreeSnapshot();
    const created = await core.addCategory({
      name: 'باب الصبر',
      description: null,
      representativeQuranExcerpt: null,
      parentCategoryId: null,
      sectionId: null,
      expectedTreeRevision: snapshot.treeRevision,
      expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
    });

    const { facade } = createFacade(new AbwabTemplatesMock({ targetCategoryIds: [targetCategoryId] }), core);
    await facade.load();

    expect(facade.targetCandidates().map((category) => category.categoryId)).toContain(created.categoryId);
  });
});
