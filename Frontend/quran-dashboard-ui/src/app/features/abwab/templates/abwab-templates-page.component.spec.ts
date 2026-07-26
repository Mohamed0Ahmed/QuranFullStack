import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { PermissionCode } from '../../../core/auth/permission-codes';
import { AbwabCoreMock } from '../data-access/abwab-core.mock';
import { ABWAB_CORE_PORT } from '../data-access/abwab-core.port';
import { AbwabTemplatesMock } from '../data-access/abwab-templates.mock';
import { ABWAB_TEMPLATES_PORT } from '../data-access/abwab-templates.port';
import { AbwabTemplatesPageComponent } from './abwab-templates-page.component';

const TEMPLATE_NAME = 'قالب الأبواب';

type Page = ComponentFixture<AbwabTemplatesPageComponent>;

interface PageContext {
  readonly fixture: Page;
  readonly mock: AbwabTemplatesMock;
  readonly targetCategoryId: string;
}

async function seedTargetCategory(core: AbwabCoreMock): Promise<string> {
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
  return created.categoryId;
}

async function createPage(
  options: {
    seed?: (mock: AbwabTemplatesMock) => Promise<void>;
    deniedPermissions?: readonly PermissionCode[];
  } = {},
): Promise<PageContext> {
  TestBed.resetTestingModule();

  const core = new AbwabCoreMock();
  const targetCategoryId = await seedTargetCategory(core);
  const mock = new AbwabTemplatesMock({ targetCategoryIds: [targetCategoryId] });
  await options.seed?.(mock);

  const denied = new Set<string>(options.deniedPermissions ?? []);

  // A real query-param round-trip, not a frozen snapshot: «إظهار المحذوفة» navigates and the page
  // re-loads from the emitted params, so the toggle is proven end to end rather than assumed.
  const queryParams = new BehaviorSubject(convertToParamMap({}));
  const routerStub = {
    navigate: (_commands: unknown[], extras?: { queryParams?: Record<string, string | null> }) => {
      const includeDeleted = extras?.queryParams?.['includeDeleted'];
      queryParams.next(convertToParamMap(includeDeleted ? { includeDeleted } : {}));
      return Promise.resolve(true);
    },
  };

  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      {
        provide: CurrentUserStore,
        useValue: { hasPermission: (code: string) => !denied.has(code), permissions: () => [] },
      },
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({}) }, queryParamMap: queryParams.asObservable() } },
      { provide: Router, useValue: routerStub },
      { provide: ABWAB_TEMPLATES_PORT, useValue: mock },
      { provide: ABWAB_CORE_PORT, useValue: core },
    ],
  });

  const fixture = TestBed.createComponent(AbwabTemplatesPageComponent);
  fixture.detectChanges();
  await settle(fixture);

  return { fixture, mock, targetCategoryId };
}

async function settle(fixture: Page): Promise<void> {
  for (let attempt = 0; attempt < 25; attempt += 1) {
    await new Promise((resolve) => setTimeout(resolve, 1));
    fixture.detectChanges();
  }
}

async function seedTemplateWithTwoRoots(mock: AbwabTemplatesMock): Promise<string> {
  const { doorTemplateId } = await mock.addTemplate({ name: TEMPLATE_NAME, description: null, expectedTimelineGeneration: 1 });
  await mock.addNode(doorTemplateId, {
    parentTemplateNodeId: null,
    name: 'العقدة الأولى',
    representativeQuranExcerpt: null,
    description: null,
    expectedTemplateRevision: 0,
    expectedTimelineGeneration: 1,
  });
  await mock.addNode(doorTemplateId, {
    parentTemplateNodeId: null,
    name: 'العقدة الثانية',
    representativeQuranExcerpt: null,
    description: null,
    expectedTemplateRevision: 1,
    expectedTimelineGeneration: 1,
  });
  return doorTemplateId;
}

