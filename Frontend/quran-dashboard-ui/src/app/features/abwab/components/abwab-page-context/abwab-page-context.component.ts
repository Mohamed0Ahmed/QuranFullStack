import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { ExplorerResultCountComponent } from '../../../../shared/ui/result-count/explorer-result-count.component';

@Component({
  selector: 'qd-abwab-page-context',
  standalone: true,
  imports: [RouterLink, QdActionDirective, ExplorerResultCountComponent],
  templateUrl: './abwab-page-context.component.html',
  styleUrl: './abwab-page-context.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabPageContextComponent {
  readonly pageTitle = input.required<string>();
  readonly pageSubtitle = input.required<string>();
  readonly archiveActive = input(false);
  readonly archiveButtonLabel = input.required<string>();
  readonly manageSectionsLabel = input.required<string>();
  readonly templatesLabel = input.required<string>();
  readonly templatesRoutePath = input.required<string>();
  readonly addRootLabel = input.required<string>();
  readonly statAllDoorsLabel = input.required<string>();
  readonly statOpenScopeLabel = input.required<string>();
  readonly totalLiveDoorsCount = input(0);
  readonly openScopeDoorsCount = input(0);
  readonly totalRootCount = input(0);
  readonly sectionCount = input(0);
  readonly loading = input(false);
  readonly hasError = input(false);
  readonly canManageSections = input(false);
  readonly canCreateDoor = input(false);

  readonly archiveToggleRequested = output<void>();
  readonly sectionsRequested = output<void>();
  readonly createRootRequested = output<void>();

  private readonly fallbackFocus = viewChild<ElementRef<HTMLButtonElement>>('fallbackFocus');

  focusFallback(): void {
    this.fallbackFocus()?.nativeElement.focus();
  }
}
