import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CategorySnapshotDto } from '../../../core/api/generated/models';
import { AbwabTemplatesMutationStatus } from '../state/abwab-templates.facade';

export interface TemplateApplyRequest {
  readonly targetCategoryId: string;
  readonly expectedTargetVersion: number;
}

type ApplicationForm = FormGroup<{
  targetCategoryId: FormControl<string>;
}>;

// Pick ONE target category, then confirm explicitly. Presentational: it holds no facade and no
// concurrency expectations — it emits the chosen target and the page turns that into the mutation.
// `mutationStatus` MUST be an APPLY-scoped status, not the page-wide one: the chosen target survives
// a conflict and is cleared only when the application itself succeeds, so a status covering every
// mutation on the page would wipe the operator's selection on an unrelated success.
@Component({
  selector: 'qd-abwab-template-application-panel',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './template-application-panel.component.html',
  styleUrl: './template-application-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TemplateApplicationPanelComponent {
  readonly targetCandidates = input.required<readonly CategorySnapshotDto[]>();
  readonly mutationStatus = input<AbwabTemplatesMutationStatus>('idle');
  readonly conflictMessage = input<string | null>(null);

  readonly apply = output<TemplateApplyRequest>();

  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly confirming = signal(false);

  protected readonly form: ApplicationForm = this.formBuilder.group({
    targetCategoryId: this.formBuilder.control('', [Validators.required]),
  });

  protected readonly isPending = computed(() => this.mutationStatus() === 'pending');

  constructor() {
    effect(() => {
      if (this.mutationStatus() === 'success') {
        this.confirming.set(false);
        this.form.reset();
      }
    });
  }

  protected beginConfirm(): void {
    if (this.form.valid) {
      this.confirming.set(true);
    }
  }

  protected cancelConfirm(): void {
    this.confirming.set(false);
  }

  protected confirmApply(): void {
    if (this.form.invalid) {
      return;
    }

    const { targetCategoryId } = this.form.getRawValue();
    const target = this.targetCandidates().find((category) => category.categoryId === targetCategoryId);
    if (!target) {
      return;
    }

    this.apply.emit({ targetCategoryId, expectedTargetVersion: target.version });
  }
}
