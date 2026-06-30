import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { WordTypeDetailsPanelComponent } from './word-type-details-panel.component';
import { WORD_TYPE_DETAIL_VIEW_KEYS, WordTypeDetailView } from '../../models/word-types.models';

describe('WordTypeDetailsPanelComponent', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function createPanel(view: WordTypeDetailView = 'ayahs') {
    TestBed.configureTestingModule({
      imports: [WordTypeDetailsPanelComponent],
      teardown: { destroyAfterEach: true },
    });
    const fixture = TestBed.createComponent(WordTypeDetailsPanelComponent);
    fixture.componentRef.setInput('view', view);
    fixture.componentRef.setInput('emptySelection', false);
    fixture.detectChanges();
    return fixture;
  }

  it('renders three tabs linked to a single tabpanel', () => {
    const fixture = createPanel('ayahs');
    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelectorAll('[role="tab"]')).toHaveLength(3);
    expect(host.querySelector('[role="tabpanel"]')?.id).toBe('word-type-details-panel-surface');
  });

  it('marks the active tab selected with roving tabindex', () => {
    const fixture = createPanel('surahs');
    const host = fixture.nativeElement as HTMLElement;

    for (const key of WORD_TYPE_DETAIL_VIEW_KEYS) {
      const tab = host.querySelector(`[data-word-type-tab="${key}"]`) as HTMLElement;
      expect(tab.getAttribute('aria-selected')).toBe(String(key === 'surahs'));
      expect(tab.getAttribute('tabindex')).toBe(key === 'surahs' ? '0' : '-1');
    }
  });

  it('hides tabs when notFound is true', () => {
    const fixture = createPanel('ayahs');
    fixture.componentRef.setInput('notFound', true);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('[role="tablist"]')).toBeNull();
  });
});
