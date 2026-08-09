import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

let nextFormFieldId = 0;

@Component({
  selector: 'qd-form-field',
  standalone: true,
  templateUrl: './form-field.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'qd-field',
  },
})
export class QdFormFieldComponent {
  readonly label = input.required<string>();
  readonly helper = input<string | null>(null);
  readonly error = input<string | null>(null);
  readonly required = input(false);
  readonly requiredLabel = input('مطلوب');

  private readonly instanceId = `qd-field-${(nextFormFieldId += 1)}`;

  readonly controlId = computed(() => `${this.instanceId}-control`);
  readonly labelId = computed(() => `${this.instanceId}-label`);
  readonly helperId = computed(() => `${this.instanceId}-helper`);
  readonly errorId = computed(() => `${this.instanceId}-error`);

  readonly invalid = computed(() => this.error() !== null && this.error() !== '');

  readonly describedBy = computed(() => {
    const ids: string[] = [];
    if (this.helper()) {
      ids.push(this.helperId());
    }
    if (this.invalid()) {
      ids.push(this.errorId());
    }
    return ids.length > 0 ? ids.join(' ') : null;
  });
}
