import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  effect,
  ElementRef,
  inject,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { ExplorerPanelSkeletonComponent } from '../../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdEmptyStateComponent } from '../../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../../shared/ui/error-state/error-state.component';
import { QdNoticeComponent } from '../../../../../shared/ui/notice/notice.component';
import { QdRefreshingIndicatorComponent } from '../../../../../shared/ui/refreshing-indicator/refreshing-indicator.component';
import { PhraseContextExplorerComponent } from '../../components/phrase-context-explorer/phrase-context-explorer.component';
import { PhraseContextOccurrenceListComponent } from '../../components/phrase-context-occurrence-list/phrase-context-occurrence-list.component';
import { PhraseQueryResolutionComponent } from '../../components/phrase-query-resolution/phrase-query-resolution.component';
import { PhraseResolutionCandidateDto } from '../../../../../core/api/generated/models/phrase-resolution-candidate-dto';
import { PhraseTextMode, isPhraseTextMode } from '../../models/phrase-repetitions.models';
import { PhraseContextFacade } from '../../state/phrase-context.facade';

@Component({
  selector: 'qd-phrase-context-page',
  standalone: true,
  imports: [
    ExplorerPanelSkeletonComponent,
    PhraseContextExplorerComponent,
    PhraseContextOccurrenceListComponent,
    PhraseQueryResolutionComponent,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdNoticeComponent,
    QdRefreshingIndicatorComponent,
  ],
  templateUrl: './phrase-context-page.component.html',
  styleUrl: './phrase-context-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseContextPageComponent implements OnInit, OnDestroy {
  protected readonly facade = inject(PhraseContextFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private focusTimer?: ReturnType<typeof setTimeout>;

  protected readonly state = this.facade.state;
  protected readonly availableModes = computed<readonly PhraseTextMode[]>(() =>
    (this.state().capabilities?.modes ?? []).map((item) => item.mode).filter(isPhraseTextMode),
  );
  protected readonly workspaceBusy = computed(() => {
    const state = this.state();
    return (
      state.branchesStatus === 'loading' ||
      state.branchesStatus === 'refreshing' ||
      state.groupsStatus === 'loading' ||
      state.groupsStatus === 'refreshing'
    );
  });
  protected readonly countAnnouncement = computed(() => {
    const state = this.state();
    if (!state.branches || (state.branchesStatus !== 'success' && state.branchesStatus !== 'refreshing')) {
      return '';
    }
    return `تم تحديث السياق، ${state.occurrencesTotalCount} موضعًا معروضًا في جدول الآيات`;
  });

  constructor() {
    effect(() => {
      const pending = this.state().focusTarget;
      if (!pending || this.workspaceBusy()) {
        return;
      }
      this.clearFocusTimer();
      this.focusTimer = setTimeout(() => this.restoreFocus(pending, 0), 350);
    });
  }

  ngOnInit(): void {
    this.facade.bindToRoute(this.route);
  }

  ngOnDestroy(): void {
    this.clearFocusTimer();
    this.facade.unbindFromRoute();
  }

  protected selectCandidate(candidate: PhraseResolutionCandidateDto): void {
    this.facade.selectCandidate(candidate);
  }

  protected selectBranch(side: 'previous' | 'following', selectionRef: string): void {
    if (side === 'previous') {
      this.facade.selectPrevious(selectionRef);
    } else {
      this.facade.selectFollowing(selectionRef);
    }
  }

  protected reverseBranch(side: 'previous' | 'following'): void {
    if (side === 'previous') {
      this.facade.reversePrevious();
    } else {
      this.facade.reverseFollowing();
    }
  }

  protected loadMoreBranches(side: 'previous' | 'following'): void {
    if (side === 'previous') {
      this.facade.loadMorePrevious();
    } else {
      this.facade.loadMoreFollowing();
    }
  }

  private restoreFocus(pending: string, attempt: number): void {
    if (this.state().focusTarget !== pending || this.workspaceBusy()) {
      return;
    }
    const side = pending.startsWith('previous') ? 'previous' : 'following';
    const scope = `.context-web--${side}`;
    const primarySelector = pending.endsWith('more')
      ? `${scope} .context-web__more, ${scope} .context-node:last-of-type`
      : `${scope} .context-node`;
    const target =
      this.host.nativeElement.querySelector<HTMLButtonElement>(primarySelector) ??
      this.host.nativeElement.querySelector<HTMLButtonElement>(`${scope} .context-web__actions button`);
    if (!target || target.disabled) {
      this.retryFocus(pending, attempt);
      return;
    }
    target.focus({ preventScroll: true });
    this.focusTimer = setTimeout(() => {
      if (document.activeElement === target) {
        this.facade.clearFocusTarget();
        return;
      }
      this.retryFocus(pending, attempt);
    }, 150);
  }

  private retryFocus(pending: string, attempt: number): void {
    if (attempt >= 4) {
      this.facade.clearFocusTarget();
      return;
    }
    this.focusTimer = setTimeout(() => this.restoreFocus(pending, attempt + 1), 200);
  }

  private clearFocusTimer(): void {
    if (this.focusTimer) {
      clearTimeout(this.focusTimer);
      this.focusTimer = undefined;
    }
  }
}
