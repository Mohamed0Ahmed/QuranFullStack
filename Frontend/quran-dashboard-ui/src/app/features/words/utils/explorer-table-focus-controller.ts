import { Signal, WritableSignal, computed, signal } from '@angular/core';

import {
  ExplorerInteractionSource,
} from './explorer-table-keydown';
import { ExplorerKeyboardNavScheduler } from './explorer-keyboard-nav.scheduler';

export interface ExplorerTableFocusState<
  Column extends string,
  View extends string,
  WordView extends string = never,
  SurahView extends string = never,
> {
  rowId: number;
  column: Column;
  view: View;
  wordView?: WordView;
  surahView?: SurahView;
}

interface ExplorerTableFocusControllerOptions<
  PanelState,
  Event,
  Column extends string,
  View extends string,
  WordView extends string,
  SurahView extends string,
> {
  panelState: Signal<PanelState>;
  getSelectedRowId: (state: PanelState) => number | null;
  getView: (state: PanelState) => View | null;
  getWordView: (state: PanelState) => WordView | null;
  getSurahView: (state: PanelState) => SurahView | null;
  getFallbackColumn: (state: PanelState) => Column | null;
  eventToFocus: (
    event: Event,
  ) => ExplorerTableFocusState<Column, View, WordView, SurahView>;
  commitDeferred: (event: Event) => void;
}

export class ExplorerTableFocusController<
  PanelState,
  Event,
  Column extends string,
  View extends string,
  WordView extends string = never,
  SurahView extends string = never,
> {
  readonly focus: WritableSignal<ExplorerTableFocusState<Column, View, WordView, SurahView> | null> =
    signal<ExplorerTableFocusState<Column, View, WordView, SurahView> | null>(null);
  readonly selectedRowId;
  readonly activeView;
  readonly activeWordView;
  readonly activeSurahView;
  readonly activeColumn;

  private readonly scheduler: ExplorerKeyboardNavScheduler<Event>;

  constructor(
    private readonly options: ExplorerTableFocusControllerOptions<
      PanelState,
      Event,
      Column,
      View,
      WordView,
      SurahView
    >,
  ) {
    this.scheduler = new ExplorerKeyboardNavScheduler<Event>((event) =>
      this.options.commitDeferred(event),
    );

    this.selectedRowId = computed(
      () => this.focus()?.rowId ?? this.options.getSelectedRowId(this.options.panelState()),
    );
    this.activeView = computed(
      () => this.focus()?.view ?? this.options.getView(this.options.panelState()),
    );
    this.activeWordView = computed(
      () => this.focus()?.wordView ?? this.options.getWordView(this.options.panelState()),
    );
    this.activeSurahView = computed(
      () => this.focus()?.surahView ?? this.options.getSurahView(this.options.panelState()),
    );
    this.activeColumn = computed(
      () => this.focus()?.column ?? this.options.getFallbackColumn(this.options.panelState()),
    );
  }

  handleEvent(event: Event, source: ExplorerInteractionSource): void {
    this.focus.set(this.options.eventToFocus(event));

    if (source === 'keyboard') {
      this.scheduler.schedule(event);
      return;
    }

    this.scheduler.cancel();
    this.options.commitDeferred(event);
  }

  setFocus(
    focus: ExplorerTableFocusState<Column, View, WordView, SurahView> | null,
  ): void {
    this.scheduler.cancel();
    this.focus.set(focus);
  }

  cancel(): void {
    this.scheduler.cancel();
  }

  clear(): void {
    this.scheduler.cancel();
    this.focus.set(null);
  }

  destroy(): void {
    this.scheduler.cancel();
  }
}
