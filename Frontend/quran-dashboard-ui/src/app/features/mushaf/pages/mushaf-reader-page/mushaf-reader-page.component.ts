import {
  Component,
  HostListener,
  OnDestroy,
  OnInit,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';

import { MushafPageAreaComponent } from '../../components/mushaf-page-area/mushaf-page-area.component';
import {
  StudyContextSectionComponent,
  StudyContextTab,
} from '../../components/study-context-section/study-context-section.component';
import { MushafSelectionStatusComponent } from '../../../linking/components/mushaf-selection-status/mushaf-selection-status.component';
import { AyahStudyTab, AyahNavigationTarget } from '../../models/mushaf.models';
import { MushafDoorDetailsRequest } from '../../models/mushaf-door-highlights.models';
import { MushafReaderFacade } from '../../state/mushaf-reader.facade';
import { MushafDoorsHighlightStore } from '../../state/mushaf-doors-highlight.store';
import { LinkingAccessService } from '../../../linking/state/linking-access.service';
import { ManualMushafSelectionStore } from '../../../linking/state/manual-mushaf-selection.store';
import { QdContextMenuComponent } from '../../../../shared/ui/context-menu/context-menu.component';

@Component({
  selector: 'qd-mushaf-reader-page',
  standalone: true,
  imports: [
    CommonModule,
    MushafPageAreaComponent,
    MushafSelectionStatusComponent,
    QdContextMenuComponent,
    StudyContextSectionComponent,
  ],
  templateUrl: './mushaf-reader-page.component.html',
  styleUrls: ['./mushaf-reader-page.component.scss'],
  providers: [MushafDoorsHighlightStore],
})
export class MushafReaderPageComponent implements OnInit, OnDestroy {
  protected readonly facade = inject(MushafReaderFacade);
  protected readonly linkingAccess = inject(LinkingAccessService);
  protected readonly ayahSelection = inject(ManualMushafSelectionStore);
  protected readonly doorHighlights = inject(MushafDoorsHighlightStore);
  protected readonly doorDetails = signal<MushafDoorDetailsRequest | null>(null);
  private readonly route = inject(ActivatedRoute);
  private readonly pageArea = viewChild(MushafPageAreaComponent);
  private readonly selectionStatus = viewChild(MushafSelectionStatusComponent);
  private restoreSelectionActionFocus = false;
  private ignoreNextWordSelection = false;

  constructor() {
    effect(() => {
      const pageNumber = this.facade.page()?.pageNumber ?? null;
      this.ayahSelection.setCurrentPage(pageNumber);
      this.doorHighlights.setPage(pageNumber);

      if (!this.restoreSelectionActionFocus || pageNumber === null) {
        return;
      }

      if (!this.selectionStatus()?.isFocusOwner()) {
        this.restoreSelectionActionFocus = false;
        return;
      }

      this.restoreSelectionActionFocus = false;
      queueMicrotask(() => this.pageArea()?.focusAyahSelectionAction());
    });
  }

  @HostListener('document:keydown', ['$event'])
  protected onDocumentKeydown(event: KeyboardEvent): void {
    if (this.ayahSelection.active() && (event.key === 'ArrowLeft' || event.key === 'ArrowRight')) {
      return;
    }

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
    this.ayahSelection.reset();
    this.facade.unbindFromRoute();
  }

  protected onPageChange(pageNumber: number): void {
    this.preserveSelectionFocusBeforePageChange();
    this.facade.changePage(pageNumber);
  }

  protected onSurahJump(surahNumber: number): void {
    this.preserveSelectionFocusBeforePageChange();
    this.facade.jumpToSurah(surahNumber);
  }

  protected onAyahSelect(verseKey: string): void {
    if (!this.ayahSelection.active()) {
      return;
    }

    this.ignoreNextWordSelection = true;
    this.ayahSelection.toggle(verseKey);
  }

  protected onWordSelect(wordLocation: string): void {
    if (this.ignoreNextWordSelection) {
      this.ignoreNextWordSelection = false;
      return;
    }

    if (this.ayahSelection.active()) {
      return;
    }

    this.facade.selectWord(wordLocation);
  }

  protected openDoorDetails(request: MushafDoorDetailsRequest): void {
    this.doorDetails.set(request);
  }

  protected closeDoorDetails(): void {
    this.doorDetails.set(null);
  }

  protected toggleAyahSelectionMode(): void {
    if (this.ayahSelection.active()) {
      this.ayahSelection.cancel();
      return;
    }

    this.ayahSelection.activate();
  }

  protected onAyahTabChange(tab: AyahStudyTab): void {
    this.facade.setAyahTab(tab);
  }

  protected onStudyContextTabChange(tab: StudyContextTab): void {
    switch (tab) {
      case 'analysis':
        this.facade.setPanel('word');
        return;
      case 'sources':
        this.facade.setAyahTab('tafsir');
        return;
      case 'similarity':
        this.facade.setAyahTab('mutashabihat');
        return;
      case 'doors':
        this.facade.setPanel('doors');
        return;
    }
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

  private preserveSelectionFocusBeforePageChange(): void {
    if (!this.ayahSelection.active()) {
      return;
    }

    this.restoreSelectionActionFocus = true;
    this.selectionStatus()?.focusOwner();
  }
}
