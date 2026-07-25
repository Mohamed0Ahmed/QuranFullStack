import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { AbwabCoreMock } from '../data-access/abwab-core.mock';
import { ABWAB_CORE_PORT } from '../data-access/abwab-core.port';
import { AbwabRelationshipsMock } from '../data-access/abwab-relationships.mock';
import {
  ABWAB_RELATIONSHIPS_PORT,
  RELATIONSHIP_TYPES,
  RELATIONSHIP_TYPE_BROADER_NARROWER,
  RELATIONSHIP_TYPE_OPPOSITE,
  RELATIONSHIP_TYPE_SIMILAR,
} from '../data-access/abwab-relationships.port';
import { AbwabRelationshipsPageComponent } from './abwab-relationships-page.component';
import {
  DIRECTIONAL_RELATIONSHIP_TYPE,
  RELATIONSHIP_TYPE_LABELS,
  RELATIONSHIP_TYPE_OPTIONS,
} from '../data-access/relationship-type-labels';

const ROUTED_NAME = 'باب الصبر';
const OTHER_NAME = 'باب الشكر';
const INVALID_REQUEST_MESSAGE = 'طلب غير صالح.';

class RejectingRelationshipsPort extends AbwabRelationshipsMock {
  override addRelationship(): Promise<never> {
    return Promise.reject(new Error(INVALID_REQUEST_MESSAGE));
  }
}

type Page = ComponentFixture<AbwabRelationshipsPageComponent>;

interface PageContext {
  readonly fixture: Page;
  readonly mock: AbwabRelationshipsMock;
  readonly routedCategoryId: string;
  readonly otherCategoryId: string;
}

async function seedTwoCategories(core: AbwabCoreMock): Promise<{ routedCategoryId: string; otherCategoryId: string }> {
  const add = async (name: string) => {
    const snapshot = await core.getTreeSnapshot();
    const created = await core.addCategory({
      name,
      description: null,
      representativeQuranExcerpt: null,
      parentCategoryId: null,
      sectionId: null,
      expectedTreeRevision: snapshot.treeRevision,
      expectedTimelineGeneration: snapshot.expectedTimelineGeneration.generation,
    });
    return created.categoryId;
  };

  return { routedCategoryId: await add(ROUTED_NAME), otherCategoryId: await add(OTHER_NAME) };
}

async function createPage(
  options: {
    makeMock?: (ids: { routedCategoryId: string; otherCategoryId: string }) => AbwabRelationshipsMock;
    seed?: (mock: AbwabRelationshipsMock, ids: { routedCategoryId: string; otherCategoryId: string }) => Promise<void>;
  } = {},
): Promise<PageContext> {
  TestBed.resetTestingModule();

  const core = new AbwabCoreMock();
  const ids = await seedTwoCategories(core);
  const mock = options.makeMock
    ? options.makeMock(ids)
    : new AbwabRelationshipsMock({ categoryIds: [ids.routedCategoryId, ids.otherCategoryId] });
  await options.seed?.(mock, ids);

  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      { provide: CurrentUserStore, useValue: { hasPermission: () => true, permissions: () => [] } },
      {
        provide: ActivatedRoute,
        useValue: {
          snapshot: { paramMap: convertToParamMap({ categoryId: ids.routedCategoryId }) },
          queryParamMap: of(convertToParamMap({})),
        },
      },
      { provide: ABWAB_RELATIONSHIPS_PORT, useValue: mock },
      { provide: ABWAB_CORE_PORT, useValue: core },
    ],
  });

  const fixture = TestBed.createComponent(AbwabRelationshipsPageComponent);
  fixture.detectChanges();
  await settle(fixture);

  return { fixture, mock, ...ids };
}

async function settle(fixture: Page): Promise<void> {
  for (let attempt = 0; attempt < 25; attempt += 1) {
    await new Promise((resolve) => setTimeout(resolve, 1));
    fixture.detectChanges();
  }
}

function selectByTestId(fixture: Page, testId: string): HTMLSelectElement {
  const element = (fixture.nativeElement as HTMLElement).querySelector<HTMLSelectElement>(`[data-testid=${testId}]`);
  if (!element) {
    throw new Error(`Missing control: ${testId}`);
  }
  return element;
}

