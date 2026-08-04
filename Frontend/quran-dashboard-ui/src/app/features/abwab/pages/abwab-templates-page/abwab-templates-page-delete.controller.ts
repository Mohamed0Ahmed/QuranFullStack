import { Injectable, inject, signal } from '@angular/core';

import { AbwabTemplatesFacade } from '../../state/abwab-templates.facade';
import { AbwabTemplatesController } from '../../state/abwab-templates.controller';

@Injectable()
export class AbwabTemplatesPageDeleteController {
  private readonly facade = inject(AbwabTemplatesFacade);
  private readonly templates = inject(AbwabTemplatesController);

  readonly confirming = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  request(): void {
    this.error.set(null);
    this.busy.set(false);
    this.confirming.set(true);
  }

  confirm(onDeleted: () => void): void {
    const template = this.facade.selectedTemplate();
    if (template === null || this.busy()) {
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.templates.deleteTemplate(template.id).subscribe((outcome) => {
      this.busy.set(false);
      if (outcome.kind !== 'success') {
        this.error.set(outcome.message);
        return;
      }
      this.confirming.set(false);
      this.facade.clearSelection();
      onDeleted();
    });
  }

  cancel(onDismissed: () => void): void {
    if (this.busy()) {
      return;
    }
    this.confirming.set(false);
    this.error.set(null);
    onDismissed();
  }
}
