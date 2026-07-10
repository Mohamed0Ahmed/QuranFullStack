import { computed, signal } from '@angular/core';
import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';

import {
  ExplorerTableFocusController,
  ExplorerTableFocusState,
} from './explorer-table-focus-controller';

interface PanelState {
  selectedId: number | null;
  view: 'ayahs' | 'surahs';
  wordView: null;
  surahView: 'mentioned' | null;
  fallbackColumn: 'occurrences' | 'ayahs' | 'surahs';
}

interface EventPayload {
  id: number;
  column: 'occurrences' | 'ayahs' | 'surahs';
  view: 'ayahs' | 'surahs';
}

describe('ExplorerTableFocusController', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('keeps immediate preview separate from deferred commit', () => {
    const panelState = signal<PanelState>({
      selectedId: 10,
      view: 'ayahs',
      wordView: null,
      surahView: null,
      fallbackColumn: 'occurrences',
    });
    const committed = vi.fn();
    const controller = new ExplorerTableFocusController<
      PanelState,
      EventPayload,
      'occurrences' | 'ayahs' | 'surahs',
      'ayahs' | 'surahs',
      never,
      'mentioned'
    >({
      panelState: computed(() => panelState()),
      getSelectedRowId: (state) => state.selectedId,
      getView: (state) => state.view,
      getWordView: () => null,
      getSurahView: (state) => state.surahView,
      getFallbackColumn: (state) => state.fallbackColumn,
      eventToFocus: (event): ExplorerTableFocusState<'occurrences' | 'ayahs' | 'surahs', 'ayahs' | 'surahs', never, 'mentioned'> => ({
        rowId: event.id,
        column: event.column,
        view: event.view,
      }),
      commitDeferred: committed,
    });

    controller.handleEvent({ id: 20, column: 'ayahs', view: 'ayahs' }, 'keyboard');

    expect(controller.selectedRowId()).toBe(20);
    expect(controller.activeColumn()).toBe('ayahs');
    expect(committed).not.toHaveBeenCalled();

    vi.advanceTimersByTime(500);
    expect(committed).toHaveBeenCalledOnce();
  });

  it('cancels deferred work when cleared', () => {
    const panelState = signal<PanelState>({
      selectedId: 10,
      view: 'ayahs',
      wordView: null,
      surahView: null,
      fallbackColumn: 'occurrences',
    });
    const committed = vi.fn();
    const controller = new ExplorerTableFocusController<
      PanelState,
      EventPayload,
      'occurrences' | 'ayahs' | 'surahs',
      'ayahs' | 'surahs'
    >({
      panelState: computed(() => panelState()),
      getSelectedRowId: (state) => state.selectedId,
      getView: (state) => state.view,
      getWordView: () => null,
      getSurahView: () => null,
      getFallbackColumn: (state) => state.fallbackColumn,
      eventToFocus: (event) => ({
        rowId: event.id,
        column: event.column,
        view: event.view,
      }),
      commitDeferred: committed,
    });

    controller.handleEvent({ id: 20, column: 'surahs', view: 'surahs' }, 'keyboard');
    controller.clear();
    vi.advanceTimersByTime(500);

    expect(controller.selectedRowId()).toBe(10);
    expect(controller.activeColumn()).toBe('occurrences');
    expect(committed).not.toHaveBeenCalled();
  });
});
