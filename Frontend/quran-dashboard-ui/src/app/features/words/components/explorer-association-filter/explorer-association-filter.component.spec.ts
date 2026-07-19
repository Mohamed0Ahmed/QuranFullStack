import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import {
  AssociationOption,
  ExplorerAssociationFilterComponent,
} from './explorer-association-filter.component';
import { WORDS_ASSOCIATION_FILTER_LABELS } from '../../models/words-shared.labels';

const OPTIONS: readonly AssociationOption[] = [
  { id: 'PN', label: 'اسم علم' },
  { id: 'N', label: 'اسم' },
];

describe('ExplorerAssociationFilterComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [ExplorerAssociationFilterComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function render(inputs: Partial<{
    options: readonly AssociationOption[];
    selectedId: string | number | null;
    selectedLabel: string | null;
    clientFilter: boolean;
    loading: boolean;
    error: boolean;
    disabled: boolean;
  }> = {}) {
    const fixture = TestBed.createComponent(ExplorerAssociationFilterComponent);
    fixture.componentRef.setInput('label', 'النوع الأساسي');
    fixture.componentRef.setInput('options', inputs.options ?? OPTIONS);
    fixture.componentRef.setInput('selectedId', inputs.selectedId ?? null);
    if (inputs.selectedLabel !== undefined) fixture.componentRef.setInput('selectedLabel', inputs.selectedLabel);
    if (inputs.clientFilter !== undefined) fixture.componentRef.setInput('clientFilter', inputs.clientFilter);
    if (inputs.loading !== undefined) fixture.componentRef.setInput('loading', inputs.loading);
    if (inputs.error !== undefined) fixture.componentRef.setInput('error', inputs.error);
    if (inputs.disabled !== undefined) fixture.componentRef.setInput('disabled', inputs.disabled);
    fixture.detectChanges();
    return fixture;
  }

  function searchField(root: HTMLElement): HTMLInputElement {
    return root.querySelector<HTMLInputElement>('[data-testid="association-filter-search"]')!;
  }

  // The option list lives in a popover panel that only renders while open. Focus alone no longer opens
  // it (an empty, unselected field stays closed), so the generic opener here is the ArrowDown escape
  // hatch: it opens from any state without touching the query or emitting searchChange.
  function openPanel(fixture: ReturnType<typeof render>): void {
    searchField(fixture.nativeElement as HTMLElement).dispatchEvent(
      new KeyboardEvent('keydown', { key: 'ArrowDown' }),
    );
    fixture.detectChanges();
  }

  // openPanel defers focusing the first option to a rAF, mirroring the panel's own layout pass.
  function flushPanelLayout(): Promise<void> {
    return new Promise((resolve) => {
      requestAnimationFrame(() => {
        requestAnimationFrame(() => resolve());
      });
    });
  }

  it('renders the label and one option button per option once the panel is open', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('النوع الأساسي');

    openPanel(fixture);

    expect(root.querySelector('[data-testid="association-filter-option-PN"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="association-filter-option-N"]')).toBeTruthy();
  });

  it('emits the selected option when an option is clicked', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    openPanel(fixture);

    let emitted: AssociationOption | null | undefined;
    fixture.componentInstance.selectionChange.subscribe((value) => (emitted = value));

    root.querySelector<HTMLButtonElement>('[data-testid="association-filter-option-PN"]')!.click();

    expect(emitted).toEqual({ id: 'PN', label: 'اسم علم' });
  });

  it('emits searchChange on input when not client-filtering (server search)', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    let term: string | undefined;
    fixture.componentInstance.searchChange.subscribe((value) => (term = value));

    const input = searchField(root);
    input.value = '  علم  ';
    input.dispatchEvent(new Event('input'));

    expect(term).toBe('علم');
  });

  it('client-filters the options locally and does NOT emit searchChange when clientFilter is true', () => {
    const fixture = render({ clientFilter: true });
    const root = fixture.nativeElement as HTMLElement;

    let searchEmitted = false;
    fixture.componentInstance.searchChange.subscribe(() => (searchEmitted = true));

    // Typing opens the panel itself, so no separate focus dispatch is needed here.
    const input = searchField(root);
    input.value = 'علم';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    // "اسم علم" (PN) matches; "اسم" (N) does not.
    expect(root.querySelector('[data-testid="association-filter-option-PN"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="association-filter-option-N"]')).toBeNull();
    expect(searchEmitted).toBe(false);
  });

  it('shows the selected value and clears it via a clear-labelled button', () => {
    const fixture = render({ selectedId: 'PN' });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="association-filter-value"]')?.textContent?.trim()).toBe('اسم علم');

    // The clear affordance must announce a clear action, not just the filter name.
    const clear = root.querySelector<HTMLButtonElement>('[data-testid="association-filter-clear"]')!;
    expect(clear.getAttribute('aria-label')).toBe(`${WORDS_ASSOCIATION_FILTER_LABELS.clear}: النوع الأساسي`);

    let emitted: AssociationOption | null | undefined = { id: 'x', label: 'x' };
    fixture.componentInstance.selectionChange.subscribe((value) => (emitted = value));
    clear.click();

    expect(emitted).toBeNull();
  });

  it('closes the panel when Clear is clicked while it is open', () => {
    const fixture = render({ selectedId: 'PN' });
    const root = fixture.nativeElement as HTMLElement;
    openPanel(fixture);
    expect(searchField(root).getAttribute('aria-expanded')).toBe('true');

    root.querySelector<HTMLButtonElement>('[data-testid="association-filter-clear"]')!.click();
    fixture.detectChanges();

    expect(searchField(root).getAttribute('aria-expanded')).toBe('false');
    expect(root.querySelector('[data-testid="association-filter-option-PN"]')).toBeNull();
  });

  it('falls back to the explicit selectedLabel when the option is not in the loaded list', () => {
    const fixture = render({ options: [], selectedId: 5001, selectedLabel: 'ك ل م' });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="association-filter-value"]')?.textContent?.trim()).toBe('ك ل م');
  });

  it('shows the neutral active-filter badge (never the raw id) while no label has resolved', () => {
    const fixture = render({ options: [], selectedId: 5001 });
    const root = fixture.nativeElement as HTMLElement;

    const badge = root.querySelector('[data-testid="association-filter-value"]')?.textContent?.trim();
    expect(badge).toBe(WORDS_ASSOCIATION_FILTER_LABELS.activeFilter);
    expect(badge).not.toContain('5001');
  });

  it('announces the loading state to assistive technology instead of hiding it', () => {
    const fixture = render({ loading: true });
    const root = fixture.nativeElement as HTMLElement;
    openPanel(fixture);

    const hint = root.querySelector('[data-testid="association-filter-loading"]')!;
    expect(hint.getAttribute('role')).toBe('status');
    expect(hint.getAttribute('aria-hidden')).toBeNull();
    expect(hint.textContent).toContain(WORDS_ASSOCIATION_FILTER_LABELS.loading);
  });

  // M32/M43 + M74: a load failure must render as its own distinct state, never collapse into a
  // silently-empty options list.
  it('renders a distinct error hint instead of an empty list when the options load failed', () => {
    const fixture = render({ error: true, options: [] });
    const root = fixture.nativeElement as HTMLElement;
    openPanel(fixture);

    const hint = root.querySelector('[data-testid="association-filter-error"]')!;
    expect(hint.getAttribute('role')).toBe('status');
    expect(hint.textContent).toContain(WORDS_ASSOCIATION_FILTER_LABELS.error);
    expect(root.querySelector('[data-testid="association-filter-loading"]')).toBeNull();
    expect(root.querySelector('.association-filter__options')).toBeNull();
  });

  it('prefers the error state over a loading options list when both are somehow set', () => {
    const fixture = render({ error: true, loading: true, options: [] });
    const root = fixture.nativeElement as HTMLElement;
    openPanel(fixture);

    expect(root.querySelector('[data-testid="association-filter-loading"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="association-filter-error"]')).toBeNull();
  });

  it('shows an explicit no-results hint for a genuine zero-match result (not loading, not error)', () => {
    const fixture = render({ options: [] });
    const root = fixture.nativeElement as HTMLElement;
    openPanel(fixture);

    const hint = root.querySelector('[data-testid="association-filter-no-results"]')!;
    expect(hint.textContent).toContain(WORDS_ASSOCIATION_FILTER_LABELS.noResults);
    expect(root.querySelector('[data-testid="association-filter-error"]')).toBeNull();
    expect(root.querySelector('[data-testid="association-filter-loading"]')).toBeNull();
  });

  it('does NOT open the panel when an empty, unselected field receives focus', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    const input = searchField(root);

    input.dispatchEvent(new FocusEvent('focus'));
    fixture.detectChanges();

    expect(input.getAttribute('aria-expanded')).toBe('false');
    expect(root.querySelector('[data-testid="association-filter-option-PN"]')).toBeNull();
  });

  it('re-opens the options on focus while a selection is active', () => {
    const fixture = render({ selectedId: 'PN' });
    const root = fixture.nativeElement as HTMLElement;
    const input = searchField(root);

    input.dispatchEvent(new FocusEvent('focus'));
    fixture.detectChanges();

    expect(input.getAttribute('aria-expanded')).toBe('true');
    expect(root.querySelector('[data-testid="association-filter-option-PN"]')).toBeTruthy();
  });

  it.each([
    ['ArrowDown', false],
    ['Alt+ArrowDown', true],
  ])('opens the panel from an empty field on %s and moves focus to the first option', async (_key, altKey) => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    const input = searchField(root);

    const event = new KeyboardEvent('keydown', { key: 'ArrowDown', altKey, cancelable: true });
    input.dispatchEvent(event);
    fixture.detectChanges();

    // preventDefault keeps the key from scrolling the toolbar instead of opening the panel.
    expect(event.defaultPrevented).toBe(true);
    expect(input.getAttribute('aria-expanded')).toBe('true');

    await flushPanelLayout();
    expect(document.activeElement).toBe(
      root.querySelector('[data-testid="association-filter-option-PN"]'),
    );
  });

  it('keeps the panel closed on focus once the selection has been cleared', () => {
    const fixture = render({ selectedId: 'PN' });
    const root = fixture.nativeElement as HTMLElement;

    root.querySelector<HTMLButtonElement>('[data-testid="association-filter-clear"]')!.click();
    // The page owns selectedId, so the clear is only real once it flows back in.
    fixture.componentRef.setInput('selectedId', null);
    fixture.detectChanges();

    const input = searchField(root);
    input.dispatchEvent(new FocusEvent('focus'));
    fixture.detectChanges();

    expect(input.getAttribute('aria-expanded')).toBe('false');
    expect(root.querySelector('[data-testid="association-filter-option-PN"]')).toBeNull();
  });

  it('opens the panel when typing, even without a prior focus event', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    const input = searchField(root);

    input.value = 'م';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(input.getAttribute('aria-expanded')).toBe('true');
    expect(root.querySelector('[data-testid="association-filter-option-PN"]')).toBeTruthy();
  });

  it('closes the panel when focus leaves the whole component (focusout to an outside target)', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    openPanel(fixture);
    expect(searchField(root).getAttribute('aria-expanded')).toBe('true');

    const outside = document.createElement('button');
    document.body.appendChild(outside);
    try {
      root.dispatchEvent(new FocusEvent('focusout', { relatedTarget: outside, bubbles: true }));
      fixture.detectChanges();

      expect(searchField(root).getAttribute('aria-expanded')).toBe('false');
      expect(root.querySelector('[data-testid="association-filter-option-PN"]')).toBeNull();
    } finally {
      document.body.removeChild(outside);
    }
  });

  it('closes the panel on Escape and suppresses the immediate reopen from the focus restore', () => {
    // A selection is active on purpose: focus would otherwise re-open the panel, which is what makes
    // the suppression guard observable at all.
    const fixture = render({ selectedId: 'PN' });
    const root = fixture.nativeElement as HTMLElement;
    const input = searchField(root);
    openPanel(fixture);
    expect(input.getAttribute('aria-expanded')).toBe('true');

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();

    expect(input.getAttribute('aria-expanded')).toBe('false');

    // Escape restores focus to the field programmatically; that restore must not reopen the panel.
    input.dispatchEvent(new FocusEvent('focus'));
    fixture.detectChanges();
    expect(input.getAttribute('aria-expanded')).toBe('false');

    // Once the field genuinely blurs, the guard clears and a fresh focus opens the panel again.
    input.dispatchEvent(new FocusEvent('blur'));
    input.dispatchEvent(new FocusEvent('focus'));
    fixture.detectChanges();
    expect(input.getAttribute('aria-expanded')).toBe('true');
  });

  it('selecting an option closes the panel and clears the query', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    const input = searchField(root);
    openPanel(fixture);
    input.value = 'علم';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    root.querySelector<HTMLButtonElement>('[data-testid="association-filter-option-PN"]')!.click();
    fixture.detectChanges();

    expect(input.getAttribute('aria-expanded')).toBe('false');
    expect(input.value).toBe('');
    expect(root.querySelector('[data-testid="association-filter-option-PN"]')).toBeNull();
  });

  it('exposes aria-controls/aria-haspopup on the field and keeps options as plain non-listbox buttons', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    const input = searchField(root);

    expect(input.getAttribute('aria-haspopup')).toBe('true');
    expect(input.getAttribute('aria-controls')).toBeNull();

    openPanel(fixture);

    const controlsId = input.getAttribute('aria-controls');
    expect(controlsId).toBeTruthy();
    expect(root.querySelector(`#${controlsId}`)).toBeTruthy();

    const option = root.querySelector<HTMLButtonElement>('[data-testid="association-filter-option-PN"]')!;
    expect(option.tagName).toBe('BUTTON');
    expect(option.getAttribute('aria-pressed')).toBe('false');
    expect(root.querySelector('[role="listbox"]')).toBeNull();
    expect(root.querySelector('[role="option"]')).toBeNull();
  });
});
