import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { QdEmptyStateComponent } from '../../../../../shared/ui/empty-state/empty-state.component';

@Component({
  selector: 'qd-phrase-search-deferred-page',
  standalone: true,
  imports: [QdEmptyStateComponent],
  templateUrl: './phrase-search-deferred-page.component.html',
  styleUrl: './phrase-search-deferred-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseSearchDeferredPageComponent {
  private readonly route = inject(ActivatedRoute);

  protected readonly title = this.route.snapshot.data['titleAr'] as string;
  protected readonly message = this.route.snapshot.data['messageAr'] as string;
}
