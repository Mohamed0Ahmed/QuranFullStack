import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { LinkingAddFeedbackStore } from '../../state/linking-add-feedback.store';

@Component({
  selector: 'qd-linking-add-feedback-toast',
  standalone: true,
  templateUrl: './linking-add-feedback-toast.component.html',
  styleUrl: './linking-add-feedback-toast.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingAddFeedbackToastComponent {
  protected readonly feedback = inject(LinkingAddFeedbackStore).feedback;
}
