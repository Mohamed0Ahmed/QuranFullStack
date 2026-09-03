import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  input,
  output,
} from '@angular/core';

import { toQuranWordDisplayText } from '../../../../shared/quran/quran-word-display-text';
import type { QuranVerseKey, QuranWordLocation } from '../../../../shared/quran/quran-location';
import { MushafWordDto } from '../../models/mushaf.models';
import {
  MushafDoorResolvedColorSlot,
  MushafDoorDetailsRequest,
  MushafDoorResolvedHighlight,
} from '../../models/mushaf-door-highlights.models';

const DOOR_DETAILS_LONG_PRESS_MS = 520;
const DOOR_DETAILS_MOVE_TOLERANCE_PX = 12;
const DOOR_DETAILS_HOVER_DELAY_MS = 200;

@Component({
  selector: 'qd-mushaf-word',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './mushaf-word.component.html',
  styleUrls: ['./mushaf-word.component.scss'],
})
export class MushafWordComponent implements OnDestroy {
  readonly word = input.required<MushafWordDto>();
  readonly highlightedVerseKey = input<QuranVerseKey | null>(null);
  readonly selectedWordLocation = input<QuranWordLocation | null>(null);
  readonly ayahSelectionMode = input(false);
  readonly selectedVerseKeys = input<readonly string[]>([]);
  readonly doorHighlight = input<MushafDoorResolvedHighlight | null>(null);

  readonly ayahSelect = output<QuranVerseKey>();
  readonly wordSelect = output<QuranWordLocation>();
  readonly doorDetailsRequest = output<MushafDoorDetailsRequest | null>();

  private pressTimer: ReturnType<typeof setTimeout> | null = null;
  private activePointerId: number | null = null;
  private pressStartX = 0;
  private pressStartY = 0;
  private suppressNextClick = false;
  private previewTimer: ReturnType<typeof setTimeout> | null = null;
  private previewOpen = false;

  protected readonly displayText = computed(() => toQuranWordDisplayText(this.word().textUthmani));
  protected readonly doorFill = computed(() => {
    const slot: MushafDoorResolvedColorSlot | undefined = this.doorHighlight()?.colorSlot;
    if (slot === undefined) {
      return null;
    }
    return slot === 'multi'
      ? 'var(--qd-door-highlight-multi-gradient)'
      : `var(--qd-door-highlight-${slot})`;
  });
  protected readonly doorMarkerFill = computed(() => {
    const slot: MushafDoorResolvedColorSlot | undefined = this.doorHighlight()?.colorSlot;
    if (slot === undefined) {
      return null;
    }
    return slot === 'multi'
      ? 'var(--qd-door-marker-multi-gradient)'
      : `var(--qd-door-highlight-${slot})`;
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
      return `رقم الآية ${this.word().verseKey}، مميز ضمن الأبواب: ${this.doorNames(highlight)}، اضغط مطولًا لعرضها`;
    }

    const target = selectionLabel ?? `الكلمة ${this.displayText()}`;
    return `${target}، مميز ضمن الأبواب: ${this.doorNames(highlight)}، اضغط مطولًا لعرضها`;
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
    this.clearDoorPreview(false);
  }

  protected onPointerDown(event: PointerEvent): void {
    this.suppressNextClick = false;
    this.clearPress();
    if (!this.doorHighlight() || !event.isPrimary || event.button !== 0) {
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

  protected onPointerEnter(event: PointerEvent): void {
    const highlight = this.doorHighlight();
    const anchorElement = event.currentTarget;
    if (
      event.pointerType !== 'mouse' ||
      !highlight ||
      !(anchorElement instanceof HTMLElement)
    ) {
      return;
    }

    this.clearDoorPreview(false);
    this.previewTimer = setTimeout(() => {
      const rect = anchorElement.getBoundingClientRect();
      this.previewTimer = null;
      this.previewOpen = true;
      this.doorDetailsRequest.emit({
        position: { x: rect.right, y: rect.bottom },
        anchorElement,
        presentation: 'tooltip',
        highlight,
      });
    }, DOOR_DETAILS_HOVER_DELAY_MS);
  }

  protected onPointerLeave(event: PointerEvent): void {
    if (event.pointerType === 'mouse') {
      this.clearDoorPreview();
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
      anchorElement: null,
      presentation: 'popover',
      highlight,
    });
  }

  private doorNames(highlight: MushafDoorResolvedHighlight): string {
    return highlight.doors.map((door) => door.name).join('، ');
  }

  private clearDoorPreview(emitDismiss = true): void {
    if (this.previewTimer !== null) {
      clearTimeout(this.previewTimer);
      this.previewTimer = null;
    }
    if (this.previewOpen && emitDismiss) {
      this.doorDetailsRequest.emit(null);
    }
    this.previewOpen = false;
  }

  private clearPress(): void {
    if (this.pressTimer !== null) {
      clearTimeout(this.pressTimer);
      this.pressTimer = null;
    }
    this.activePointerId = null;
  }
}
