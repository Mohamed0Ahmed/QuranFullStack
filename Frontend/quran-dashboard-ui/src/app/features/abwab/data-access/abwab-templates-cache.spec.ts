import { describe, expect, it } from 'vitest';

import { InMemoryKeyValueStore } from '../../../core/caching/async-key-value-store';
import { CacheEntry } from '../../../core/caching/persistent-cache';
import { DoorTemplateDetailDto, DoorTemplateListDto } from '../../../core/api/generated/models';
import {
  AbwabTemplatesCache,
  abwabTemplateDetailCacheKey,
  abwabTemplateListCacheKey,
} from './abwab-templates-cache';

function list(templateCount: number): DoorTemplateListDto {
  return {
    schemaVersion: 1,
    templates: Array.from({ length: templateCount }, (_, index) => ({
      doorTemplateId: `template-${index}`,
      name: `قالب ${index}`,
      description: null,
      templateRevision: 0,
      isDeleted: false,
      version: 1,
      nodeCount: 0,
    })),
    expectedTimelineGeneration: { generation: 1 },
    serverTimeUtc: new Date().toISOString(),
  };
}

function detail(doorTemplateId: string, nodeCount: number): DoorTemplateDetailDto {
  return {
    schemaVersion: 1,
    template: {
      doorTemplateId,
      name: 'قالب',
      description: null,
      templateRevision: 0,
      isDeleted: false,
      version: 1,
      nodeCount,
    },
    nodes: Array.from({ length: nodeCount }, (_, index) => ({
      templateNodeId: `node-${index}`,
      parentTemplateNodeId: null,
      name: `عقدة ${index}`,
      representativeQuranExcerpt: null,
      description: null,
      siblingOrder: index,
      isDeleted: false,
      version: 1,
      aliases: [],
    })),
    expectedTimelineGeneration: { generation: 1 },
    treeRevision: 1,
    serverTimeUtc: new Date().toISOString(),
  };
}

function createCache() {
  const store = new InMemoryKeyValueStore<CacheEntry<DoorTemplateListDto | DoorTemplateDetailDto>>();
  return new AbwabTemplatesCache({ store });
}

describe('AbwabTemplatesCache', () => {
  it('addresses the list per includeDeleted and the detail per (template, includeDeleted)', () => {
    expect(abwabTemplateListCacheKey(false)).not.toBe(abwabTemplateListCacheKey(true));
    expect(abwabTemplateDetailCacheKey('t-1', false)).not.toBe(abwabTemplateDetailCacheKey('t-1', true));
    expect(abwabTemplateDetailCacheKey('t-1', false)).not.toBe(abwabTemplateDetailCacheKey('t-2', false));
    expect(abwabTemplateDetailCacheKey('t-1', false)).not.toBe(abwabTemplateListCacheKey(false));
  });

  it('has no cached projection before the first store', async () => {
    const cache = createCache();

    expect(await cache.getCachedList(false)).toBeUndefined();
    expect(await cache.getCachedDetail('t-1', false)).toBeUndefined();
  });

  it('returns the stored projections until invalidated', async () => {
    const cache = createCache();
    const storedList = list(2);
    const storedDetail = detail('t-1', 3);

    await cache.storeList(false, storedList);
    await cache.storeDetail('t-1', false, storedDetail);

    expect(await cache.getCachedList(false)).toEqual(storedList);
    expect(await cache.getCachedDetail('t-1', false)).toEqual(storedDetail);
  });

  it('the active and with-deleted projections are stored independently on both resources', async () => {
    const cache = createCache();
    await cache.storeList(false, list(1));
    await cache.storeList(true, list(3));
    await cache.storeDetail('t-1', false, detail('t-1', 1));
    await cache.storeDetail('t-1', true, detail('t-1', 4));

    expect((await cache.getCachedList(false))!.templates).toHaveLength(1);
    expect((await cache.getCachedList(true))!.templates).toHaveLength(3);
    expect((await cache.getCachedDetail('t-1', false))!.nodes).toHaveLength(1);
    expect((await cache.getCachedDetail('t-1', true))!.nodes).toHaveLength(4);
  });

  // A delete moves a template between the list projections and a node edit changes the detail, so a
  // mutation has to clear all four keys of the affected template together.
  it('post-mutation: invalidate() clears BOTH list projections and BOTH detail projections of the template', async () => {
    const cache = createCache();
    await cache.storeList(false, list(1));
    await cache.storeList(true, list(2));
    await cache.storeDetail('t-1', false, detail('t-1', 1));
    await cache.storeDetail('t-1', true, detail('t-1', 2));

    await cache.invalidate('t-1');

    expect(await cache.getCachedList(false)).toBeUndefined();
    expect(await cache.getCachedList(true)).toBeUndefined();
    expect(await cache.getCachedDetail('t-1', false)).toBeUndefined();
    expect(await cache.getCachedDetail('t-1', true)).toBeUndefined();
  });

  it('invalidating with no selected template still clears both list projections, because an add changes them', async () => {
    const cache = createCache();
    await cache.storeList(false, list(1));
    await cache.storeList(true, list(2));
    await cache.storeDetail('t-1', false, detail('t-1', 1));

    await cache.invalidate(null);

    expect(await cache.getCachedList(false)).toBeUndefined();
    expect(await cache.getCachedList(true)).toBeUndefined();
    expect(await cache.getCachedDetail('t-1', false)).toBeDefined();
  });

  it('invalidating one template leaves another template detail cached', async () => {
    const cache = createCache();
    await cache.storeDetail('t-1', false, detail('t-1', 1));
    await cache.storeDetail('t-2', false, detail('t-2', 1));

    await cache.invalidate('t-1');

    expect(await cache.getCachedDetail('t-1', false)).toBeUndefined();
    expect(await cache.getCachedDetail('t-2', false)).toBeDefined();
  });
});
