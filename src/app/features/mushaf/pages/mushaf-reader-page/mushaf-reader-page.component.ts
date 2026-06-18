import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';

import { MushafPageAreaComponent } from '../../components/mushaf-page-area/mushaf-page-area.component';
import { SelectedAyahSectionComponent } from '../../components/selected-ayah-section/selected-ayah-section.component';
import { SelectedWordSectionComponent } from '../../components/selected-word-section/selected-word-section.component';
import { AyahStudyTab, PanelMode } from '../../models/mushaf.models';
import { MushafReaderFacade } from '../../state/mushaf-reader.facade';

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

  ngOnInit(): void {
    this.facade.loadSurahCatalog();
    this.facade.bindToRoute(this.route);
  }

  protected panelClass(panel: PanelMode): string {
    return `mushaf-reader--panel-${panel}`;
  }

  protected isPanelFocused(section: 'ayah' | 'word'): boolean {
    const panel = this.facade.panel();
    return panel === section;
  }

  protected onPageChange(pageNumber: number): void {
    this.facade.changePage(pageNumber);
  }

  protected onSurahJump(surahNumber: number): void {
    this.facade.jumpToSurah(surahNumber);
  }

  protected onAyahSelect(verseKey: string): void {
    this.facade.selectAyah(verseKey);
  }

  protected onWordSelect(wordLocation: string): void {
    this.facade.selectWord(wordLocation);
  }

  protected onAyahTabChange(tab: AyahStudyTab): void {
    this.facade.setAyahTab(tab);
  }

  protected onTafsirSourceChange(sourceKey: string): void {
    this.facade.setTafsirSource(sourceKey);
  }

  protected onTranslationSourceChange(sourceKey: string): void {
    this.facade.setTranslationSource(sourceKey);
  }

  protected onFullI3rabSourceChange(sourceKey: string): void {
    this.facade.setFullI3rabSource(sourceKey);
  }

  protected onStudySectionFocus(section: 'ayah' | 'word'): void {
    this.facade.setPanel(section);
  }
}
