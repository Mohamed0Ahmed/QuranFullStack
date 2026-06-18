import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';

import { MushafPageAreaComponent } from '../../components/mushaf-page-area/mushaf-page-area.component';
import { SelectedAyahSectionComponent } from '../../components/selected-ayah-section/selected-ayah-section.component';
import { SelectedWordSectionComponent } from '../../components/selected-word-section/selected-word-section.component';
import { AyahStudyTab, MUSHAF_URL_KEYS, WordAnalysisTab } from '../../models/mushaf.models';
import { MushafReaderFacade } from '../../state/mushaf-reader.facade';

const AYAH_TABS: ReadonlySet<string> = new Set(['tafsir', 'translation', 'full-i3rab']);
const WORD_TABS: ReadonlySet<string> = new Set(['morphology', 'segments', 'i3rab', 'identity']);

function parseAyahTab(value: string | null): AyahStudyTab | null {
  if (value && AYAH_TABS.has(value)) {
    return value as AyahStudyTab;
  }
  return null;
}

function parseWordTab(value: string | null): WordAnalysisTab | null {
  if (value && WORD_TABS.has(value)) {
    return value as WordAnalysisTab;
  }
  return null;
}

@Component({
  selector: 'qd-mushaf-reader-page',
  standalone: true,
  imports: [
    CommonModule,
    MushafPageAreaComponent,
    SelectedWordSectionComponent,
    SelectedAyahSectionComponent,
  ],
  templateUrl: './mushaf-reader-page.component.html',
  styleUrls: ['./mushaf-reader-page.component.scss'],
})
export class MushafReaderPageComponent implements OnInit {
  protected readonly facade = inject(MushafReaderFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.facade.loadSurahCatalog();

    this.route.queryParamMap.subscribe((params) => {
      const rawPage = Number(params.get(MUSHAF_URL_KEYS.page) ?? '1');
      const pageNumber = Number.isFinite(rawPage)
        ? Math.min(604, Math.max(1, rawPage))
        : 1;
      this.facade.loadPage(pageNumber);

      this.facade.applyUrlState({
        ayah: params.get(MUSHAF_URL_KEYS.ayah),
        tafsirSource: params.get(MUSHAF_URL_KEYS.tafsirSource),
        translationSource: params.get(MUSHAF_URL_KEYS.translationSource),
        fullI3rabSource: params.get(MUSHAF_URL_KEYS.fullI3rabSource),
        ayahTab: parseAyahTab(params.get(MUSHAF_URL_KEYS.ayahTab)),
        word: params.get(MUSHAF_URL_KEYS.word),
        segment: params.get(MUSHAF_URL_KEYS.segment),
        wordTab: parseWordTab(params.get(MUSHAF_URL_KEYS.wordTab)),
      });
    });
  }

  protected onPageChange(pageNumber: number): void {
    this.navigateQueryParams({ [MUSHAF_URL_KEYS.page]: pageNumber });
  }

  protected onSurahJump(surahNumber: number): void {
    const startPage = this.facade.resolveSurahStartPage(surahNumber);
    if (startPage !== null) {
      this.navigateQueryParams({ [MUSHAF_URL_KEYS.page]: startPage });
    }
  }

  protected onAyahSelect(verseKey: string): void {
    this.navigateQueryParams({
      [MUSHAF_URL_KEYS.ayah]: verseKey,
      [MUSHAF_URL_KEYS.panel]: 'ayah',
    });
  }

  protected onWordSelect(wordLocation: string): void {
    this.navigateQueryParams({
      [MUSHAF_URL_KEYS.word]: wordLocation,
      [MUSHAF_URL_KEYS.panel]: 'word',
    });
  }

  protected onAyahTabChange(tab: AyahStudyTab): void {
    this.navigateQueryParams({ [MUSHAF_URL_KEYS.ayahTab]: tab });
  }

  protected onWordTabChange(tab: WordAnalysisTab): void {
    this.navigateQueryParams({ [MUSHAF_URL_KEYS.wordTab]: tab });
  }

  protected onTafsirSourceChange(sourceKey: string): void {
    this.navigateQueryParams({ [MUSHAF_URL_KEYS.tafsirSource]: sourceKey });
  }

  protected onTranslationSourceChange(sourceKey: string): void {
    this.navigateQueryParams({ [MUSHAF_URL_KEYS.translationSource]: sourceKey });
  }

  protected onFullI3rabSourceChange(sourceKey: string): void {
    this.navigateQueryParams({ [MUSHAF_URL_KEYS.fullI3rabSource]: sourceKey });
  }

  private navigateQueryParams(
    params: Partial<Record<(typeof MUSHAF_URL_KEYS)[keyof typeof MUSHAF_URL_KEYS], string | number>>,
  ): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: params,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
