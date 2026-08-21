import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  input,
  output,
} from '@angular/core';

import { toQuranWordDisplayText } from '../../../../shared/quran/quran-word-display-text';
import { MushafWordDto } from '../../models/mushaf.models';
import {
  MushafDoorDetailsRequest,
  MushafDoorResolvedHighlight,
} from '../../models/mushaf-door-highlights.models';

const DOOR_DETAILS_LONG_PRESS_MS = 520;
const DOOR_DETAILS_MOVE_TOLERANCE_PX = 12;

@Component({
  selector: 'qd-mushaf-word',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './mushaf-word.component.html',
  styleUrls: ['./mushaf-word.component.scss'],
})
export class MushafWordComponent implements OnDestroy {
  readonly word = input.required<MushafWordDto>();
  readonly highlightedVerseKey = input<string | null>(null);
  readonly selectedWordLocation = input<string | null>(null);
  readonly ayahSelectionMode = input(false);
  readonly selectedVerseKeys = input<readonly string[]>([]);
  readonly doorHighlight = input<MushafDoorResolvedHighlight | null>(null);

  readonly ayahSelect = output<string>();
  readonly wordSelect = output<string>();
  readonly doorDetailsRequest = output<MushafDoorDetailsRequest>();

  private pressTimer: ReturnType<typeof setTimeout> | null = null;
  private activePointerId: number | null = null;
  private pressStartX = 0;
  private pressStartY = 0;
  private suppressNextClick = false;

  protected readonly displayText = computed(() => toQuranWordDisplayText(this.word().textUthmani));

  protected readonly doorBackground = computed(() => {
    const highlight = this.doorHighlight();
    if (!highlight || this.word().isAyahMarker) {
      return null;
    }

    return highlight.colorSlot === 'multi'
      ? 'var(--qd-door-highlight-multi-gradient)'
      : `var(--qd-door-highlight-${highlight.colorSlot})`;
  });

  protected readonly isSelectedAyah = computed(
    () => this.ayahSelectionMode() && this.selectedVerseKeys().includes(this.word().verseKey),
  );

  protected readonly selectionLabel = computed(() => {
    const word = this.word();
    if (!this.ayahSelectionMode() || word.isAyahMarker) {
      return null;
    }

    return `${this.isSelectedAyah() ? 'إلغاء تحديد' : 'تحديد'} الآية ${word.verseKey}`;
  });

  protected readonly interactionLabel = computed(() => {
    const selectionLabel = this.selectionLabel();
    const highlight = this.doorHighlight();
    if (!highlight) {
      return selectionLabel;
    }

    if (this.word().isAyahMarker) {
      return `رقم الآية ${this.word().verseKey}، مميز ضمن ${highlight.doors.length} من الأبواب`;
    }

    const target = selectionLabel ?? `الكلمة ${this.displayText()}`;
    return `${target}، مميز ضمن ${highlight.doors.length} من الأبواب، اضغط مطولًا لعرضها`;
  });

  protected readonly isHighlightedAyahWord = computed(() => {
    const word = this.word();
    const highlightedVerseKey = this.highlightedVerseKey();
    if (!highlightedVerseKey || word.isAyahMarker) {
      return false;
    }

    if (this.selectedWordLocation() === word.wordLocation) {
      return false;
    }

    return word.verseKey === highlightedVerseKey;
  });

  ngOnDestroy(): void {
    this.clearPress();
  }

  protected onPointerDown(event: PointerEvent): void {
    this.suppressNextClick = false;
    this.clearPress();
    if (this.word().isAyahMarker || !this.doorHighlight() || !event.isPrimary || event.button !== 0) {
      return;
    }
    if (event.pointerType !== 'touch' && event.pointerType !== 'pen') {
      return;
    }

    this.activePointerId = event.pointerId;
    this.pressStartX = event.clientX;
    this.pressStartY = event.clientY;
    this.pressTimer = setTimeout(() => this.openDoorDetails(), DOOR_DETAILS_LONG_PRESS_MS);
  }

  protected onPointerMove(event: PointerEvent): void {
    if (event.pointerId !== this.activePointerId) {
      return;
    }

    const movedX = Math.abs(event.clientX - this.pressStartX);
    const movedY = Math.abs(event.clientY - this.pressStartY);
    if (movedX > DOOR_DETAILS_MOVE_TOLERANCE_PX || movedY > DOOR_DETAILS_MOVE_TOLERANCE_PX) {
      this.clearPress();
    }
  }

  protected onPointerEnd(event: PointerEvent): void {
    if (event.pointerId === this.activePointerId) {
      this.clearPress();
    }
  }

  protected onContextMenu(event: MouseEvent): void {
    if (this.activePointerId !== null || this.suppressNextClick) {
      event.preventDefault();
    }
  }

  protected onWordClick(event: MouseEvent): void {
    if (this.suppressNextClick) {
      this.suppressNextClick = false;
      event.preventDefault();
      event.stopPropagation();
      return;
    }

    if (!this.word().isAyahMarker) {
      this.ayahSelect.emit(this.word().verseKey);
      this.wordSelect.emit(this.word().wordLocation);
    }
  }

  private openDoorDetails(): void {
    const highlight = this.doorHighlight();
    if (!highlight || this.activePointerId === null) {
      return;
    }

    this.pressTimer = null;
    this.suppressNextClick = true;
    this.doorDetailsRequest.emit({
      position: { x: this.pressStartX, y: this.pressStartY },
      highlight,
    });
  }

  private clearPress(): void {
    if (this.pressTimer !== null) {
      clearTimeout(this.pressTimer);
      this.pressTimer = null;
    }
    this.activePointerId = null;
  }
}
