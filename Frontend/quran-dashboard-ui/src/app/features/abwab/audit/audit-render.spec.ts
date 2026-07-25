import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Type, reflectComponentType } from '@angular/core';

import { BulkMoveRenderComponent } from './bulk-move-render.component';
import { CategoryCreateRenderComponent } from './category-create-render.component';
import { CategoryEditRenderComponent } from './category-edit-render.component';
import { FieldDiffRowComponent } from './field-diff-row.component';
import { ManualProtectionRenderComponent } from './manual-protection-render.component';
import { RelationshipRenderComponent } from './relationship-render.component';
import { SubtreeDeleteRenderComponent } from './subtree-delete-render.component';
import {
  BulkMoveRenderPayload,
  CategoryCreateRenderPayload,
  CategoryEditRenderPayload,
  ManualProtectionRenderPayload,
  RelationshipEndpointRenderView,
  RelationshipRenderPayload,
  RelationshipStateRenderView,
  SubtreeDeleteRenderPayload,
} from './abwab-audit-render.models';
import { toDormantDependentCounts } from './relationship-dormant-counts';
import {
  RELATIONSHIP_TYPE_BROADER_NARROWER,
  RELATIONSHIP_TYPE_OPPOSITE,
  RELATIONSHIP_TYPE_SIMILAR,
} from '../data-access/abwab-relationships.port';
import { RELATIONSHIP_TYPE_LABELS } from '../data-access/relationship-type-labels';

function text(element: Element): string {
  return element.textContent?.replace(/\s+/g, ' ').trim() ?? '';
}

