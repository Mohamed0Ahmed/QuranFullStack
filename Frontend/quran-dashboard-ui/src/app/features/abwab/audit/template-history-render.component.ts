import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import {
  EMPTY_FIELD_VALUE_LABEL,
  FieldDiff,
  TemplateFieldChangeRenderView,
  TemplateHistoryAction,
  TemplateHistoryRenderPayload,
  TemplateNodeRenderView,
} from './abwab-audit-render.models';
import { FieldDiffRowComponent } from './field-diff-row.component';

const FIELD_LABELS: Record<string, string> = {
  Name: 'الاسم',
  Description: 'الوصف',
  RepresentativeQuranExcerpt: 'المقطع التمثيلي',
  ParentTemplateNodeId: 'العقدة الأصل',
  SiblingOrder: 'الترتيب',
  Aliases: 'المرادفات',
  IsDeleted: 'الحذف',
};

const TEMPLATE_SCOPE_LABEL = 'القالب';

const ACTION_LABELS: Record<TemplateHistoryAction, string> = {
  created: 'إنشاء قالب',
  edited: 'تعديل قالب',
  deleted: 'حذف قالب',
  restored: 'استعادة قالب',
  node_added: 'إضافة عقدة',
  node_edited: 'تعديل عقدة',
  node_reparented: 'نقل عقدة',
  nodes_reordered: 'إعادة ترتيب العقد',
  node_removed: 'إزالة عقدة',
  alias_added: 'إضافة مرادف',
  alias_edited: 'تعديل مرادف',
  alias_removed: 'إزالة مرادف',
  alias_restored: 'استعادة مرادف',
};

// The SEPARATE template-history view (§6.3) — deliberately not part of the main product-audit render
// set, because template CRUD produces no main-log row. Presentational only: no fetching.
@Component({
  selector: 'qd-abwab-template-history-render',
  standalone: true,
  imports: [FieldDiffRowComponent],
  templateUrl: './template-history-render.component.html',
  styleUrl: './template-history-render.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TemplateHistoryRenderComponent {
  readonly payload = input.required<TemplateHistoryRenderPayload>();

  protected readonly emptyValueLabel = EMPTY_FIELD_VALUE_LABEL;
  protected readonly actionLabels = ACTION_LABELS;

  protected readonly beforeNodes = computed<readonly TemplateNodeRenderView[]>(() => this.payload().before?.nodes ?? []);
  protected readonly afterNodes = computed<readonly TemplateNodeRenderView[]>(() => this.payload().after?.nodes ?? []);

  protected readonly changedNodeIds = computed(
    () => new Set(this.payload().changedNodes.map((node) => node.templateNodeId)),
  );

  protected readonly changedFieldDiffs = computed<readonly FieldDiff[]>(() =>
    this.payload().changedFields.map((change) => this.toDiff(change)),
  );

  protected isChanged(node: TemplateNodeRenderView): boolean {
    return this.changedNodeIds().has(node.templateNodeId);
  }

  private toDiff(change: TemplateFieldChangeRenderView): FieldDiff {
    const fieldLabel = FIELD_LABELS[change.field] ?? change.field;
    const scopeLabel = change.templateNodeId === null ? TEMPLATE_SCOPE_LABEL : this.nodeNameOf(change.templateNodeId);

    return {
      key: `${change.templateNodeId ?? 'template'}:${change.field}`,
      label: `${scopeLabel} — ${fieldLabel}`,
      before: change.before,
      after: change.after,
      changed: true,
    };
  }

  private nodeNameOf(templateNodeId: string): string {
    const node =
      this.afterNodes().find((entry) => entry.templateNodeId === templateNodeId) ??
      this.beforeNodes().find((entry) => entry.templateNodeId === templateNodeId);

    return node?.name ?? EMPTY_FIELD_VALUE_LABEL;
  }
}
