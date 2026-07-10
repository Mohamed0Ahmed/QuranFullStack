import { isPlatformBrowser } from '@angular/common';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  input,
  output,
  PLATFORM_ID,
  signal,
} from '@angular/core';

import {
  PAGINATION_GO_LABEL,
  PAGINATION_INVALID_PAGE_LABEL,
  PAGINATION_JUMP_INPUT_LABEL,
  PAGINATION_NEXT_LABEL,
  PAGINATION_PAGES_GROUP_LABEL,
  PAGINATION_PREV_LABEL,
} from './pagination.labels';
import { lastPageNumber } from './pagination-range';
import {
  buildPaginationWindow,
  DEFAULT_PAGINATION_WINDOW_SIZE,
  MOBILE_PAGINATION_WINDOW_SIZE,
} from './pagination-window';

import { QD_BP_PHONE_MAX_QUERY } from '../../layout/breakpoints';

@Component({
  selector: 'qd-pagination',
  standalone: true,
  templateUrl: './pagination.component.html',
  styleUrl: './pagination.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginationComponent {
  private readonly destroyRef = inject(DestroyRef);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isCompactLayout = signal(false);

  readonly currentPage = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly disabled = input(false);
  readonly ariaLabel = input.required<string>();

  readonly pageChange = output<number>();

  protected readonly prevLabel = PAGINATION_PREV_LABEL;
  protected readonly nextLabel = PAGINATION_NEXT_LABEL;
  protected readonly goLabel = PAGINATION_GO_LABEL;
  protected readonly jumpInputLabel = PAGINATION_JUMP_INPUT_LABEL;
  protected readonly pagesGroupLabel = PAGINATION_PAGES_GROUP_LABEL;
  protected readonly jumpPlaceholder = '…';
  protected readonly invalidPageLabel = PAGINATION_INVALID_PAGE_LABEL;

  protected readonly jumpValue = signal('');
  protected readonly jumpError = signal<string | null>(null);
  protected readonly jumpActive = signal(false);

  protected readonly showPagination = computed(() => this.totalCount() > this.pageSize());

  protected readonly lastPage = computed(() => lastPageNumber(this.pageSize(), this.totalCount()));

  protected readonly visiblePages = computed(() =>
    buildPaginationWindow(
      this.currentPage(),
      this.lastPage(),
      this.isCompactLayout() ? MOBILE_PAGINATION_WINDOW_SIZE : DEFAULT_PAGINATION_WINDOW_SIZE,
    ),
  );

  constructor() {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    afterNextRender(() => {
      if (typeof window.matchMedia !== 'function') {
        return;
      }

      const query = window.matchMedia(QD_BP_PHONE_MAX_QUERY);
      const sync = () => this.isCompactLayout.set(query.matches);
      sync();
      query.addEventListener('change', sync);
      this.destroyRef.onDestroy(() => query.removeEventListener('change', sync));
    });
  }

  protected selectPage(page: number): void {
    if (this.disabled() || page === this.currentPage() || page < 1 || page > this.lastPage()) {
      return;
    }

    this.resetJump();
    this.pageChange.emit(page);
  }

  protected onJumpFocus(): void {
    if (this.disabled()) {
      return;
    }

    this.jumpActive.set(true);
    this.jumpError.set(null);
  }

  protected onJumpBlur(): void {
    this.jumpActive.set(false);

    if (!this.jumpError()) {
      this.jumpValue.set('');
    }
  }

  protected onJumpInput(value: string): void {
    this.jumpValue.set(value);
    if (this.jumpError()) {
      this.jumpError.set(null);
    }
  }

  protected submitJump(event: Event): void {
    event.preventDefault();

    if (this.disabled()) {
      return;
    }

    const parsed = Number.parseInt(this.jumpValue().trim(), 10);
    if (!Number.isFinite(parsed) || parsed < 1 || parsed > this.lastPage()) {
      this.jumpError.set(this.invalidPageLabel);
      return;
    }

    if (parsed === this.currentPage()) {
      this.resetJump();
      return;
    }

    this.pageChange.emit(parsed);
    this.resetJump();
  }

  protected onJumpKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.resetJump();
      (event.target as HTMLInputElement).blur();
      return;
    }

    if (event.key === 'Enter') {
      event.preventDefault();
      this.submitJump(event);
    }
  }

  private resetJump(): void {
    this.jumpValue.set('');
    this.jumpError.set(null);
    this.jumpActive.set(false);
  }
}
