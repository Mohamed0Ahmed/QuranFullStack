import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdControlDirective } from '../../../../shared/ui/form-field/control.directive';
import { LINKING_LABELS } from '../../models/linking.labels';
import {
  LINKING_MAX_DESCRIPTIONS_PER_AYAH,
  LINKING_MAX_DESCRIPTION_LENGTH,
} from '../../models/linking-workspace.models';

@Component({
  selector: 'qd-linking-ayah-descriptions',
  standalone: true,
  imports: [QdActionDirective, QdControlDirective],
  templateUrl: './linking-ayah-descriptions.component.html',
  styleUrl: './linking-ayah-descriptions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingAyahDescriptionsComponent {
  readonly verseKey = input.required<string>();
  readonly descriptions = input.required<readonly string[]>();
  readonly disabled = input(false);

  readonly changed = output<readonly string[]>();

  protected readonly labels = LINKING_LABELS;
  protected readonly maxLength = LINKING_MAX_DESCRIPTION_LENGTH;
  protected readonly expanded = signal(false);
  protected readonly draft = signal('');
  protected readonly canAdd = computed(
    () => !this.disabled() && this.descriptions().length < LINKING_MAX_DESCRIPTIONS_PER_AYAH,
  );
  protected readonly canSubmitDraft = computed(() => this.canAdd() && this.isAcceptable(this.draft()));

  protected toggle(): void {
    this.expanded.update((expanded) => !expanded);
  }

  protected setDraft(event: Event): void {
    this.draft.set((event.target as HTMLTextAreaElement).value);
  }

  protected add(): void {
    const body = this.draft().trim();
    if (!this.canSubmitDraft()) {
      return;
    }
    this.changed.emit([...this.descriptions(), body]);
    this.draft.set('');
  }

  protected commit(index: number, event: Event): void {
    const element = event.target as HTMLTextAreaElement;
    const current = this.descriptions();
    const body = element.value.trim();
    if (body === current[index]) {
      return;
    }
    if (this.disabled() || !this.isAcceptable(body)) {
      element.value = current[index] ?? '';
      return;
    }
    this.changed.emit(current.map((existing, position) => (position === index ? body : existing)));
  }

  protected remove(index: number): void {
    this.changed.emit(this.descriptions().filter((_, position) => position !== index));
  }

  protected move(index: number, offset: number): void {
    const current = this.descriptions();
    const target = index + offset;
    if (target < 0 || target >= current.length) {
      return;
    }
    const reordered = [...current];
    reordered[index] = current[target];
    reordered[target] = current[index];
    this.changed.emit(reordered);
  }

  private isAcceptable(body: string): boolean {
    const trimmed = body.trim();
    return trimmed !== '' && trimmed.length <= LINKING_MAX_DESCRIPTION_LENGTH;
  }
}
