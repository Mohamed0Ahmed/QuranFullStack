import { ListRange } from '@angular/cdk/collections';
import {
  CdkVirtualScrollViewport,
  ScrollingModule,
  VIRTUAL_SCROLL_STRATEGY,
} from '@angular/cdk/scrolling';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  Injector,
  afterNextRender,
  effect,
  inject,
  input,
  output,
  runInInjectionContext,
  signal,
  viewChild,
} from '@angular/core';
import { Observable, Subscription } from 'rxjs';

import { LinkingPreparedAyahOverlayDto } from '../../../../core/api/generated/models/linking-prepared-ayah-overlay-dto';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LINKING_LABELS } from '../../models/linking.labels';
import {
  LinkingPageRange,
  LinkingPreparedDetailPage,
  LinkingPreparedDetailRequest,
  LinkingSourcePage,
  LinkingSourcePageRequest,
} from '../../models/linking-page.models';
import { LinkingQuranEntityStore } from '../../state/linking-quran-entity.store';
import { LinkingPreflightDetailsFacade } from '../../state/linking-preflight-details.facade';
import { LinkingSourcePagesFacade } from '../../state/linking-source-pages.facade';
import { MeasuredRowVirtualScrollStrategy } from '../../utils/measured-row-virtual-scroll.strategy';
import { LinkingAyahCardComponent } from '../linking-ayah-card/linking-ayah-card.component';

export interface LinkingVirtualWordToggle {
  ayahId: number;
  quranWordId: number;
}

interface LinkingVirtualAyahRow {
  position: number;
  ayahId: number | null;
  linkingDataRevision: number | null;
  wordIds: readonly number[];
  matchedWordIds: readonly number[];
  overlays: readonly LinkingPreparedAyahOverlayDto[];
  groupLabel: string | null;
}

const ESTIMATED_ROW_SIZE = 168;
const ROW_BUFFER = 720;

