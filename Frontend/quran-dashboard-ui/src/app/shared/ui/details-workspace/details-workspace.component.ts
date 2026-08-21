import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type QdDetailsWorkspaceLayout = 'selection' | 'no-selection';
export type QdDetailsWorkspaceVariant = 'entity' | 'safety' | 'action-rail' | 'study' | 'overlay-body';

let nextWorkspaceId = 0;

@Component({
  selector: 'qd-details-workspace',
  standalone: true,
  templateUrl: './details-workspace.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'qd-details',
    '[class.qd-details--no-selection]': "layout() === 'no-selection'",
  },
})
export class QdDetailsWorkspaceComponent {
  readonly identity = input('');
  readonly layout = input<QdDetailsWorkspaceLayout>('selection');
  readonly variant = input<QdDetailsWorkspaceVariant>('entity');
  readonly noSelectionMessage = input('');
  readonly labelledBy = input<string | null>(null);
  readonly hasHeader = input(true);
  readonly hasTabs = input(false);
  readonly hasFooter = input(false);
  readonly testIdPrefix = input('qd-details');

  readonly showsIdentity = computed(() => this.hasHeader() && this.identity() !== '');

  private readonly instance = nextWorkspaceId++;
  readonly instanceId = `qd-details-${this.instance}`;
  readonly identityId = `${this.instanceId}-identity`;
  readonly statusId = `${this.instanceId}-status`;

  panelId(key: string): string {
    return `${this.instanceId}-panel-${key}`;
  }

  tabId(key: string): string {
    return `${this.instanceId}-tab-${key}`;
  }
}
