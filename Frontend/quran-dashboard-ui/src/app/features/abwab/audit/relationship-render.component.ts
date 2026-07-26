import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { RELATIONSHIP_TYPE_LABELS, relationshipEndpointLabels } from '../data-access/relationship-type-labels';
import {
  RelationshipAction,
  RelationshipEndpointRenderView,
  RelationshipRenderPayload,
  RelationshipStateRenderView,
} from './abwab-audit-render.models';

const ACTION_LABELS: Record<RelationshipAction, string> = {
  added: 'إضافة علاقة',
  edited: 'تعديل علاقة',
  deleted: 'حذف علاقة',
  restored: 'استعادة علاقة',
};

const TYPE_ROW_LABEL = 'النوع';

export interface RelationshipDiffRow {
  readonly key: 'type' | 'from' | 'to';
  readonly label: string;
  readonly before: RelationshipEndpointRenderView | null;
  readonly after: RelationshipEndpointRenderView | null;
  readonly beforeText: string | null;
  readonly afterText: string | null;
  readonly changed: boolean;
}

// The payload carries the wire RelationshipType, never Arabic text: both the type label and the
// Broader/Narrower inverse label are DERIVED here for display from the one label source, so a caller
// cannot introduce a second wording and §7.3 still stores one row per relationship, never a reversed
// second one. Reviewer is «غير مطلوب» because a relationship mutation is a direct audited structure
// action.
@Component({
  selector: 'qd-abwab-relationship-render',
  standalone: true,
  imports: [NgTemplateOutlet],
  templateUrl: './relationship-render.component.html',
  styleUrl: './relationship-render.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RelationshipRenderComponent {
  readonly payload = input.required<RelationshipRenderPayload>();

  protected readonly actionLabels = ACTION_LABELS;
  protected readonly reviewerLabel = 'غير مطلوب';

  protected readonly hasDiff = computed(() => this.payload().before !== null && this.payload().after !== null);

  // The state that describes the relationship as it stands after the operation; a delete has only a
  // before state, so shape-derived labels fall back to it.
  protected readonly resultState = computed<RelationshipStateRenderView | null>(
    () => this.payload().after ?? this.payload().before,
  );

  protected readonly inverseLabel = computed(() => {
    const state = this.resultState();
    return state?.isDirectional ? `${state.to.name} أخص من ${state.from.name}` : null;
  });

  protected readonly rows = computed<readonly RelationshipDiffRow[]>(() => {
    const { before, after } = this.payload();
    const state = this.resultState();

    const typeRow: RelationshipDiffRow = {
      key: 'type',
      label: TYPE_ROW_LABEL,
      before: null,
      after: null,
      beforeText: before ? RELATIONSHIP_TYPE_LABELS[before.relationshipType] : null,
      afterText: after ? RELATIONSHIP_TYPE_LABELS[after.relationshipType] : null,
      changed: this.isChanged('type'),
    };

    const endpointRows = (['from', 'to'] as const).map<RelationshipDiffRow>((position) => ({
      key: position,
      label: state ? this.directionLabel(state, position) : position,
      before: before ? before[position] : null,
      after: after ? after[position] : null,
      beforeText: null,
      afterText: null,
      changed: this.isChanged(position),
    }));

    return [typeRow, ...endpointRows];
  });

  protected directionLabel(state: RelationshipStateRenderView, position: 'from' | 'to'): string {
    return relationshipEndpointLabels(state.isDirectional)[position];
  }

  private isChanged(field: 'type' | 'from' | 'to'): boolean {
    const { before, after } = this.payload();
    if (!before || !after) {
      return false;
    }
    if (field === 'type') {
      return before.relationshipType !== after.relationshipType || before.isDirectional !== after.isDirectional;
    }
    return before[field].categoryId !== after[field].categoryId;
  }
}