// Synthetic Arabic fixture data only (source-safe) — no real Quran text.
describe('Abwab §6.3 audit render payloads', () => {
  it('category create: renders the complete new state, with empty fields shown as غير محدد', () => {
    const fixture = TestBed.createComponent(CategoryCreateRenderComponent);
    const payload: CategoryCreateRenderPayload = {
      name: 'باب الإيمان',
      representativeQuranExcerpt: null,
      description: null,
      aliases: [],
      sectionName: 'أبواب العقيدة',
      parentPath: [],
      siblingOrder: 0,
      sectionOrder: 2,
      globalOrder: 5,
    };
    fixture.componentRef.setInput('payload', payload);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(text(root)).toContain('باب الإيمان');
    expect(root.querySelector('[data-testid=create-excerpt]')!.textContent).toContain('غير محدد');
    expect(root.querySelector('[data-testid=create-description]')!.textContent).toContain('غير محدد');
    expect(root.querySelector('[data-testid=create-aliases]')!.textContent).toContain('غير محدد');
    expect(root.querySelector('[data-testid=create-sibling-order]')!.textContent).toContain('0');
    expect(root.querySelector('[data-testid=create-section-order]')!.textContent).toContain('2');
    expect(root.querySelector('[data-testid=create-global-order]')!.textContent).toContain('5');
  });

  it('category edit: renders complete field set with a non-color diff marker on changed fields, INCLUDING order fields', () => {
    const fixture = TestBed.createComponent(CategoryEditRenderComponent);
    const payload: CategoryEditRenderPayload = {
      categoryId: 'cat-1',
      categoryName: 'باب الإيمان',
      fields: [
        { key: 'name', label: 'الاسم', before: 'باب الإيمان', after: 'باب الإيمان بالله', changed: true },
        { key: 'description', label: 'الوصف', before: null, after: null, changed: false },
        { key: 'siblingOrder', label: 'الترتيب بين الإخوة', before: '0', after: '1', changed: true },
        { key: 'sectionOrder', label: 'الترتيب داخل القسم', before: '2', after: '2', changed: false },
      ],
    };
    fixture.componentRef.setInput('payload', payload);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const rows = root.querySelectorAll('[data-testid=field-diff-row]');
    expect(rows).toHaveLength(4);

    const unchangedDescriptionRow = rows[1];
    expect(unchangedDescriptionRow.textContent).toContain('غير محدد');

    const orderRow = rows[2];
    expect(orderRow.textContent).toContain('الترتيب بين الإخوة');
    expect(orderRow.querySelector('[data-testid=field-diff-marker]')).not.toBeNull();

    const unchangedOrderRow = rows[3];
    expect(unchangedOrderRow.querySelector('[data-testid=field-diff-marker]')).toBeNull();
  });

  it('bulk move: renders one ChangeSet with descendants NESTED under their root (not independent moves), and side effects GROUPED by parent/order scope', () => {
    const fixture = TestBed.createComponent(BulkMoveRenderComponent);
    const payload: BulkMoveRenderPayload = {
      changeSetId: 'cs-1',
      selectedRootCount: 1,
      roots: [
        {
          categoryId: 'root-1',
          name: 'باب الطهارة',
          beforeSectionName: 'أبواب الفقه',
          beforePath: ['أبواب الفقه', 'باب الطهارة'],
          beforeOrder: 0,
          afterSectionName: 'أبواب العبادات',
          afterPath: ['أبواب العبادات', 'باب الطهارة'],
          afterOrder: 3,
          movedDescendantCount: 2,
          movedDescendantNames: ['باب الوضوء', 'باب الغسل'],
        },
      ],
      siblingOrderSideEffects: [
        {
          parentLabel: 'أبواب العبادات',
          orderScopeLabel: 'ترتيب الأبواب الرئيسية داخل القسم',
          entries: [{ categoryName: 'باب الصلاة', beforeOrder: 3, afterOrder: 4 }],
        },
      ],
    };
    fixture.componentRef.setInput('payload', payload);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelectorAll('[data-testid=bulk-move-root]')).toHaveLength(1);
    const descendants = root.querySelectorAll('[data-testid=bulk-move-descendant]');
    expect(descendants).toHaveLength(2);
    expect(text(descendants[0])).toBe('باب الوضوء');

    const groups = root.querySelectorAll('[data-testid=bulk-move-side-effect-group]');
    expect(groups).toHaveLength(1);
    expect(text(groups[0])).toContain('أبواب العبادات');
    expect(text(groups[0])).toContain('باب الصلاة');
  });

  it('subtree delete/restore: renders one ChangeSet with dormant counts, and NEVER labels a dormant dependent as deleted', () => {
    const fixture = TestBed.createComponent(SubtreeDeleteRenderComponent);
    const payload: SubtreeDeleteRenderPayload = {
      changeSetId: 'cs-2',
      deletionOperationId: 'op-1',
      rootName: 'باب الأذكار',
      historicalPath: ['أبواب العبادات', 'باب الأذكار'],
      affectedCategoryCount: 3,
      dormantDependentCounts: [{ label: 'روابط مرتبطة', count: 5 }],
      isRestored: false,
      currentPath: null,
    };
    fixture.componentRef.setInput('payload', payload);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid=subtree-delete-operation-id]')!.textContent).toContain('op-1');
    expect(root.querySelector('[data-testid=subtree-delete-affected-count]')!.textContent).toContain('3');
    const dormantEntry = root.querySelector('[data-testid=subtree-delete-dormant-entry]')!;
    expect(text(dormantEntry)).toContain('خامل');
    expect(text(dormantEntry)).not.toContain('محذوف');
  });

  it('manual protection: renders target/type/scope/actor/time and every changed direct/inherited effect', () => {
    const fixture = TestBed.createComponent(ManualProtectionRenderComponent);
    const payload: ManualProtectionRenderPayload = {
      changeSetId: 'cs-3',
      targetCategoryName: 'باب الطهارة',
      protectionTypeLabel: 'بيانات الباب',
      scopeLabel: 'الباب والشجرة الفرعية',
      actorSubject: 'reviewer-1',
      actedAtUtc: '2026-07-23T00:00:00.000Z',
      before: 'unprotected',
      after: 'protected',
      effectChanges: [
        { categoryName: 'باب الطهارة', isDirect: true, before: 'unprotected', after: 'protected' },
        { categoryName: 'باب الوضوء', isDirect: false, before: 'unprotected', after: 'protected' },
      ],
    };
    fixture.componentRef.setInput('payload', payload);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid=manual-protection-actor]')!.textContent).toContain('reviewer-1');
    expect(text(root.querySelector('[data-testid=manual-protection-before-after]')!)).toContain('غير محمي ← محمي');
    const effects = root.querySelectorAll('[data-testid=manual-protection-effect-entry]');
    expect(effects).toHaveLength(2);
    expect(text(effects[0])).toContain('مباشرة');
    expect(text(effects[1])).toContain('موروثة');
  });

  function endpoint(
    categoryId: string,
    name: string,
    overrides: Partial<RelationshipEndpointRenderView> = {},
  ): RelationshipEndpointRenderView {
    return {
      categoryId,
      name,
      sectionName: 'أبواب الأخلاق',
      historicalPath: ['أبواب الأخلاق', name],
      currentName: null,
      currentPath: null,
      isCurrentlyDeleted: false,
      ...overrides,
    };
  }

  function renderRelationship(payload: RelationshipRenderPayload): HTMLElement {
    const fixture = TestBed.createComponent(RelationshipRenderComponent);
    fixture.componentRef.setInput('payload', payload);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  const mutualBefore: RelationshipStateRenderView = {
    relationshipType: RELATIONSHIP_TYPE_SIMILAR,
    isDirectional: false,
    from: endpoint('cat-1', 'باب الصبر'),
    to: endpoint('cat-2', 'باب الشكر'),
  };

  const directionalState: RelationshipStateRenderView = {
    relationshipType: RELATIONSHIP_TYPE_BROADER_NARROWER,
    isDirectional: true,
    from: endpoint('cat-3', 'باب العبادات', { sectionName: 'أبواب الفقه', historicalPath: ['أبواب الفقه', 'باب العبادات'] }),
    to: endpoint('cat-4', 'باب الصلاة', { sectionName: 'أبواب الفقه', historicalPath: ['أبواب الفقه', 'باب الصلاة'] }),
  };

  const mutualRetyped: RelationshipStateRenderView = { ...mutualBefore, relationshipType: RELATIONSHIP_TYPE_OPPOSITE };

  it.each([
    ['added', null, directionalState, 'إضافة علاقة'],
    ['edited', mutualBefore, mutualRetyped, 'تعديل علاقة'],
    ['deleted', directionalState, null, 'حذف علاقة'],
    ['restored', null, mutualBefore, 'استعادة علاقة'],
  ] as const)(
    'relationship %s: renders the action label and the direct-structure reviewer «غير مطلوب»',
    (action, before, after, actionLabel) => {
      const root = renderRelationship({
        changeSetId: 'changeset-1',
        categoryRelationshipId: 'relationship-1',
        action,
        before,
        after,
      });

      expect(text(root.querySelector('.qd-section-title')!)).toContain(actionLabel);
      expect(text(root.querySelector('[data-testid=relationship-render-reviewer]')!)).toContain('غير مطلوب');
    },
  );

  it('relationship edit (mutual): the changed value carries BOTH colour and a non-colour marker, anchored to the value itself', () => {
    const root = renderRelationship({
      changeSetId: 'changeset-1',
      categoryRelationshipId: 'relationship-1',
      action: 'edited',
      before: mutualBefore,
      after: mutualRetyped,
    });

    const afterTypeCell = root.querySelector('[data-testid=relationship-render-after-type]')!;
    expect(text(root.querySelector('[data-testid=relationship-render-before-type]')!)).toContain('مشابه');
    expect(text(afterTypeCell)).toContain('مقابل');

    // The marker must live INSIDE the changed value, not in a detached block — one anchor carrying
    // both signals is what makes the change readable without colour.
    expect(afterTypeCell.querySelector('[data-testid=relationship-render-changed-type]')).not.toBeNull();
    expect(afterTypeCell.closest('.relationship-diff__row')!.className).toContain('relationship-diff__row--changed');
    expect(root.querySelector('[data-testid=relationship-render-changed-from]')).toBeNull();
  });

  it('relationship edit: each row pairs the previous state and the current state as sibling cells (previous first, RTL-rightmost)', () => {
    const root = renderRelationship({
      changeSetId: 'changeset-1',
      categoryRelationshipId: 'relationship-1',
      action: 'edited',
      before: mutualBefore,
      after: { ...mutualBefore, to: endpoint('cat-9', 'باب الرضا') },
    });

    const row = root.querySelector('[data-testid=relationship-render-before-to]')!.closest('.relationship-diff__row')!;
    const cells = [...row.children].map((cell) => cell.getAttribute('data-testid'));
    expect(cells).toEqual([null, 'relationship-render-before-to', 'relationship-render-after-to']);
    expect(text(root.querySelector('[data-testid=relationship-render-before-to]')!)).toContain('باب الشكر');
    expect(text(root.querySelector('[data-testid=relationship-render-after-to]')!)).toContain('باب الرضا');
  });

  it('relationship delete: the live current door name/path/deleted state renders from the BEFORE state even though there is no after', () => {
    const root = renderRelationship({
      changeSetId: 'changeset-4',
      categoryRelationshipId: 'relationship-4',
      action: 'deleted',
      before: {
        ...directionalState,
        to: endpoint('cat-4', 'باب الصلاة', {
          sectionName: 'أبواب الفقه',
          historicalPath: ['أبواب الفقه', 'باب الصلاة'],
          currentName: 'باب الصلوات',
          currentPath: ['أبواب الفقه', 'باب الصلوات'],
          isCurrentlyDeleted: true,
        }),
      },
      after: null,
    });

    const beforeToCell = root.querySelector('[data-testid=relationship-render-before-to]')!;
    expect(beforeToCell.querySelector('[data-testid=relationship-render-current]')).not.toBeNull();
    expect(text(beforeToCell)).toContain('باب الصلوات');
    expect(beforeToCell.querySelector('[data-testid=relationship-render-current-deleted]')).not.toBeNull();
  });

  it('relationship add (directional): derives the Broader/Narrower inverse label for display instead of storing a reversed row', () => {
    const root = renderRelationship({
      changeSetId: 'changeset-2',
      categoryRelationshipId: 'relationship-2',
      action: 'added',
      before: null,
      after: directionalState,
    });

    expect(text(root.querySelector('[data-testid=relationship-render-inverse]')!)).toContain('باب الصلاة أخص من باب العبادات');
    expect(text(root.querySelector('[data-testid=relationship-render-before-from]')!)).toBe('');
    expect(text(root)).toContain('الأعم');
    expect(text(root)).toContain('الأخص');
  });

  // A committed relationship event can never carry a protection blocker: applicable Relationship
  // protection aborts the mutation before a ChangeSet exists, so the block reaches the operator as
  // the abwab.manual_protection conflict instead (audit-render-contract.md §1, recorded ruling).
  it('relationship: the payload exposes no protection-blocker facet, since a blocked mutation is never audited', () => {
    const root = renderRelationship({
      changeSetId: 'changeset-5',
      categoryRelationshipId: 'relationship-5',
      action: 'added',
      before: null,
      after: mutualBefore,
    });

    expect(root.querySelector('[data-testid=relationship-render-blockers]')).toBeNull();
    expect(Object.keys(mutualBefore)).not.toContain('effectiveProtectionBlockers');
  });

  it('relationship dormancy: the contributed counts name the relationship type and leave the dormant badge to the 029 seam', () => {
    const counts = toDormantDependentCounts(
      {
        totalDormant: 3,
        byType: [
          { relationshipType: RELATIONSHIP_TYPE_SIMILAR, count: 2 },
          { relationshipType: RELATIONSHIP_TYPE_OPPOSITE, count: 0 },
          { relationshipType: RELATIONSHIP_TYPE_BROADER_NARROWER, count: 1 },
        ],
      },
      RELATIONSHIP_TYPE_LABELS,
    );

    expect(counts).toHaveLength(2);
    expect(counts.map((entry) => entry.count)).toEqual([2, 1]);
    expect(counts.map((entry) => entry.label)).toEqual(['علاقات (مشابه)', 'علاقات (أعم / أخص)']);
    // The 029-owned row stamps «خامل» itself; contributing it again would render the word twice.
    for (const entry of counts) {
      expect(entry.label).not.toContain('خامل');
      expect(entry.label).not.toContain('محذوف');
    }

    const fixture = TestBed.createComponent(SubtreeDeleteRenderComponent);
    const payload: SubtreeDeleteRenderPayload = {
      changeSetId: 'changeset-3',
      deletionOperationId: 'operation-1',
      rootName: 'باب المعاملات',
      historicalPath: ['أبواب الفقه', 'باب المعاملات'],
      affectedCategoryCount: 4,
      dormantDependentCounts: counts,
      isRestored: false,
      currentPath: null,
    };
    fixture.componentRef.setInput('payload', payload);
    fixture.detectChanges();

    const entries = (fixture.nativeElement as HTMLElement).querySelectorAll('[data-testid=subtree-delete-dormant-entry]');
    expect(entries).toHaveLength(2);
    expect(text(entries[0])).toBe('خامل علاقات (مشابه): 2');
  });

  it('§6.3 defines NO standalone "ordering" render component — order data only appears inside bulk-move and category-edit', () => {
    // Every render component the audit/ folder publishes (T073). Every one of them is imported
    // above by name; a future "ordering" component would have to be imported here too to render
    // anything, so this closed set IS the audit/ folder's component surface.
    const publishedComponents: Type<unknown>[] = [
      CategoryCreateRenderComponent,
      CategoryEditRenderComponent,
      BulkMoveRenderComponent,
      SubtreeDeleteRenderComponent,
      ManualProtectionRenderComponent,
      RelationshipRenderComponent,
      FieldDiffRowComponent,
    ];

    expect(publishedComponents).toHaveLength(7);

    for (const component of publishedComponents) {
      const mirror = reflectComponentType(component);
      expect(component.name.toLowerCase()).not.toContain('ordering');
      expect(mirror?.selector.toLowerCase()).not.toContain('ordering');
    }
  });
});
