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
    disabled: boolean;
  }> = {}) {
    const fixture = TestBed.createComponent(ExplorerAssociationFilterComponent);
    fixture.componentRef.setInput('label', 'النوع الأساسي');
    fixture.componentRef.setInput('options', inputs.options ?? OPTIONS);
    fixture.componentRef.setInput('selectedId', inputs.selectedId ?? null);
    if (inputs.selectedLabel !== undefined) fixture.componentRef.setInput('selectedLabel', inputs.selectedLabel);
    if (inputs.clientFilter !== undefined) fixture.componentRef.setInput('clientFilter', inputs.clientFilter);
    if (inputs.loading !== undefined) fixture.componentRef.setInput('loading', inputs.loading);
    if (inputs.disabled !== undefined) fixture.componentRef.setInput('disabled', inputs.disabled);
    fixture.detectChanges();
    return fixture;
  }

  function searchField(root: HTMLElement): HTMLInputElement {
    return root.querySelector<HTMLInputElement>('[data-testid="association-filter-search"]')!;
  }

  // The option list now lives in a popover panel that only renders while open (FOCUS-driven), unlike
  // the old <details> disclosure whose children stayed in the DOM even when closed.
  function openPanel(fixture: ReturnType<typeof render>): void {
    searchField(fixture.nativeElement as HTMLElement).dispatchEvent(new FocusEvent('focus'));
    fixture.detectChanges();
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

  it('opens the panel on field focus and reflects it via aria-expanded', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    const input = searchField(root);
    expect(input.getAttribute('aria-expanded')).toBe('false');
    expect(root.querySelector('[data-testid="association-filter-option-PN"]')).toBeNull();

    openPanel(fixture);

    expect(input.getAttribute('aria-expanded')).toBe('true');
    expect(root.querySelector('[data-testid="association-filter-option-PN"]')).toBeTruthy();
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
    const fixture = render();
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
