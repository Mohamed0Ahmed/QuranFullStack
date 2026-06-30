import { getTestBed, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { WordTypeFilterComponent } from './word-type-filter.component';
import { WordTypeTreeDto } from '../../models/word-types.models';

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
    { code: 'particle', label: { ar: 'حرف وأداة' }, count: 1, secondaryFilter: { kind: 'none', options: [], voiceOptions: [] }, children: [] },
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
    expect(root.textContent).toContain('اسم');
    expect(root.textContent).toContain('فعل');
    expect(root.textContent).toContain('حرف وأداة');
    expect(root.textContent).toContain('حروف مقطّعة');
    expect(root.querySelector('[aria-current="true"]')?.textContent).toContain('فعل');
  });

  it('emits selected main type from keyboard-operable buttons', () => {
    const fixture = TestBed.createComponent(WordTypeFilterComponent);
    const emitted: string[] = [];
    fixture.componentRef.setInput('tree', tree);
    fixture.componentInstance.typeSelected.subscribe((type) => emitted.push(type));
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll('.word-type-filter__button') as NodeListOf<HTMLButtonElement>;
    buttons[2].click();

    expect(emitted).toEqual(['particle']);
  });

  it('hides child nodes and expand affordance until a parent is expanded', () => {
    const fixture = TestBed.createComponent(WordTypeFilterComponent);
    fixture.componentRef.setInput('tree', tree);
    fixture.componentRef.setInput('selectedType', 'particle');
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).not.toContain('اسم علم');
    // Only the two parents with children (noun, verb) render an expand button; particle and inl do not.
    expect(root.querySelectorAll('.word-type-filter__expand').length).toBe(2);
  });

  it('expands the active parent to reveal children and emits the selected child code', () => {
    const fixture = TestBed.createComponent(WordTypeFilterComponent);
    const emitted: (string | null)[] = [];
    fixture.componentRef.setInput('tree', tree);
    fixture.componentRef.setInput('selectedType', 'noun');
    fixture.componentRef.setInput('selectedChildCode', null);
    fixture.componentInstance.childSelected.subscribe((code) => emitted.push(code));
    fixture.detectChanges();

    // noun is the active parent, so its children are visible without an explicit toggle.
    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('اسم علم');

    const childButtons = root.querySelectorAll('.word-type-filter__child-button') as NodeListOf<HTMLButtonElement>;
    childButtons[1].click();

    expect(emitted).toEqual(['PN']);
  });

  it('marks the selected child with aria-current and styled state', () => {
    const fixture = TestBed.createComponent(WordTypeFilterComponent);
    fixture.componentRef.setInput('tree', tree);
    fixture.componentRef.setInput('selectedType', 'noun');
    fixture.componentRef.setInput('selectedChildCode', 'PN');
    fixture.detectChanges();

    const selectedChild = fixture.nativeElement.querySelector('.word-type-filter__child-button[aria-current="true"]') as HTMLButtonElement;
    expect(selectedChild).not.toBeNull();
    expect(selectedChild.textContent).toContain('اسم علم');
  });

  it('toggles expand state via the expand affordance and exposes aria-expanded', () => {
    const fixture = TestBed.createComponent(WordTypeFilterComponent);
    fixture.componentRef.setInput('tree', tree);
    fixture.componentRef.setInput('selectedType', 'inl');
    fixture.detectChanges();

    const expandButtons = fixture.nativeElement.querySelectorAll('.word-type-filter__expand') as NodeListOf<HTMLButtonElement>;
    // Expand the verb parent (which is not the active type).
    const verbExpand = Array.from(expandButtons).find((button) => button.getAttribute('aria-label')?.includes('فعل'))!;
    expect(verbExpand.getAttribute('aria-expanded')).toBe('false');

    verbExpand.click();
    fixture.detectChanges();

    expect(verbExpand.getAttribute('aria-expanded')).toBe('true');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('ماض');
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