@Component({
  selector: 'qd-linking-virtual-ayah-list',
  standalone: true,
  imports: [ScrollingModule, LinkingAyahCardComponent],
  providers: [
    {
      provide: VIRTUAL_SCROLL_STRATEGY,
      useFactory: (): MeasuredRowVirtualScrollStrategy =>
        new MeasuredRowVirtualScrollStrategy(ESTIMATED_ROW_SIZE, ROW_BUFFER),
    },
  ],
  templateUrl: './linking-virtual-ayah-list.component.html',
  styleUrl: './linking-virtual-ayah-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingVirtualAyahListComponent {
  private readonly sourcePages = inject(LinkingSourcePagesFacade);
  private readonly detailPages = inject(LinkingPreflightDetailsFacade);
  private readonly entities = inject(LinkingQuranEntityStore);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);
  private readonly viewport = viewChild(CdkVirtualScrollViewport);
  private readonly rowsSignal = signal<readonly LinkingVirtualAyahRow[]>([]);
  private readonly statusSignal = signal<'preparing' | 'loading' | 'ready' | 'error'>('preparing');
  private readonly errorSignal = signal<string | null>(null);
  private requestSubscription: Subscription | null = null;
  private rangeSubscription: Subscription | null = null;
  private requestIdentity = '';
  private mountedGeneration = 0;

  readonly scope = input.required<string>();
  readonly sourceRequest = input<Omit<LinkingSourcePageRequest, 'page'> | null>(null);
  readonly preparedRequest = input<Omit<LinkingPreparedDetailRequest, 'page'> | null>(null);
  readonly selectedAyahIds = input<readonly number[]>([]);
  readonly selectionMode = input<'all-except' | 'only'>('only');
  readonly selectedWordIdsByAyahId = input<Readonly<Record<number, readonly number[]>>>({});
  readonly selectionEnabled = input(false);
  readonly wordSelectable = input(false);
  readonly highlightSourceWords = input(true);
  readonly grouped = input(false);
  readonly preparedSourceId = input<number | null>(null);
  readonly listLabel = input('آيات الربط');

  readonly ayahToggled = output<number>();
  readonly wordToggled = output<LinkingVirtualWordToggle>();
  readonly pageReady = output<{ linkingDataRevision: number; totalItems: number }>();

  protected readonly rows = this.rowsSignal.asReadonly();
  protected readonly labels = LINKING_LABELS;
  protected readonly status = this.statusSignal.asReadonly();
  protected readonly errorMessage = this.errorSignal.asReadonly();
  protected readonly trackRow = (_index: number, row: LinkingVirtualAyahRow): string | number =>
    row.ayahId ?? `placeholder-${row.position}`;

  constructor() {
    effect(() => this.scheduleMount(requestKey(this.sourceRequest(), this.preparedRequest())));
    this.destroyRef.onDestroy(() => this.dispose());
  }

  protected ayahFor(row: LinkingVirtualAyahRow): LinkingAyah | null {
    if (row.ayahId === null || row.linkingDataRevision === null) {
      return null;
    }
    const ayah = this.entities.ayah(row.linkingDataRevision, row.ayahId);
    if (ayah === null) {
      return null;
    }
    const selectedWords = this.selectedWordIdsByAyahId()[row.ayahId];
    const matches = new Set(
      this.wordSelectable() ? selectedWords ?? [] : selectedWords ?? row.matchedWordIds,
    );
    const words = row.wordIds.flatMap((wordId) => {
      const word = this.entities.word(row.linkingDataRevision!, wordId);
      return word === null
        ? []
        : [{
            renderPosition: word.wordNumber,
            canonicalQuranWordId: word.id,
            textUthmani: word.textUthmani,
            isAyahMarker: word.isAyahMarker,
            isSourceMatch: matches.has(word.id),
          }];
    });
    return {
      verseKey: ayah.verseKey,
      ayahId: ayah.id,
      surahNumber: ayah.surahNumber,
      surahNameArabic: ayah.surahNameArabic,
      ayahNumber: ayah.ayahNumber,
      pageNumber: ayah.pageFrom,
      words,
    };
  }

  protected overlayFor(row: LinkingVirtualAyahRow): LinkingPreparedAyahOverlayDto | null {
    const preparedSourceId = this.preparedSourceId();
    return row.overlays.find((overlay) => preparedSourceId === null || overlay.preparedSourceId === preparedSourceId) ?? null;
  }

  protected statusLabel(row: LinkingVirtualAyahRow): string | null {
    return this.overlayFor(row)?.classification ?? null;
  }

  protected isSelected(ayahId: number): boolean {
    const overridden = this.selectedAyahIds().includes(ayahId);
    return this.selectionMode() === 'all-except' ? !overridden : overridden;
  }

  protected isGroupedRow(row: LinkingVirtualAyahRow): boolean {
    return this.grouped() && row.ayahId !== null && this.isSelected(row.ayahId);
  }

  protected isGroupedStart(index: number): boolean {
    const rows = this.rowsSignal();
    return this.isGroupedRow(rows[index]) && (index === 0 || !this.isGroupedRow(rows[index - 1]));
  }

  protected isGroupedEnd(index: number): boolean {
    const rows = this.rowsSignal();
    return (
      this.isGroupedRow(rows[index]) &&
      (index === rows.length - 1 || !this.isGroupedRow(rows[index + 1]))
    );
  }

  protected displayGroupLabel(row: LinkingVirtualAyahRow): string | null {
    return this.grouped() ? null : row.groupLabel;
  }

  protected retry(): void {
    this.requestIdentity = '';
    this.scheduleMount(requestKey(this.sourceRequest(), this.preparedRequest()));
  }

  private scheduleMount(identity: string): void {
    if (identity === this.requestIdentity) {
      return;
    }
    this.requestIdentity = identity;
    const generation = ++this.mountedGeneration;
    this.disposeRequests();
    this.rowsSignal.set([]);
    this.statusSignal.set('preparing');
    this.errorSignal.set(null);
    runInInjectionContext(this.injector, () => {
      afterNextRender(() => {
        requestAnimationFrame(() => {
          if (generation === this.mountedGeneration) {
            this.mount(generation);
          }
        });
      });
    });
  }

  private mount(generation: number): void {
    const request = this.sourceRequest() ?? this.preparedRequest();
    if (request === null) {
      return;
    }
    this.statusSignal.set('loading');
    this.loadRange({ start: 0, end: request.pageSize }, generation);
    runInInjectionContext(this.injector, () => {
      afterNextRender(() => {
        const viewport = this.viewport();
        if (viewport === undefined) {
          return;
        }
        this.rangeSubscription = viewport.renderedRangeStream.subscribe((range) =>
          this.loadRange(range, generation),
        );
      });
    });
  }

  private loadRange(range: ListRange, generation: number): void {
    if (generation !== this.mountedGeneration || range.end <= range.start) {
      return;
    }
    this.requestSubscription?.unsubscribe();
    const sourceRequest = this.sourceRequest();
    const preparedRequest = this.preparedRequest();
    const request$: Observable<
      LinkingPageRange<LinkingSourcePage | LinkingPreparedDetailPage>
    > | null = sourceRequest !== null
      ? this.sourcePages.loadRange(this.scope(), sourceRequest, range.start, range.end - 1)
      : preparedRequest !== null
        ? this.detailPages.loadRange(preparedRequest, range.start, range.end - 1)
        : null;
    if (request$ === null) {
      return;
    }
    this.requestSubscription = request$.subscribe({
      next: (loaded) => this.acceptRange(loaded, generation),
      error: (error: unknown) => {
        if (generation === this.mountedGeneration) {
          this.statusSignal.set('error');
          this.errorSignal.set(error instanceof Error ? error.message : 'تعذر تحميل آيات الربط.');
        }
      },
    });
  }

  private acceptRange(
    range: LinkingPageRange<LinkingSourcePage | LinkingPreparedDetailPage>,
    generation: number,
  ): void {
    if (generation !== this.mountedGeneration) {
      range.release();
      return;
    }
    const first = range.pages[0];
    if (first === undefined) {
      return;
    }
    const totalItems = 'totalAyahCount' in first ? first.totalAyahCount : first.totalItems;
    const rows = ensureRows([], totalItems);
    for (const page of range.pages) {
      const offset = (page.page - 1) * page.pageSize;
      page.ayahIds.forEach((ayahId, index) => {
        const overlays = 'overlaysByAyahId' in page ? page.overlaysByAyahId[ayahId] ?? [] : [];
        rows[offset + index] = {
          position: offset + index,
          ayahId,
          linkingDataRevision: page.linkingDataRevision,
          wordIds: page.wordIdsByAyahId[ayahId] ?? [],
          matchedWordIds:
            'matchedWordIdsByAyahId' in page ? page.matchedWordIdsByAyahId[ayahId] ?? [] : [],
          overlays,
          groupLabel: groupLabel(overlays, index, this.grouped()),
        };
      });
    }
    this.rowsSignal.set(rows);
    this.statusSignal.set('ready');
    this.pageReady.emit({ linkingDataRevision: first.linkingDataRevision, totalItems });
  }

  private disposeRequests(): void {
    this.requestSubscription?.unsubscribe();
    this.requestSubscription = null;
    this.rangeSubscription?.unsubscribe();
    this.rangeSubscription = null;
    const prepared = this.preparedRequest();
    if (prepared !== null) {
      this.detailPages.cancel(prepared.preflightId);
    }
    this.sourcePages.cancel(this.scope());
  }

  private dispose(): void {
    this.mountedGeneration += 1;
    this.disposeRequests();
  }
}

function requestKey(
  source: Omit<LinkingSourcePageRequest, 'page'> | null,
  prepared: Omit<LinkingPreparedDetailRequest, 'page'> | null,
): string {
  return source === null
    ? JSON.stringify(prepared)
    : JSON.stringify([
        source.source,
        source.view,
        source.pageSize,
        source.draftGeneration,
      ]);
}

function ensureRows(
  current: readonly LinkingVirtualAyahRow[],
  length: number,
): LinkingVirtualAyahRow[] {
  return Array.from({ length }, (_, position) =>
    current[position] ?? {
      position,
      ayahId: null,
      linkingDataRevision: null,
      wordIds: [],
      matchedWordIds: [],
      overlays: [],
      groupLabel: null,
    },
  );
}

function groupLabel(
  overlays: readonly LinkingPreparedAyahOverlayDto[],
  pageIndex: number,
  groupedSource: boolean,
): string | null {
  const grouped = overlays.find((overlay) => overlay.isGrouped);
  if (grouped !== undefined && (pageIndex === 0 || grouped.ayahOrder === 1)) {
    return `المجموعة ${grouped.unitOrder}`;
  }
  return groupedSource && pageIndex === 0 ? LINKING_LABELS.groupedAyahs : null;
}
