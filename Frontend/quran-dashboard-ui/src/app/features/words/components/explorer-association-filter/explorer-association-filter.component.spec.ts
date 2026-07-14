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

  it('renders the label and one option button per option', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.textContent).toContain('النوع الأساسي');
    expect(root.querySelector('[data-testid="association-filter-option-PN"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="association-filter-option-N"]')).toBeTruthy();
  });

  it('emits the selected option when an option is clicked', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

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

    const input = root.querySelector<HTMLInputElement>('[data-testid="association-filter-search"]')!;
    input.value = '  علم  ';
    input.dispatchEvent(new Event('input'));

    expect(term).toBe('علم');
  });

  it('client-filters the options locally and does NOT emit searchChange when clientFilter is true', () => {
    const fixture = render({ clientFilter: true });
    const root = fixture.nativeElement as HTMLElement;

    let searchEmitted = false;
    fixture.componentInstance.searchChange.subscribe(() => (searchEmitted = true));

    const input = root.querySelector<HTMLInputElement>('[data-testid="association-filter-search"]')!;
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

    const hint = root.querySelector('[data-testid="association-filter-loading"]')!;
    expect(hint.getAttribute('role')).toBe('status');
    expect(hint.getAttribute('aria-hidden')).toBeNull();
    expect(hint.textContent).toContain(WORDS_ASSOCIATION_FILTER_LABELS.loading);
  });
});
