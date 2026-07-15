import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import {
  WordTypePresenceFlagChange,
  WordTypesPresenceFilterComponent,
} from './word-types-presence-filter.component';
import { WORD_TYPES_PRESENCE_FILTER_LABELS } from '../../models/word-types.labels';

describe('WordTypesPresenceFilterComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [WordTypesPresenceFilterComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function render(inputs: {
    hasRoot?: boolean | null;
    hasStem?: boolean | null;
    hasLemma?: boolean | null;
    disabled?: boolean;
  } = {}) {
    const fixture = TestBed.createComponent(WordTypesPresenceFilterComponent);
    fixture.componentRef.setInput('hasRoot', inputs.hasRoot ?? null);
    fixture.componentRef.setInput('hasStem', inputs.hasStem ?? null);
    fixture.componentRef.setInput('hasLemma', inputs.hasLemma ?? null);
    fixture.componentRef.setInput('disabled', inputs.disabled ?? false);
    fixture.detectChanges();
    return fixture;
  }

  it('renders a three-option chip group per dimension with the lock-D labels', () => {
    const root = render().nativeElement as HTMLElement;

    for (const dimension of ['root', 'stem', 'lemma']) {
      expect(root.querySelector(`[data-testid="word-types-presence-${dimension}-any"]`)).toBeTruthy();
      expect(root.querySelector(`[data-testid="word-types-presence-${dimension}-present"]`)).toBeTruthy();
      expect(root.querySelector(`[data-testid="word-types-presence-${dimension}-missing"]`)).toBeTruthy();
    }
    expect(root.textContent).toContain(WORD_TYPES_PRESENCE_FILTER_LABELS.root);
    expect(root.textContent).toContain(WORD_TYPES_PRESENCE_FILTER_LABELS.stem);
    expect(root.textContent).toContain(WORD_TYPES_PRESENCE_FILTER_LABELS.lemma);
  });

  it('marks the active tri-state option via aria-pressed', () => {
    const root = render({ hasRoot: true, hasStem: false }).nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="word-types-presence-root-present"]')?.getAttribute('aria-pressed')).toBe('true');
    expect(root.querySelector('[data-testid="word-types-presence-root-any"]')?.getAttribute('aria-pressed')).toBe('false');
    expect(root.querySelector('[data-testid="word-types-presence-stem-missing"]')?.getAttribute('aria-pressed')).toBe('true');
    expect(root.querySelector('[data-testid="word-types-presence-lemma-any"]')?.getAttribute('aria-pressed')).toBe('true');
  });

  it('emits the chosen dimension/value on click', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    const emitted: WordTypePresenceFlagChange[] = [];
    fixture.componentInstance.flagChange.subscribe((change) => emitted.push(change));

    (root.querySelector('[data-testid="word-types-presence-root-present"]') as HTMLButtonElement).click();
    (root.querySelector('[data-testid="word-types-presence-lemma-missing"]') as HTMLButtonElement).click();
    (root.querySelector('[data-testid="word-types-presence-stem-any"]') as HTMLButtonElement).click();

    expect(emitted).toEqual([
      { dimension: 'root', value: true },
      { dimension: 'lemma', value: false },
      { dimension: 'stem', value: null },
    ]);
  });

  it('disables every chip and emits nothing while loading', () => {
    const fixture = render({ disabled: true });
    const root = fixture.nativeElement as HTMLElement;

    const chips = root.querySelectorAll<HTMLButtonElement>('.presence-filter__chip');
    expect(chips.length).toBe(9);
    expect([...chips].every((chip) => chip.disabled)).toBe(true);
  });
});
