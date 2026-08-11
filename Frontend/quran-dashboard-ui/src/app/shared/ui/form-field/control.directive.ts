import { Directive, computed, inject, input } from '@angular/core';

import { QdFormFieldComponent } from './form-field.component';

@Directive({
  selector: 'input[qdControl], select[qdControl], textarea[qdControl]',
  standalone: true,
  host: {
    class: 'qd-control',
    '[attr.id]': 'controlId()',
    '[attr.aria-describedby]': 'describedBy()',
    '[attr.aria-invalid]': "invalid() ? 'true' : null",
  },
})
export class QdControlDirective {
  private readonly field = inject(QdFormFieldComponent, { optional: true });

  readonly invalidOverride = input<boolean | undefined>(undefined, { alias: 'invalid' });

  protected readonly controlId = computed(() => this.field?.controlId() ?? null);
  protected readonly describedBy = computed(() => this.field?.describedBy() ?? null);
  protected readonly invalid = computed(
    () => this.invalidOverride() ?? this.field?.invalid() ?? false,
  );
}
