import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { ExplorerPanelSkeletonComponent } from '../../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdActionDirective } from '../../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../../shared/ui/error-state/error-state.component';
import { QdNoticeComponent } from '../../../../../shared/ui/notice/notice.component';
import { PaginationComponent } from '../../../../../shared/ui/pagination/pagination.component';
import { QdRefreshingIndicatorComponent } from '../../../../../shared/ui/refreshing-indicator/refreshing-indicator.component';
import { QdModalShellComponent } from '../../../../../shared/ui/modal-shell/modal-shell.component';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../../../shared/layout/breakpoints';
import { PhraseOccurrenceListComponent } from '../../components/phrase-occurrence-list/phrase-occurrence-list.component';
import { PhraseRepetitionsListComponent } from '../../components/phrase-repetitions-list/phrase-repetitions-list.component';
import { PhraseTextModeToggleComponent } from '../../components/phrase-text-mode-toggle/phrase-text-mode-toggle.component';
import {
  PHRASE_REPETITION_SORT_OPTIONS,
  PhraseRepetitionSort,
  PhraseTextMode,
  isPhraseRepetitionSort,
  isPhraseTextMode,
} from '../../models/phrase-repetitions.models';
import { PhraseRepetitionsFacade } from '../../state/phrase-repetitions.facade';

@Component({
  selector: 'qd-phrase-repetitions-page',
  standalone: true,
  imports: [
    ExplorerPanelSkeletonComponent,
    NgTemplateOutlet,
    PaginationComponent,
    PhraseOccurrenceListComponent,
    PhraseRepetitionsListComponent,
    PhraseTextModeToggleComponent,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdNoticeComponent,
    QdModalShellComponent,
    QdRefreshingIndicatorComponent,
    QdActionDirective,
  ],
  templateUrl: './phrase-repetitions-page.component.html',
  styleUrl: './phrase-repetitions-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseRepetitionsPageComponent implements OnInit, OnDestroy {
  private readonly facade = inject(PhraseRepetitionsFacade);
  private readonly route = inject(ActivatedRoute);

  protected readonly state = this.facade.state;
  protected readonly sortOptions = PHRASE_REPETITION_SORT_OPTIONS;
  protected readonly availableModes = computed<readonly PhraseTextMode[]>(() =>
    (this.state().capabilities?.modes ?? [])
      .map((item) => item.mode)
      .filter(isPhraseTextMode),
  );
  protected readonly availableLengths = computed<readonly number[]>(() => {
    const state = this.state();
    return (
      state.capabilities?.modes.find((item) => item.mode === state.route.mode)
        ?.repeatedLengths ?? []
    );
  });
  protected readonly controlsBusy = computed(() => {
    const state = this.state();
    return (
      state.capabilitiesStatus === 'loading' ||
      state.capabilitiesStatus === 'refreshing' ||
      state.listStatus === 'loading' ||
      state.listStatus === 'refreshing'
    );
  });
  protected readonly listBusy = computed(() => {
    const status = this.state().listStatus;
    return status === 'loading' || status === 'refreshing';
  });
  protected readonly occurrencesBusy = computed(() => {
    const status = this.state().occurrencesStatus;
    return status === 'loading' || status === 'refreshing';
  });
  protected readonly countAnnouncement = computed(() => {
    const state = this.state();
    if (state.listStatus !== 'success' && state.listStatus !== 'empty') {
      return '';
    }
    return `تم تحميل ${state.list?.totalCount ?? 0} عبارة متكررة`;
  });
  protected readonly isDesktop = signal(true);
  private desktopQuery?: MediaQueryList;
  private readonly onDesktopChange = (event: MediaQueryListEvent): void =>
    this.isDesktop.set(event.matches);

  ngOnInit(): void {
    this.facade.bindToRoute(this.route);
    if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
      this.desktopQuery = window.matchMedia(QD_BP_DESKTOP_MIN_QUERY);
      this.isDesktop.set(this.desktopQuery.matches);
      this.desktopQuery.addEventListener('change', this.onDesktopChange);
    }
  }

  ngOnDestroy(): void {
    this.desktopQuery?.removeEventListener('change', this.onDesktopChange);
    this.facade.unbindFromRoute();
  }

  protected onModeChange(mode: PhraseTextMode): void {
    this.facade.setMode(mode);
  }

  protected onLengthChange(value: string): void {
    const length = Number(value);
    if (Number.isSafeInteger(length) && this.availableLengths().includes(length)) {
      this.facade.setLength(length);
    }
  }

  protected onSortChange(value: string): void {
    if (isPhraseRepetitionSort(value)) {
      this.facade.setSort(value);
    }
  }

  protected submitSearch(event: Event, query: string): void {
    event.preventDefault();
    this.facade.setQuery(query);
  }

  protected clearSearch(input: HTMLInputElement): void {
    input.value = '';
    this.facade.setQuery('');
    input.focus();
  }

  protected onPageChange(page: number): void {
    this.facade.setPage(page);
  }

  protected onPhraseSelected(variantId: number): void {
    this.facade.selectPhrase(variantId);
  }

  protected onOccurrencePageChange(page: number): void {
    this.facade.setOccurrencePage(page);
  }

  protected clearPhrase(): void {
    this.facade.clearPhrase();
  }

  protected retry(): void {
    this.facade.retry();
  }

  protected resetInvalidState(): void {
    this.facade.resetInvalidState();
  }

  protected dismissIndexNotice(): void {
    this.facade.dismissIndexNotice();
  }
}
