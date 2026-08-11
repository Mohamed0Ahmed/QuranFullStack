import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Data } from '@angular/router';

import { QdEmptyStateComponent } from '../empty-state/empty-state.component';

export const PLACEHOLDER_MESSAGE = 'سيتم ربط هذا القسم ضمن خطة الميزات التالية.';

@Component({
  selector: 'qd-placeholder-page',
  standalone: true,
  imports: [QdEmptyStateComponent],
  templateUrl: './placeholder-page.component.html',
  styleUrls: ['./placeholder-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlaceholderPageComponent {
  private readonly routeData = toSignal(inject(ActivatedRoute).data, {
    initialValue: {} as Data,
  });

  protected readonly message = PLACEHOLDER_MESSAGE;
  protected readonly titleAr = computed(() => (this.routeData()['titleAr'] as string) ?? '');
}