// Drives the real <select> through a real change event, which is the ONLY way the reactive-forms
// SelectControlValueAccessor is exercised: it resolves the submitted value from its option map, so a
// [value]-bound enum option hands the form the option's STRING instead of the wire-contract integer.
function chooseOption(select: HTMLSelectElement, optionLabel: string): void {
  const option = [...select.options].find((candidate) => candidate.textContent?.trim() === optionLabel);
  if (!option) {
    throw new Error(`Missing option "${optionLabel}" among [${[...select.options].map((o) => o.textContent?.trim()).join(' | ')}]`);
  }
  select.value = option.value;
  select.dispatchEvent(new Event('change'));
}

describe('AbwabRelationshipsPageComponent — wire contract of the relationship form', () => {
  it('choosing «أعم / أخص» submits the INTEGER RelationshipType, so a directional edge is actually created', async () => {
    const { fixture, mock, routedCategoryId } = await createPage();

    chooseOption(selectByTestId(fixture, 'relationship-type'), 'أعم / أخص');
    fixture.detectChanges();
    chooseOption(selectByTestId(fixture, 'relationship-endpoint'), OTHER_NAME);
    fixture.detectChanges();

    fixture.componentInstance['submitAdd']();
    await settle(fixture);

    const stored = (await mock.getRelationships(routedCategoryId)).relationships;
    expect(stored).toHaveLength(1);
    expect(stored[0].relationshipType).toBe(RELATIONSHIP_TYPE_BROADER_NARROWER);
    expect(typeof stored[0].relationshipType).toBe('number');
    expect(stored[0].isDirectional).toBe(true);
  });

  it('choosing «مقابل» submits the INTEGER RelationshipType rather than its string form', async () => {
    const { fixture, mock, routedCategoryId } = await createPage();

    chooseOption(selectByTestId(fixture, 'relationship-type'), 'مقابل');
    fixture.detectChanges();
    chooseOption(selectByTestId(fixture, 'relationship-endpoint'), OTHER_NAME);
    fixture.detectChanges();

    fixture.componentInstance['submitAdd']();
    await settle(fixture);

    const stored = (await mock.getRelationships(routedCategoryId)).relationships;
    expect(stored).toHaveLength(1);
    expect(stored[0].relationshipType).toBe(RELATIONSHIP_TYPE_OPPOSITE);
    expect(typeof stored[0].relationshipType).toBe('number');
    expect(stored[0].isDirectional).toBe(false);
  });

  it('a directional edge can be authored INBOUND — the routed door chosen as the narrower end', async () => {
    const { fixture, mock, routedCategoryId, otherCategoryId } = await createPage();

    chooseOption(selectByTestId(fixture, 'relationship-type'), 'أعم / أخص');
    fixture.detectChanges();
    chooseOption(selectByTestId(fixture, 'relationship-direction'), 'هذا الباب هو الأخص');
    chooseOption(selectByTestId(fixture, 'relationship-endpoint'), OTHER_NAME);
    fixture.detectChanges();

    fixture.componentInstance['submitAdd']();
    await settle(fixture);

    const stored = (await mock.getRelationships(routedCategoryId)).relationships[0];
    expect(stored.from.categoryId).toBe(otherCategoryId);
    expect(stored.to.categoryId).toBe(routedCategoryId);
  });

  it.each([
    ['NARROWER', 'inbound'],
    ['BROADER', 'outbound'],
  ])(
    'editing a directional relationship from the %s door and saving unchanged preserves the stored direction',
    async (endpointRole) => {
      const routedIsNarrower = endpointRole === 'NARROWER';
      const { fixture, mock, routedCategoryId, otherCategoryId } = await createPage({
        seed: async (relationships, ids) => {
          const [broader, narrower] = routedIsNarrower
            ? [ids.otherCategoryId, ids.routedCategoryId]
            : [ids.routedCategoryId, ids.otherCategoryId];
          await relationships.addRelationship({
            relationshipType: RELATIONSHIP_TYPE_BROADER_NARROWER,
            firstCategoryId: broader,
            secondCategoryId: narrower,
            expectedTimelineGeneration: 1,
          });
        },
      });

      const expectedBroader = routedIsNarrower ? otherCategoryId : routedCategoryId;
      const expectedNarrower = routedIsNarrower ? routedCategoryId : otherCategoryId;

      const listed = (await mock.getRelationships(routedCategoryId)).relationships[0];
      expect(listed.from.categoryId).toBe(expectedBroader);

      const page = fixture.componentInstance;
      page['beginEdit'](listed);
      fixture.detectChanges();
      page['submitEdit'](listed);
      await settle(fixture);

      const afterSave = (await mock.getRelationships(routedCategoryId)).relationships[0];
      expect(afterSave.from.categoryId).toBe(expectedBroader);
      expect(afterSave.to.categoryId).toBe(expectedNarrower);
    },
  );

  it('a NON-conflict mutation failure renders a visible message instead of a silent no-op', async () => {
    const { fixture } = await createPage({
      makeMock: (ids) => new RejectingRelationshipsPort({ categoryIds: [ids.routedCategoryId, ids.otherCategoryId] }),
    });

    chooseOption(selectByTestId(fixture, 'relationship-endpoint'), OTHER_NAME);
    fixture.detectChanges();
    fixture.componentInstance['submitAdd']();
    await settle(fixture);

    const banner = (fixture.nativeElement as HTMLElement).querySelector('[data-testid=relationships-mutation-error]');
    expect(banner).not.toBeNull();
    expect(banner!.textContent).toContain(INVALID_REQUEST_MESSAGE);
  });

  it('a conflict renders the Arabic conflict message, never the raw abwab.* code', async () => {
    const { fixture } = await createPage({
      seed: (relationships, ids) =>
        relationships
          .addRelationship({
            relationshipType: RELATIONSHIP_TYPE_SIMILAR,
            firstCategoryId: ids.routedCategoryId,
            secondCategoryId: ids.otherCategoryId,
            expectedTimelineGeneration: 1,
          })
          .then(() => undefined),
    });

    chooseOption(selectByTestId(fixture, 'relationship-endpoint'), OTHER_NAME);
    fixture.detectChanges();
    fixture.componentInstance['submitAdd']();
    await settle(fixture);

    const banner = (fixture.nativeElement as HTMLElement).querySelector('[data-testid=relationships-conflict]');
    expect(banner).not.toBeNull();
    expect(banner!.textContent).not.toContain('abwab.');
  });

  it('the endpoint picker offers the other doors by NAME and never the routed door itself, so a self-link cannot be submitted', async () => {
    const { fixture } = await createPage();

    const options = [...selectByTestId(fixture, 'relationship-endpoint').options].map((option) => option.textContent?.trim());
    expect(options).toContain(OTHER_NAME);
    expect(options).not.toContain(ROUTED_NAME);
  });

  it('every wire relationship type carries an Arabic label, so a new type cannot ship unlabelled', () => {
    expect(Object.keys(RELATIONSHIP_TYPE_LABELS).map(Number).sort()).toEqual([...RELATIONSHIP_TYPES].sort());
    expect(DIRECTIONAL_RELATIONSHIP_TYPE).toBe(RELATIONSHIP_TYPE_BROADER_NARROWER);
  });

  it('every wire RelationshipType is offered by the form with its Arabic label', async () => {
    const { fixture } = await createPage();

    const offered = [...selectByTestId(fixture, 'relationship-type').options].map((option) => option.textContent?.trim());

    expect(offered).toEqual(RELATIONSHIP_TYPE_OPTIONS.map((type) => RELATIONSHIP_TYPE_LABELS[type]));
  });

  // Pins the one construction-time read of an imported binding left in the component. A class-field
  // initialiser reading an import captures `undefined` under the unit-test builder; this default is
  // read inside a method body instead, and an `undefined` here would otherwise pass silently.
  it('the add form is built with the wire default RelationshipType, not an undefined one', async () => {
    const { fixture } = await createPage();

    expect(fixture.componentInstance['addForm'].getRawValue().relationshipType).toBe(RELATIONSHIP_TYPE_SIMILAR);
    expect(fixture.componentInstance['editForm'].getRawValue().relationshipType).toBe(RELATIONSHIP_TYPE_SIMILAR);
  });

  it('the direction control appears only for the directional type', async () => {
    const { fixture } = await createPage();

    expect((fixture.nativeElement as HTMLElement).querySelector('[data-testid=relationship-direction]')).toBeNull();

    chooseOption(selectByTestId(fixture, 'relationship-type'), 'أعم / أخص');
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('[data-testid=relationship-direction]')).not.toBeNull();
  });
});
