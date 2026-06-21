import { Component, HostListener, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';

import { MushafPageAreaComponent } from '../../components/mushaf-page-area/mushaf-page-area.component';
import { StudyContextSectionComponent } from '../../components/study-context-section/study-context-section.component';
import { AyahStudyTab, AyahNavigationTarget } from '../../models/mushaf.models';
import { MushafReaderFacade } from '../../state/mushaf-reader.facade';

@Component({
  selector: 'qd-mushaf-reader-page',
  standalone: true,
  imports: [CommonModule, MushafPageAreaComponent, StudyContextSectionComponent],
  templateUrl: './mushaf-reader-page.component.html',
  styleUrls: ['./mushaf-reader-page.component.scss'],
})
export class MushafReaderPageComponent implements OnInit, OnDestroy {
  protected readonly facade = inject(MushafReaderFacade);
  private readonly route = inject(ActivatedRoute);

  @HostListener('document:keydown', ['$event'])
  protected onDocumentKeydown(event: KeyboardEvent): void {
    if (!this.shouldHandleWordNavigation(event)) {
      return;
    }

    if (event.key === 'ArrowLeft') {
      if (this.facade.moveSelectedWord('next')) {
        event.preventDefault();
      }
      return;
    }

    if (event.key === 'ArrowRight') {
      if (this.facade.moveSelectedWord('previous')) {
        event.preventDefault();
      }
    }
  }

  ngOnInit(): void {
    this.facade.loadStudySourceCatalog();
    this.facade.bindToRoute(this.route);
  }

  ngOnDestroy(): void {
    this.facade.unbindFromRoute();
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

  protected onAyahNavigate(target: AyahNavigationTarget): void {
    this.facade.viewAyahOnPage(target.verseKey, target.pageNumber);
  }

  private shouldHandleWordNavigation(event: KeyboardEvent): boolean {
    if (event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) {
      return false;
    }

    const target = event.target;
    if (!(target instanceof HTMLElement)) {
      return true;
    }

    if (target.isContentEditable) {
      return false;
    }

    return !['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName);
  }
}