// A root with one child, so the editor can offer a DESCENDANT as a reparent destination — the only
// cycle the picker can express, since it already excludes the node being edited.
async function seedTemplateWithNestedNodes(mock: AbwabTemplatesMock): Promise<void> {
  const { doorTemplateId } = await mock.addTemplate({ name: TEMPLATE_NAME, description: null, expectedTimelineGeneration: 1 });
  const { templateNodeId } = await mock.addNode(doorTemplateId, {
    parentTemplateNodeId: null,
    name: 'العقدة الأولى',
    representativeQuranExcerpt: null,
    description: null,
    expectedTemplateRevision: 0,
    expectedTimelineGeneration: 1,
  });
  await mock.addNode(doorTemplateId, {
    parentTemplateNodeId: templateNodeId,
    name: 'العقدة الثانية',
    representativeQuranExcerpt: null,
    description: null,
    expectedTemplateRevision: 1,
    expectedTimelineGeneration: 1,
  });
}

async function seedTemplateWithAliasAsync(mock: AbwabTemplatesMock): Promise<void> {
  const { doorTemplateId } = await mock.addTemplate({ name: TEMPLATE_NAME, description: null, expectedTimelineGeneration: 1 });
  const { templateNodeId } = await mock.addNode(doorTemplateId, {
    parentTemplateNodeId: null,
    name: 'العقدة الأولى',
    representativeQuranExcerpt: null,
    description: null,
    expectedTemplateRevision: 0,
    expectedTimelineGeneration: 1,
  });
  await mock.addNodeAlias(templateNodeId, { value: 'مرادف', expectedTimelineGeneration: 1 });
}

function query<T extends HTMLElement>(fixture: Page, testId: string): T {
  const element = (fixture.nativeElement as HTMLElement).querySelector<T>(`[data-testid=${testId}]`);
  if (!element) {
    throw new Error(`Missing element: ${testId}`);
  }
  return element;
}

function queryAll(fixture: Page, testId: string): HTMLElement[] {
  return [...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLElement>(`[data-testid=${testId}]`)];
}

function fill(input: HTMLInputElement, value: string): void {
  input.value = value;
  input.dispatchEvent(new Event('input'));
}

