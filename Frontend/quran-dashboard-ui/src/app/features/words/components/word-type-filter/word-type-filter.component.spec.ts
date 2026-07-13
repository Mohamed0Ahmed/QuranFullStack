import { getTestBed, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { WordTypeFilterComponent } from './word-type-filter.component';
import { WORD_TYPES_CURRENT_FILTER_LABEL, WORD_TYPES_NO_SUBTYPES_LABEL } from '../../models/word-types.labels';
import { WordTypeMainType, WordTypeTreeDto } from '../../models/word-types.models';

interface ScopeSelectedEvent {
  type: WordTypeMainType;
  childCode: string | null;
}

function captureScopeSelections(component: WordTypeFilterComponent): ScopeSelectedEvent[] {
  const emitted: ScopeSelectedEvent[] = [];
  const output = (component as unknown as {
    scopeSelected?: { subscribe: (listener: (event: ScopeSelectedEvent) => void) => void };
  }).scopeSelected;
  output?.subscribe((event) => emitted.push(event));
  return emitted;
}

const tree: WordTypeTreeDto = {
  mainTypes: [
    {
      code: 'noun', label: { ar: 'اسم' }, count: 3,
      secondaryFilter: { kind: 'case', options: [], voiceOptions: [] },
      children: [
        { code: 'N', childCode: 'N', label: { ar: 'اسم' }, count: 2 },
        { code: 'PN', childCode: 'PN', label: { ar: 'اسم علم' }, count: 1 },
      ],
    },
    {
      code: 'verb', label: { ar: 'فعل' }, count: 2,
      secondaryFilter: { kind: 'tense+voice', options: [], voiceOptions: [] },
      children: [
        { code: 'past', childCode: 'past', label: { ar: 'ماض' }, count: 1 },
      ],
    },
    {
      code: 'particle', label: { ar: 'حرف وأداة' }, count: 1,
      secondaryFilter: { kind: 'none', options: [], voiceOptions: [] },
      children: [{ code: 'PRO', childCode: 'PRO', label: { ar: 'حرف نهي' }, count: 1 }],
    },
    { code: 'inl', label: { ar: 'حروف مقطّعة' }, count: 1, secondaryFilter: { kind: 'none', options: [], voiceOptions: [] }, children: [] },
  ],
};

describe('WordTypeFilterComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({ imports: [WordTypeFilterComponent], teardown: { destroyAfterEach: true } });
  });

  afterEach(() => getTestBed().resetTestingModule());

  it('renders four main types with counts and current state', () => {
    const fixture = TestBed.createComponent(WordTypeFilterComponent);
    fixture.componentRef.setInput('tree', tree);
    fixture.componentRef.setInput('selectedType', 'verb');
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const selectedTrigger = root.querySelector('.word-type-filter__trigger.qd-is-selected') as HTMLElement;

    expect(root.textContent).toContain('اسم');
    expect(root.textContent).toContain('فعل');
    expect(root.textContent).toContain('حرف وأداة');
    expect(root.textContent).toContain('حروف مقطّعة');
    expect(root.querySelector('[aria-current="true"]')?.textContent).toContain('فعل');
    expect(selectedTrigger.textContent).toContain(WORD_TYPES_CURRENT_FILTER_LABEL);
  });

  it('browses a parent without changing the committed scope', () => {
    const fixture = TestBed.createComponent(WordTypeFilterComponent);
    fixture.componentRef.setInput('tree', tree);
    fixture.componentRef.setInput('selectedType', 'verb');
    fixture.componentRef.setInput('selectedChildCode', 'past');
    const emitted = captureScopeSelections(fixture.componentInstance);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const nounButton = root.querySelector('[data-word-type-code="noun"]') as HTMLButtonElement;
    nounButton.click();
    fixture.detectChanges();

    expect(emitted).toEqual([]);
    expect(root.querySelector('[data-word-type-code="verb"]')?.getAttribute('aria-current')).toBe('true');
    expect(nounButton.getAttribute('aria-expanded')).toBe('true');
    expect(root.textContent).toContain('اسم علم');
    expect(root.textContent).not.toContain('ماض');
    expect(root.querySelector('[data-testid="word-type-case-filter"]')).toBeNull();
    expect(root.querySelector('[data-testid="word-type-verb-filter"]')).toBeNull();
  });

  it('shows only the browsed type\'s children in the always-open panel', () => {
    const fixture = TestBed.createComponent(WordTypeFilterComponent);
    fixture.componentRef.setInput('tree', tree);
    fixture.componentRef.setInput('selectedType', 'particle');
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('حرف نهي');
    expect(root.textContent).not.toContain('اسم علم');
    expect(root.textContent).not.toContain('ماض');
    expect(root.querySelectorAll('.word-type-filter__expand').length).toBe(0);
  });

  it('commits the browsed parent together with the clicked child', () => {
    const fixture = TestBed.createComponent(WordTypeFilterComponent);
    fixture.componentRef.setInput('tree', tree);
    fixture.componentRef.setInput('selectedType', 'verb');
    fixture.componentRef.setInput('selectedChildCode', 'past');
    const emitted = captureScopeSelections(fixture.componentInstance);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('[data-word-type-code="noun"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    const childButtons = root.querySelectorAll('.word-type-filter__child-button') as NodeListOf<HTMLButtonElement>;
    childButtons[1]?.click();

    expect(emitted).toEqual([{ type: 'noun', childCode: 'PN' }]);
  });

  it('keeps inl as a directly committable childless leaf', () => {
    const fixture = TestBed.createComponent(WordTypeFilterComponent);
    fixture.componentRef.setInput('tree', tree);
    fixture.componentRef.setInput('selectedType', 'noun');
    const emitted = captureScopeSelections(fixture.componentInstance);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('[data-word-type-code="inl"]') as HTMLButtonElement).click();

    expect(emitted).toEqual([{ type: 'inl', childCode: null }]);
  });

  it('marks the selected child with aria-current and styled state', () => {
    const fixture = TestBed.createComponent(WordTypeFilterComponent);
    fixture.componentRef.setInput('tree', tree);
    fixture.componentRef.setInput('selectedType', 'noun');
    fixture.componentRef.setInput('selectedChildCode', 'PN');
    fixture.detectChanges();

    const selectedChild = fixture.nativeElement.querySelector('.word-type-filter__child-button[aria-current="true"]') as HTMLButtonElement;
    expect(selectedChild).not.toBeNull();
    expect(selectedChild.getAttribute('aria-selected')).toBe('true');
    expect(selectedChild.classList.contains('qd-is-selected')).toBe(true);
    expect(selectedChild.textContent).toContain('اسم علم');
  });

  it('shows a static placeholder for a type with no subtypes and no secondary filter', () => {
    const fixture = TestBed.createComponent(WordTypeFilterComponent);
    fixture.componentRef.setInput('tree', tree);
    fixture.componentRef.setInput('selectedType', 'inl');
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.word-type-filter__panel')).not.toBeNull();
    expect(root.querySelector('.word-type-filter__placeholder')?.textContent).toContain(WORD_TYPES_NO_SUBTYPES_LABEL);
  });

  it('focuses the active type button for focus return', () => {
    const fixture = TestBed.createComponent(WordTypeFilterComponent);
    fixture.componentRef.setInput('tree', tree);
    fixture.componentRef.setInput('selectedType', 'verb');
    fixture.detectChanges();

    fixture.componentInstance.focusSelectedType();

    const selected = fixture.nativeElement.querySelector('.word-type-filter__button[aria-current="true"]') as HTMLButtonElement;
    expect(document.activeElement).toBe(selected);
  });

  describe('secondary filter visibility', () => {
    it('renders case controls only for the noun type', () => {
      const fixture = TestBed.createComponent(WordTypeFilterComponent);
      fixture.componentRef.setInput('tree', tree);
      fixture.componentRef.setInput('selectedType', 'noun');
      fixture.detectChanges();

      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="word-type-case-filter"]')).not.toBeNull();
      expect(root.querySelector('[data-testid="word-type-verb-filter"]')).toBeNull();
    });

    it('renders tense/voice controls only for the verb type', () => {
      const fixture = TestBed.createComponent(WordTypeFilterComponent);
      fixture.componentRef.setInput('tree', tree);
      fixture.componentRef.setInput('selectedType', 'verb');
      fixture.detectChanges();

      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="word-type-verb-filter"]')).not.toBeNull();
      expect(root.querySelector('[data-testid="word-type-case-filter"]')).toBeNull();
    });

    it('renders no secondary controls for particle or inl', () => {
      const particleFixture = TestBed.createComponent(WordTypeFilterComponent);
      particleFixture.componentRef.setInput('tree', tree);
      particleFixture.componentRef.setInput('selectedType', 'particle');
      particleFixture.detectChanges();

      const particleRoot = particleFixture.nativeElement as HTMLElement;
      expect(particleRoot.querySelector('[data-testid="word-type-case-filter"]')).toBeNull();
      expect(particleRoot.querySelector('[data-testid="word-type-verb-filter"]')).toBeNull();

      const inlFixture = TestBed.createComponent(WordTypeFilterComponent);
      inlFixture.componentRef.setInput('tree', tree);
      inlFixture.componentRef.setInput('selectedType', 'inl');
      inlFixture.detectChanges();

      const inlRoot = inlFixture.nativeElement as HTMLElement;
      expect(inlRoot.querySelector('[data-testid="word-type-case-filter"]')).toBeNull();
      expect(inlRoot.querySelector('[data-testid="word-type-verb-filter"]')).toBeNull();
    });
  });

  describe('secondary filter emission', () => {
    it('emits the selected case when the noun case select changes', () => {
      const fixture = TestBed.createComponent(WordTypeFilterComponent);
      const emitted: string[] = [];
      fixture.componentRef.setInput('tree', tree);
      fixture.componentRef.setInput('selectedType', 'noun');
      fixture.componentInstance.caseSelected.subscribe((value) => emitted.push(value));
      fixture.detectChanges();

      const select = fixture.nativeElement.querySelector(
        '[data-testid="word-type-case-filter"] select',
      ) as HTMLSelectElement;
      select.value = 'genitive';
      select.dispatchEvent(new Event('change'));

      expect(emitted).toEqual(['genitive']);
    });

    it('emits the selected tense when the verb tense select changes', () => {
      const fixture = TestBed.createComponent(WordTypeFilterComponent);
      const emitted: string[] = [];
      fixture.componentRef.setInput('tree', tree);
      fixture.componentRef.setInput('selectedType', 'verb');
      fixture.componentInstance.tenseSelected.subscribe((value) => emitted.push(value));
      fixture.detectChanges();

      const tenseSelect = (fixture.nativeElement.querySelectorAll(
        '[data-testid="word-type-verb-filter"] select',
      )[0]) as HTMLSelectElement;
      tenseSelect.value = 'past';
      tenseSelect.dispatchEvent(new Event('change'));

      expect(emitted).toEqual(['past']);
    });

    it('emits the selected voice when the verb voice select changes', () => {
      const fixture = TestBed.createComponent(WordTypeFilterComponent);
      const emitted: string[] = [];
      fixture.componentRef.setInput('tree', tree);
      fixture.componentRef.setInput('selectedType', 'verb');
      fixture.componentInstance.voiceSelected.subscribe((value) => emitted.push(value));
      fixture.detectChanges();

      const voiceSelect = (fixture.nativeElement.querySelectorAll(
        '[data-testid="word-type-verb-filter"] select',
      )[1]) as HTMLSelectElement;
      voiceSelect.value = 'passive';
      voiceSelect.dispatchEvent(new Event('change'));

      expect(emitted).toEqual(['passive']);
    });
  });
});
