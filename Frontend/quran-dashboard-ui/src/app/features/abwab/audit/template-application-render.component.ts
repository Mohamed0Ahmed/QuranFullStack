import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import {
  CreatedCategoryRenderView,
  EMPTY_FIELD_VALUE_LABEL,
  TemplateApplicationRenderPayload,
} from './abwab-audit-render.models';

export interface CreatedCategoryRow {
  readonly view: CreatedCategoryRenderView;
  readonly depth: number;
}

// Presentational only (§6.3): it renders the FROZEN snapshot the application event stored, so a later
// template edit cannot change what this component shows. No fetching, no pagination — that read model
// is `033`'s.
@Component({
  selector: 'qd-abwab-template-application-render',
  standalone: true,
  templateUrl: './template-application-render.component.html',
  styleUrl: './template-application-render.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TemplateApplicationRenderComponent {
  readonly payload = input.required<TemplateApplicationRenderPayload>();

  protected readonly emptyValueLabel = EMPTY_FIELD_VALUE_LABEL;
  protected readonly reviewerLabel = 'غير مطلوب';

  protected readonly createdRows = computed<readonly CreatedCategoryRow[]>(() => flatten(this.payload().createdTree, 0));

  protected readonly totalCreated = computed(() =>
    this.payload().countsByLevel.reduce((total, entry) => total + entry.count, 0),
  );
}

function flatten(views: readonly CreatedCategoryRenderView[], depth: number): CreatedCategoryRow[] {
  return views.flatMap((view) => [{ view, depth }, ...flatten(view.children, depth + 1)]);
}
