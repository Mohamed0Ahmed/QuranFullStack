import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingSourceDescriptor } from '../../models/linking-source.models';
import { LinkingAccessService } from '../../state/linking-access.service';
import { LinkingWorkspaceStore } from '../../state/linking-workspace.store';
import { linkingSourcePresentation } from '../../utils/linking-source-presentation';

@Component({
  selector: 'qd-linking-quick-add',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './linking-quick-add.component.html',
  styleUrl: './linking-quick-add.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[style.display]': "canUseLinking() ? null : 'none'",
  },
})
export class LinkingQuickAddComponent {
  private readonly workspace = inject(LinkingWorkspaceStore);

  readonly source = input.required<LinkingSourceDescriptor>();
  readonly testId = input<string | null>(null);

  protected readonly canUseLinking = inject(LinkingAccessService).canUseLinking;
  protected readonly actionLabel = computed(() => {
    const source = this.source();
    return `${LINKING_LABELS.addToWorkspace}: ${linkingSourcePresentation(source)}، ${source.label}`;
  });

  protected add(event: MouseEvent): void {
    event.stopPropagation();
    this.workspace.addSource(this.source());
  }
}
