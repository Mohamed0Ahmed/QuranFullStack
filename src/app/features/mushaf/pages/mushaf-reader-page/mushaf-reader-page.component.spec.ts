import { describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';

import { MushafAyahStudyApi } from '../../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../../data-access/mushaf-pages.api';
import { MushafSurahCatalogApi } from '../../data-access/mushaf-surah-catalog.api';
import { MushafWordAnalysisApi } from '../../data-access/mushaf-word-analysis.api';
import { MushafReaderFacade } from '../../state/mushaf-reader.facade';
import { MushafReaderPageComponent } from './mushaf-reader-page.component';

describe('MushafReaderPageComponent panel semantics', () => {
  it('keeps both study sections in the DOM on wide desktop regardless of panel', () => {
    const queryParamMap$ = new BehaviorSubject(convertToParamMap({ page: '5', panel: 'word' }));

    TestBed.configureTestingModule({
      imports: [MushafReaderPageComponent],
      providers: [
        MushafReaderFacade,
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: queryParamMap$.asObservable() },
        },
        {
          provide: Router,
          useValue: { navigate: vi.fn().mockResolvedValue(true) },
        },
        {
          provide: MushafPagesApi,
          useValue: {
            getPage: vi.fn(() =>
              of({
                isSuccess: true,
                message: 'ok',
                data: {
                  pageNumber: 5,
                  previousPageNumber: 4,
                  nextPageNumber: 6,
                  surahs: [],
                  ayahRange: { firstVerseKey: '2:25', lastVerseKey: '2:26' },
                  navigation: { juzNumbers: [], hizbNumbers: [], rubNumbers: [] },
                  lines: [],
                  markers: [],
                },
              }),
            ),
          },
        },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy: vi.fn() } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
        {
          provide: MushafSurahCatalogApi,
          useValue: {
            getCatalog: vi.fn(() =>
              of({ isSuccess: true, message: 'ok', data: { surahs: [] } }),
            ),
          },
        },
      ],
    });

    const fixture: ComponentFixture<MushafReaderPageComponent> = TestBed.createComponent(
      MushafReaderPageComponent,
    );
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="selected-word-section"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="selected-ayah-section"]')).toBeTruthy();
    expect(root.querySelector('.mushaf-reader--panel-word')).toBeTruthy();
    expect(root.querySelector('.mushaf-reader__word-study--focused')).toBeTruthy();
  });
});
