import { getTestBed, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { WordTypeFilterComponent } from './word-type-filter.component';
import { WordTypeTreeDto } from '../../models/word-types.models';

const tree: WordTypeTreeDto = {
  mainTypes: [
    { code: 'noun', label: { ar: 'اسم' }, count: 3, secondaryFilter: { kind: 'case', options: [], voiceOptions: [] }, children: [] },
    { code: 'verb', label: { ar: 'فعل' }, count: 2, secondaryFilter: { kind: 'tense+voice', options: [], voiceOptions: [] }, children: [] },
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

    const buttons = fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>;
    buttons[2].click();

    expect(emitted).toEqual(['particle']);
  });
});
