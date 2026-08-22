import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';

import { QdControlDirective } from '../../../../shared/ui/form-field/control.directive';
import { QdFormFieldComponent } from '../../../../shared/ui/form-field/form-field.component';
import { ABWAB_LABELS } from '../../models/abwab.labels';

const ANNOUNCE_SETTLE_MS = 500;
const SEARCH_SETTLE_MS = 180;

@Component({
  selector: 'qd-abwab-search-controls',
  standalone: true,
  imports: [QdControlDirective, QdFormFieldComponent],
  templateUrl: './abwab-search-controls.component.html',
  styleUrl: './abwab-search-controls.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabSearchControlsComponent {
  readonly query = input<string>('');
  readonly matchCount = input<number>(0);
  readonly hideUnrelatedRoots = input<boolean>(false);
  readonly label = input<string>(ABWAB_LABELS.searchLabel);
  readonly placeholder = input<string>(ABWAB_LABELS.searchPlaceholder);
  readonly helper = input<string | null>(null);
  readonly testIdPrefix = input<string>('abwab');

  readonly queryChanged = output<string>();
  readonly hideUnrelatedRootsChanged = output<boolean>();

  private readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');
  protected readonly searchDraft = signal('');
  protected readonly announcedCountText = signal('');
  protected readonly matchCountText = computed(() => ABWAB_LABELS.searchMatchCount(this.matchCount()));
  protected readonly toggleDisabled = computed(() => this.searchDraft().trim() === '');
  protected readonly hideUnrelatedLabel = ABWAB_LABELS.hideUnrelatedRootsLabel;

  private announceTimer: ReturnType<typeof setTimeout> | null = null;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    effect(() => {
      const query = this.query();
      untracked(() => {
        this.clearSearchTimer();
        this.searchDraft.set(query);
      });
    });

    effect(() => {
      const query = this.query();
      const count = this.matchCount();
      untracked(() => {
        this.clearAnnounceTimer();
        if (query.trim() === '') {
          this.announcedCountText.set('');
          return;
        }
        this.announceTimer = setTimeout(() => {
          this.announcedCountText.set(ABWAB_LABELS.searchMatchCount(count));
          this.announceTimer = null;
        }, ANNOUNCE_SETTLE_MS);
      });
    });

    inject(DestroyRef).onDestroy(() => {
      this.clearAnnounceTimer();
      this.clearSearchTimer();
    });
  }

  focusInput(): void {
    this.searchInput()?.nativeElement.focus();
  }

  protected onSearchInput(event: Event): void {
    const query = (event.target as HTMLInputElement).value;
    this.searchDraft.set(query);
    this.clearSearchTimer();
    if (query === this.query()) {
      return;
    }
    this.searchTimer = setTimeout(() => {
      this.searchTimer = null;
      this.queryChanged.emit(query);
    }, SEARCH_SETTLE_MS);
  }

  protected onHideUnrelatedRootsChanged(event: Event): void {
    this.hideUnrelatedRootsChanged.emit((event.target as HTMLInputElement).checked);
  }

  private clearAnnounceTimer(): void {
    if (this.announceTimer !== null) {
      clearTimeout(this.announceTimer);
      this.announceTimer = null;
    }
  }

  private clearSearchTimer(): void {
    if (this.searchTimer !== null) {
      clearTimeout(this.searchTimer);
      this.searchTimer = null;
    }
  }
}
