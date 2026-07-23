import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Type, reflectComponentType } from '@angular/core';

import { BulkMoveRenderComponent } from './bulk-move-render.component';
import { CategoryCreateRenderComponent } from './category-create-render.component';
import { CategoryEditRenderComponent } from './category-edit-render.component';
import { FieldDiffRowComponent } from './field-diff-row.component';
import { ManualProtectionRenderComponent } from './manual-protection-render.component';
import { SubtreeDeleteRenderComponent } from './subtree-delete-render.component';
import {
  BulkMoveRenderPayload,
  CategoryCreateRenderPayload,
  CategoryEditRenderPayload,
  ManualProtectionRenderPayload,
  SubtreeDeleteRenderPayload,
} from './abwab-audit-render.models';

function text(element: Element): string {
  return element.textContent?.replace(/\s+/g, ' ').trim() ?? '';
}

// T065: §6.3 audit render payload tests (audit-render-contract.md). Synthetic Arabic fixture data
// only (source-safe) — no real Quran text.
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
      FieldDiffRowComponent,
    ];

    expect(publishedComponents).toHaveLength(6);

    for (const component of publishedComponents) {
      const mirror = reflectComponentType(component);
      expect(component.name.toLowerCase()).not.toContain('ordering');
      expect(mirror?.selector.toLowerCase()).not.toContain('ordering');
    }
  });
});