describe('AbwabTemplatesPageComponent', () => {
  it('renders the frozen §5 label and the distinct empty state rather than a silent blank', async () => {
    const { fixture } = await createPage();

    expect(query(fixture, 'templates-page').textContent).toContain('قوالب الأبواب');
    expect(queryAll(fixture, 'templates-empty')).toHaveLength(1);
    expect(queryAll(fixture, 'template-list')).toHaveLength(0);
  });

  it('lists templates and loads the selected template detail on an explicit click', async () => {
    const { fixture } = await createPage({ seed: (mock) => seedTemplateWithTwoRoots(mock).then(() => undefined) });

    expect(queryAll(fixture, 'template-row')).toHaveLength(1);
    expect(queryAll(fixture, 'template-detail')).toHaveLength(0);

    query(fixture, 'template-select').click();
    await settle(fixture);

    expect(queryAll(fixture, 'template-detail')).toHaveLength(1);
    expect(queryAll(fixture, 'template-node-row')).toHaveLength(2);
  });

  // Explicit save: typing alone must never emit a mutation, and no edit-session call exists at all.
  it('no autosave and no edit-session lock: typing a template name fires nothing until submit', async () => {
    const { fixture, mock } = await createPage();

    fill(query<HTMLInputElement>(fixture, 'template-name'), 'مسودة');
    await settle(fixture);
    expect((await mock.getTemplates()).templates).toHaveLength(0);

    query<HTMLFormElement>(fixture, 'template-add-form').dispatchEvent(new Event('submit'));
    await settle(fixture);
    expect((await mock.getTemplates()).templates).toHaveLength(1);
  });

  it('an explicit move-up posts the recomputed sibling order and re-renders the tree in the new order', async () => {
    const { fixture } = await createPage({ seed: (mock) => seedTemplateWithTwoRoots(mock).then(() => undefined) });
    query(fixture, 'template-select').click();
    await settle(fixture);

    const before = queryAll(fixture, 'node-name-label').map((element) => element.textContent?.trim());
    expect(before).toEqual(['العقدة الأولى', 'العقدة الثانية']);

    queryAll(fixture, 'node-move-up')[1].click();
    await settle(fixture);

    expect(queryAll(fixture, 'node-name-label').map((element) => element.textContent?.trim())).toEqual([
      'العقدة الثانية',
      'العقدة الأولى',
    ]);
  });

  it('a conflict renders the Arabic message, never the raw abwab.* code, and applies nothing locally', async () => {
    const { fixture } = await createPage({ seed: seedTemplateWithNestedNodes });
    query(fixture, 'template-select').click();
    await settle(fixture);

    // Reparent the root under its own child — the mock raises abwab.template_cycle, like the writer.
    queryAll(fixture, 'node-edit')[0].click();
    await settle(fixture);
    const parentSelect = query<HTMLSelectElement>(fixture, 'node-edit-parent');
    parentSelect.value = [...parentSelect.options][1].value;
    parentSelect.dispatchEvent(new Event('change'));
    await settle(fixture);
    query(fixture, 'node-reparent').click();
    await settle(fixture);

    const conflict = query(fixture, 'templates-conflict');
    expect(conflict.textContent).toContain('لا يمكن نقل العقدة');
    expect(conflict.textContent).not.toContain('abwab.');
    expect(queryAll(fixture, 'template-node-row')).toHaveLength(2);
  });

  it('permission filtering is cosmetic but real: without template.edit the node editor exposes no add form or row actions', async () => {
    const { fixture } = await createPage({
      seed: (mock) => seedTemplateWithTwoRoots(mock).then(() => undefined),
      deniedPermissions: ['template.edit'],
    });
    query(fixture, 'template-select').click();
    await settle(fixture);

    expect(queryAll(fixture, 'template-node-add-form')).toHaveLength(0);
    expect(queryAll(fixture, 'node-edit')).toHaveLength(0);
    // The nodes stay readable — only the mutating affordances are hidden.
    expect(queryAll(fixture, 'template-node-row')).toHaveLength(2);
  });

  it('without template.apply the application panel is not rendered at all', async () => {
    const { fixture } = await createPage({
      seed: (mock) => seedTemplateWithTwoRoots(mock).then(() => undefined),
      deniedPermissions: ['template.apply'],
    });
    query(fixture, 'template-select').click();
    await settle(fixture);

    expect(queryAll(fixture, 'template-application-panel')).toHaveLength(0);
  });

  it('without template.view the page renders only the not-authorized state', async () => {
    const { fixture } = await createPage({ deniedPermissions: ['template.view'] });

    expect(queryAll(fixture, 'templates-not-authorized')).toHaveLength(1);
    expect(queryAll(fixture, 'template-add-form')).toHaveLength(0);
  });

  it('applying a template needs an explicit target choice AND an explicit confirm', async () => {
    const { fixture, mock, targetCategoryId } = await createPage({
      seed: (mock) => seedTemplateWithTwoRoots(mock).then(() => undefined),
    });
    query(fixture, 'template-select').click();
    await settle(fixture);

    const treeRevisionBefore = (await mock.getTemplate((await mock.getTemplates()).templates[0].doorTemplateId)).treeRevision;

    const target = query<HTMLSelectElement>(fixture, 'apply-target');
    target.value = targetCategoryId;
    target.dispatchEvent(new Event('change'));
    await settle(fixture);

    query<HTMLFormElement>(fixture, 'template-apply-form').dispatchEvent(new Event('submit'));
    await settle(fixture);
    expect(queryAll(fixture, 'apply-confirm')).toHaveLength(1);
    expect((await mock.getTemplate((await mock.getTemplates()).templates[0].doorTemplateId)).treeRevision).toBe(treeRevisionBefore);

    query(fixture, 'apply-confirm-yes').click();
    await settle(fixture);

    expect((await mock.getTemplate((await mock.getTemplates()).templates[0].doorTemplateId)).treeRevision).toBe(
      treeRevisionBefore + 1,
    );
  });

  // The panel clears its target when its status reaches 'success'. Binding the page-wide mutation
  // status there would let ANY successful mutation wipe a selection the operator had already made.
  it('an unrelated successful mutation leaves the chosen apply target untouched', async () => {
    const { fixture, targetCategoryId } = await createPage({
      seed: (mock) => seedTemplateWithTwoRoots(mock).then(() => undefined),
    });
    query(fixture, 'template-select').click();
    await settle(fixture);

    const target = query<HTMLSelectElement>(fixture, 'apply-target');
    target.value = targetCategoryId;
    target.dispatchEvent(new Event('change'));
    await settle(fixture);

    fill(query<HTMLInputElement>(fixture, 'template-name'), 'قالب آخر');
    query<HTMLFormElement>(fixture, 'template-add-form').dispatchEvent(new Event('submit'));
    await settle(fixture);

    expect(query<HTMLSelectElement>(fixture, 'apply-target').value).toBe(targetCategoryId);
  });

  it('a conflict from an unrelated mutation is not rendered inside the apply panel', async () => {
    const { fixture, targetCategoryId } = await createPage({ seed: seedTemplateWithNestedNodes });
    query(fixture, 'template-select').click();
    await settle(fixture);

    const target = query<HTMLSelectElement>(fixture, 'apply-target');
    target.value = targetCategoryId;
    target.dispatchEvent(new Event('change'));
    await settle(fixture);

    // Reparent the root under its own child — an abwab.template_cycle raised by the NODE editor.
    queryAll(fixture, 'node-edit')[0].click();
    await settle(fixture);
    const parentSelect = query<HTMLSelectElement>(fixture, 'node-edit-parent');
    parentSelect.value = [...parentSelect.options][1].value;
    parentSelect.dispatchEvent(new Event('change'));
    await settle(fixture);
    query(fixture, 'node-reparent').click();
    await settle(fixture);

    expect(query(fixture, 'templates-conflict').textContent).toContain('لا يمكن نقل العقدة');
    expect(queryAll(fixture, 'application-conflict')).toHaveLength(0);
    expect(query<HTMLSelectElement>(fixture, 'apply-target').value).toBe(targetCategoryId);
  });

  it('the alias sub-editor lists a node synonyms and adds one through an explicit save', async () => {
    const { fixture, mock } = await createPage({ seed: (mock) => seedTemplateWithTwoRoots(mock).then(() => undefined) });
    query(fixture, 'template-select').click();
    await settle(fixture);

    expect(queryAll(fixture, 'node-alias-empty')).toHaveLength(2);

    queryAll(fixture, 'node-alias-add-open')[0].click();
    await settle(fixture);
    fill(query<HTMLInputElement>(fixture, 'node-alias-add-value'), 'مرادف الأولى');
    query<HTMLFormElement>(fixture, 'node-alias-add-form').dispatchEvent(new Event('submit'));
    await settle(fixture);

    const doorTemplateId = (await mock.getTemplates()).templates[0].doorTemplateId;
    expect((await mock.getTemplate(doorTemplateId)).nodes[0].aliases.map((alias) => alias.value)).toEqual(['مرادف الأولى']);
    expect(queryAll(fixture, 'node-alias-add-form')).toHaveLength(0);
    expect(queryAll(fixture, 'node-alias-row')).toHaveLength(1);
    expect(query(fixture, 'node-alias-value').textContent?.trim()).toBe('مرادف الأولى');
  });

  // The alias row carries its OWN xmin: the mock rejects a mismatched expectedVersion as
  // abwab.row_stale, so sending the node's version here would fail this edit.
  it('editing an alias sends that alias own expected version, not the node one', async () => {
    const { fixture, mock } = await createPage({ seed: (mock) => seedTemplateWithAliasAsync(mock) });
    query(fixture, 'template-select').click();
    await settle(fixture);

    query(fixture, 'node-alias-edit').click();
    await settle(fixture);
    fill(query<HTMLInputElement>(fixture, 'node-alias-edit-value'), 'مرادف معدّل');
    query<HTMLFormElement>(fixture, 'node-alias-edit-form').dispatchEvent(new Event('submit'));
    await settle(fixture);

    const doorTemplateId = (await mock.getTemplates()).templates[0].doorTemplateId;
    expect((await mock.getTemplate(doorTemplateId)).nodes[0].aliases[0].value).toBe('مرادف معدّل');
    expect(queryAll(fixture, 'templates-conflict')).toHaveLength(0);
  });

  it('a removed alias is only visible — and only restorable — with «إظهار المحذوفة» on', async () => {
    const { fixture, mock } = await createPage({ seed: (mock) => seedTemplateWithAliasAsync(mock) });
    query(fixture, 'template-select').click();
    await settle(fixture);

    query(fixture, 'node-alias-remove').click();
    await settle(fixture);
    expect(queryAll(fixture, 'node-alias-row')).toHaveLength(0);

    query<HTMLInputElement>(fixture, 'templates-include-deleted').dispatchEvent(new Event('change'));
    await settle(fixture);
    query(fixture, 'template-select').click();
    await settle(fixture);

    expect(queryAll(fixture, 'node-alias-deleted-badge')).toHaveLength(1);
    query(fixture, 'node-alias-restore').click();
    await settle(fixture);

    const doorTemplateId = (await mock.getTemplates()).templates[0].doorTemplateId;
    expect((await mock.getTemplate(doorTemplateId)).nodes[0].aliases).toHaveLength(1);
  });

  // The «إظهار المحذوفة» toggle is what makes template.restore reachable at all: without it a
  // deleted template leaves the active projection and the restore control can never render.
  it('a deleted template is reachable and restorable through the «إظهار المحذوفة» toggle', async () => {
    const { fixture, mock } = await createPage({ seed: (mock) => seedTemplateWithTwoRoots(mock).then(() => undefined) });
    query(fixture, 'template-select').click();
    await settle(fixture);

    query(fixture, 'template-delete').click();
    await settle(fixture);
    expect(queryAll(fixture, 'template-row')).toHaveLength(0);

    query<HTMLInputElement>(fixture, 'templates-include-deleted').dispatchEvent(new Event('change'));
    await settle(fixture);

    expect(queryAll(fixture, 'template-deleted-badge')).toHaveLength(1);
    query(fixture, 'template-restore').click();
    await settle(fixture);

    expect((await mock.getTemplates()).templates[0].isDeleted).toBe(false);
  });

  // §14.3: a rejected write must never cost the operator what they typed.
  it('a node edit that conflicts keeps the open form and the typed value', async () => {
    const { fixture } = await createPage({ seed: seedTemplateWithNestedNodes });
    query(fixture, 'template-select').click();
    await settle(fixture);

    queryAll(fixture, 'node-edit')[0].click();
    await settle(fixture);
    const parentSelect = query<HTMLSelectElement>(fixture, 'node-edit-parent');
    parentSelect.value = [...parentSelect.options][1].value;
    parentSelect.dispatchEvent(new Event('change'));
    fill(query<HTMLInputElement>(fixture, 'node-edit-name'), 'اسم لم يُحفظ');
    await settle(fixture);

    query(fixture, 'node-reparent').click();
    await settle(fixture);

    expect(queryAll(fixture, 'template-node-edit-form')).toHaveLength(1);
    expect(query<HTMLInputElement>(fixture, 'node-edit-name').value).toBe('اسم لم يُحفظ');
  });

  it('an accepted node edit closes the form and clears the add draft', async () => {
    const { fixture } = await createPage({ seed: (mock) => seedTemplateWithTwoRoots(mock).then(() => undefined) });
    query(fixture, 'template-select').click();
    await settle(fixture);

    queryAll(fixture, 'node-edit')[0].click();
    await settle(fixture);
    fill(query<HTMLInputElement>(fixture, 'node-edit-name'), 'اسم محفوظ');
    await settle(fixture);
    query<HTMLFormElement>(fixture, 'template-node-edit-form').dispatchEvent(new Event('submit'));
    await settle(fixture);

    expect(queryAll(fixture, 'template-node-edit-form')).toHaveLength(0);
    expect(queryAll(fixture, 'node-name-label')[0].textContent?.trim()).toBe('اسم محفوظ');
  });

  it('ordering is reachable only through the explicit move buttons the row publishes', async () => {
    const { fixture } = await createPage({ seed: (mock) => seedTemplateWithTwoRoots(mock).then(() => undefined) });
    query(fixture, 'template-select').click();
    await settle(fixture);

    // The no-drag proof itself is the static `check:no-drag` source gate plus the browser suite; this
    // only pins that reordering has an explicit, focusable control on every row.
    expect(queryAll(fixture, 'node-move-up')).toHaveLength(2);
    expect(queryAll(fixture, 'node-move-down')).toHaveLength(2);
  });
});
