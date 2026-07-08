import { getTestBed, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { WordTypesTableComponent } from './word-types-table.component';
import { PagedResultDto, WordTypeRowDto } from '../../models/word-types.models';

function row(overrides: Partial<WordTypeRowDto> = {}): WordTypeRowDto {
  return {
    tashkeelWordId: 191001,
    contextCode: 'PN',
    case: 'all',
    tense: 'all',
    voice: 'all',
    displayText: 'كَلِمَة',
    typeCode: 'PN',
    typeLabel: { ar: 'اسم علم' },
    broadLabel: { ar: 'اسم' },
    caseOrFeature: null,
    rootText: 'ك ل م',
    lemmaText: null,
    stemText: null,
    occurrencesCount: 2,
    ayahsCount: 2,
    surahsCount: 1,
    ...overrides,
  };
}

const page: PagedResultDto<WordTypeRowDto> = {
  page: 1,
  pageSize: 25,
  totalCount: 1,
  items: [row()],
};

describe('WordTypesTableComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({ imports: [WordTypesTableComponent], teardown: { destroyAfterEach: true } });
  });

  afterEach(() => getTestBed().resetTestingModule());

  it('renders required columns, Uthmani display, and neutral null placeholders', () => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    fixture.componentRef.setInput('rows', page);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((header) => header.textContent?.trim());
    expect(headers).toEqual(['الكلمة', 'النوع', 'الجذر', 'الصيغة', 'الأصل', 'المواضع', 'الآيات', 'السور']);
    expect(root.textContent).toContain('كَلِمَة');
    expect(root.textContent).toContain('—');
    expect(root.textContent).not.toContain('191001');
    expect(root.querySelector('.word-types-table__header-gutter')).not.toBeNull();
  });

  it('emits selected row and marks active row beyond color', () => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    const emitted: WordTypeRowDto[] = [];
    fixture.componentRef.setInput('rows', page);
    fixture.componentRef.setInput('selectedRow', row());
    fixture.componentInstance.rowSelected.subscribe((selected) => emitted.push(selected));
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('.word-types-table__row') as HTMLButtonElement;
    expect(button.getAttribute('aria-current')).toBe('true');
    expect(button.getAttribute('aria-selected')).toBe('true');
    expect(button.classList.contains('qd-is-selected')).toBe(true);
    button.click();

    expect(emitted[0].contextCode).toBe('PN');
  });

  it('renders a skeleton body while loading, even when prior rows exist, matching the other word tables', () => {
    const loadingFixture = TestBed.createComponent(WordTypesTableComponent);
    loadingFixture.componentRef.setInput('loading', true);
    loadingFixture.detectChanges();

    const loadingRoot = loadingFixture.nativeElement as HTMLElement;
    expect(loadingRoot.querySelector('[data-testid="word-types-table-loading"]')).not.toBeNull();
    expect(loadingRoot.querySelector('.word-types-table__row--loading')).not.toBeNull();

    const refreshFixture = TestBed.createComponent(WordTypesTableComponent);
    refreshFixture.componentRef.setInput('rows', page);
    refreshFixture.componentRef.setInput('loading', true);
    refreshFixture.detectChanges();

    const refreshRoot = refreshFixture.nativeElement as HTMLElement;
    expect(refreshRoot.querySelector('[data-testid="word-types-table-loading"]')).not.toBeNull();
    expect(refreshRoot.querySelector('[data-word-types-row]')).toBeNull();
  });

  it('focuses a row by identity for focus return', () => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    fixture.componentRef.setInput('rows', page);
    fixture.detectChanges();

    fixture.componentInstance.focusRow(row());

    expect(document.activeElement).toBe(fixture.nativeElement.querySelector('.word-types-table__row'));
  });
});
