import { ScrollingModule, VIRTUAL_SCROLL_STRATEGY } from '@angular/cdk/scrolling';
import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  TemplateRef,
  computed,
  contentChild,
  effect,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingSelection } from '../../models/linking-workspace.models';
import { LinkingFocusCoordinator } from '../../state/linking-focus.coordinator';
import { isVerseSelected } from '../../utils/linking-selection';
import { MeasuredRowVirtualScrollStrategy } from '../../utils/measured-row-virtual-scroll.strategy';
import { LinkingAyahCardComponent } from '../linking-ayah-card/linking-ayah-card.component';
import { LinkingAyahGroupComponent } from '../linking-ayah-group/linking-ayah-group.component';

export interface LinkingWordToggle {
  verseKey: string;
  quranWordId: number;
}

const ESTIMATED_AYAH_ROW_SIZE = 168;
const AYAH_ROW_BUFFER = 720;

let nextSelectionId = 0;

@Component({
  selector: 'qd-linking-ayah-selection',
  standalone: true,
  imports: [
    NgTemplateOutlet,
    ScrollingModule,
    QdActionDirective,
    LinkingAyahCardComponent,
    LinkingAyahGroupComponent,
  ],
  providers: [
    {
      provide: VIRTUAL_SCROLL_STRATEGY,
      useFactory: (): MeasuredRowVirtualScrollStrategy =>
        new MeasuredRowVirtualScrollStrategy(ESTIMATED_AYAH_ROW_SIZE, AYAH_ROW_BUFFER),
    },
  ],
  templateUrl: './linking-ayah-selection.component.html',
  styleUrl: './linking-ayah-selection.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingAyahSelectionComponent {
  private readonly focus = inject(LinkingFocusCoordinator);
  private readonly selectAllButton = viewChild<ElementRef<HTMLButtonElement>>('selectAllButton');
  private readonly instance = nextSelectionId++;

  readonly ayahs = input.required<readonly LinkingAyah[]>();
  readonly selection = input.required<LinkingSelection>();
  readonly highlightSourceWords = input(true);
  readonly selectedCount = input.required<number>();
  readonly disabled = input(false);
  readonly focusOnEntry = input(false);
  readonly wordSelectable = input(false);
  readonly grouped = input(false);
  readonly listLabel = input<string>(LINKING_LABELS.selectAyahs);

  readonly selectionToggled = output<string>();
  readonly wordToggled = output<LinkingWordToggle>();
  readonly selectAllRequested = output<void>();
  readonly clearAllRequested = output<void>();

  protected readonly labels = LINKING_LABELS;
  protected readonly ayahExtraTemplate = contentChild<TemplateRef<{ $implicit: LinkingAyah }>>(
    'ayahExtraTemplate',
  );
  protected readonly trackAyah = (_index: number, ayah: LinkingAyah): string => ayah.verseKey;
  protected readonly groupedAyahs = computed(() =>
    this.ayahs().filter((ayah) => this.isSelected(ayah.verseKey)),
  );
  protected readonly ungroupedAyahs = computed(() =>
    this.ayahs().filter((ayah) => !this.isSelected(ayah.verseKey)),
  );

  constructor() {
    effect(() => {
      if (this.focusOnEntry()) {
        this.focus.focusAfterRender(() => this.selectAllButton()?.nativeElement ?? null);
      }
    });
  }

  protected inputId(verseKey: string): string {
    return `linking-ayah-selection-${this.instance}-${verseKey.replace(':', '-')}`;
  }

  protected isSelected(verseKey: string): boolean {
    return isVerseSelected(this.selection(), verseKey);
  }
}
