import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  effect,
  ElementRef,
  inject,
  signal,
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

const MINIMUM_WORKSPACE_BUSY_MS = 300;

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
  private workspaceBusyTimer?: ReturnType<typeof setTimeout>;
  private readonly minimumWorkspaceBusy = signal(false);

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
  protected readonly workspaceInteractionBusy = computed(
    () => this.workspaceBusy() || this.minimumWorkspaceBusy(),
  );
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
      if (!pending || this.workspaceInteractionBusy()) {
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
    this.clearWorkspaceBusyTimer();
    this.facade.unbindFromRoute();
  }

  protected selectCandidate(candidate: PhraseResolutionCandidateDto): void {
    this.facade.selectCandidate(candidate);
  }

  protected selectBranch(side: 'previous' | 'following', selectionRef: string): void {
    this.showWorkspaceBusy();
    if (side === 'previous') {
      this.facade.selectPrevious(selectionRef);
    } else {
      this.facade.selectFollowing(selectionRef);
    }
  }

  protected selectPath(side: 'previous' | 'following', selectionRef: string | null): void {
    this.showWorkspaceBusy();
    if (side === 'previous') {
      this.facade.selectPreviousPath(selectionRef);
    } else {
      this.facade.selectFollowingPath(selectionRef);
    }
  }

  protected loadMoreBranches(side: 'previous' | 'following'): void {
    this.showWorkspaceBusy();
    if (side === 'previous') {
      this.facade.loadMorePrevious();
    } else {
      this.facade.loadMoreFollowing();
    }
  }

  private restoreFocus(pending: string, attempt: number): void {
    if (this.state().focusTarget !== pending || this.workspaceInteractionBusy()) {
      return;
    }
    const side = pending.startsWith('previous') ? 'previous' : 'following';
    const scope = `.context-web--${side}`;
    const options = this.host.nativeElement.querySelectorAll<HTMLButtonElement>(
      `${scope} .context-option`,
    );
    const target = pending.endsWith('more')
      ? (this.host.nativeElement.querySelector<HTMLButtonElement>(`${scope} .context-web__more`) ??
        options.item(options.length - 1))
      : (options.item(0) ??
        this.host.nativeElement.querySelector<HTMLButtonElement>(`${scope} .context-path__item`));
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

  private showWorkspaceBusy(): void {
    this.clearWorkspaceBusyTimer();
    this.minimumWorkspaceBusy.set(true);
    this.workspaceBusyTimer = setTimeout(() => {
      this.minimumWorkspaceBusy.set(false);
      this.workspaceBusyTimer = undefined;
    }, MINIMUM_WORKSPACE_BUSY_MS);
  }

  private clearWorkspaceBusyTimer(): void {
    if (this.workspaceBusyTimer) {
      clearTimeout(this.workspaceBusyTimer);
      this.workspaceBusyTimer = undefined;
    }
  }
}
