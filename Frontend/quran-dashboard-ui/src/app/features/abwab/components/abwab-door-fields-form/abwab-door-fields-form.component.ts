import { ChangeDetectionStrategy, Component, computed, effect, input, signal, untracked } from '@angular/core';

import { QdChipComponent } from '../../../../shared/ui/chip/chip.component';
import { AbwabAuthoringFields, EMPTY_AUTHORING_FIELDS } from '../../models/abwab-templates.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

/**
 * The four authoring fields shared by a door and a template node — name, description,
 * representative-ayah free text, and alias chips — plus their dirty tracking and inline error
 * surface. Presentational: it injects nothing, so a shell can render it without standing up a
 * write controller.
 *
 * Two shells compose it: `abwab-door-modal` and `abwab-template-node-modal`. The field labels are
 * the door's in both, deliberately — a template node exists to become a door, and the user's
 * locked requirement is the *same* authoring modal, not a parallel vocabulary. The shells carry
 * the framing (title, context line).
 *
 * `testIdPrefix` keeps the door modal's existing ids (`abwab-door-modal-name`, …) byte-identical
 * through the extraction, which is what lets `abwab-door-modal.component.spec.ts` and
 * `abwab-operations.e2e.ts` stay green unchanged.
 *
 * What deliberately does **not** live here: the door modal's `sectionId` defense-in-depth
 * (M10/M33). This form has no concept of a section and must not acquire one.
 */
@Component({
  selector: 'qd-abwab-door-fields-form',
  standalone: true,
  imports: [QdChipComponent],
  templateUrl: './abwab-door-fields-form.component.html',
  styleUrl: './abwab-door-fields-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorFieldsFormComponent {
  readonly value = input<AbwabAuthoringFields>(EMPTY_AUTHORING_FIELDS);
  readonly errorMessage = input<string | null>(null);
  readonly testIdPrefix = input('abwab-door-modal');

  protected readonly name = signal('');
  protected readonly description = signal('');
  protected readonly ayah = signal('');
  protected readonly aliases = signal<readonly string[]>([]);
  protected readonly aliasDraft = signal('');

  private readonly initialSnapshot = signal('');

  readonly current = computed<AbwabAuthoringFields>(() => ({
    name: this.name(),
    description: this.description(),
    representativeAyahText: this.ayah(),
    aliases: this.aliases(),
  }));

  readonly isDirty = computed(() => snapshotKey(this.current()) !== this.initialSnapshot());

  protected get nameLabel(): string { return ABWAB_LABELS.nameFieldLabel; }
  protected get descriptionLabel(): string { return ABWAB_LABELS.descriptionFieldLabel; }
  protected get descriptionPlaceholder(): string { return ABWAB_LABELS.descriptionPlaceholder; }
  protected get ayahLabel(): string { return ABWAB_LABELS.ayahFieldLabel; }
  protected get ayahPlaceholder(): string { return ABWAB_LABELS.ayahPlaceholder; }
  protected get ayahHint(): string { return ABWAB_LABELS.ayahHint; }
  protected get aliasLabel(): string { return ABWAB_LABELS.aliasFieldLabel; }
  protected get aliasPlaceholder(): string { return ABWAB_LABELS.aliasPlaceholder; }

  protected removeAliasLabel(alias: string): string {
    return ABWAB_LABELS.removeAliasAriaLabel(alias);
  }

  constructor() {
    // `untracked` is load-bearing: the reset both writes and reads the field signals, so without
    // it every keystroke would re-trigger it and wipe the user's input back to `value`.
    effect(() => {
      const fields = this.value();
      untracked(() => this.resetFrom(fields));
    });
  }

  private resetFrom(fields: AbwabAuthoringFields): void {
    this.name.set(fields.name);
    this.description.set(fields.description);
    this.ayah.set(fields.representativeAyahText);
    this.aliases.set([...fields.aliases]);
    this.aliasDraft.set('');
    this.initialSnapshot.set(snapshotKey(fields));
  }

  protected onNameInput(event: Event): void {
    this.name.set((event.target as HTMLInputElement).value);
  }
  protected onDescriptionInput(event: Event): void {
    this.description.set((event.target as HTMLTextAreaElement).value);
  }
  protected onAyahInput(event: Event): void {
    this.ayah.set((event.target as HTMLInputElement).value);
  }
  protected onAliasDraftInput(event: Event): void {
    this.aliasDraft.set((event.target as HTMLInputElement).value);
  }

  protected onAliasEnter(event: Event): void {
    event.preventDefault();
    const value = this.aliasDraft().trim();
    if (!value) {
      return;
    }
    this.aliases.update((current) => [...current, value]);
    this.aliasDraft.set('');
  }

  protected removeAliasAt(index: number): void {
    this.aliases.update((current) => current.filter((_, i) => i !== index));
  }
}

function snapshotKey(fields: AbwabAuthoringFields): string {
  return JSON.stringify({
    name: fields.name,
    description: fields.description,
    ayah: fields.representativeAyahText,
    aliases: fields.aliases,
  });
}
