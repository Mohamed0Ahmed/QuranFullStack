import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';

import { NavigationResumeService } from '../../../../core/navigation/navigation-resume.service';
import { DashboardAbwabPreviewComponent } from '../../components/dashboard-abwab-preview/dashboard-abwab-preview.component';
import { DashboardEntrySectionComponent } from '../../components/dashboard-entry-section/dashboard-entry-section.component';
import { DashboardHeroComponent } from '../../components/dashboard-hero/dashboard-hero.component';
import { DashboardLinkingFlowComponent } from '../../components/dashboard-linking-flow/dashboard-linking-flow.component';
import { DashboardMushafPreviewComponent } from '../../components/dashboard-mushaf-preview/dashboard-mushaf-preview.component';
import { DashboardPhrasePreviewComponent } from '../../components/dashboard-phrase-preview/dashboard-phrase-preview.component';
import { DashboardResumeStripComponent } from '../../components/dashboard-resume-strip/dashboard-resume-strip.component';
import { DashboardWordStructureComponent } from '../../components/dashboard-word-structure/dashboard-word-structure.component';
import { DashboardWorkflowRailComponent } from '../../components/dashboard-workflow-rail/dashboard-workflow-rail.component';
import { DashboardSectionObserverDirective } from '../../directives/dashboard-section-observer.directive';
import {
  DASHBOARD_ENTRY_HEADING,
  DASHBOARD_ENTRY_ITEMS,
  DASHBOARD_HERO,
  DASHBOARD_RESUME_ITEMS,
  DASHBOARD_WORKFLOW_STEPS,
} from '../../models/dashboard-home.content';
import type {
  DashboardNavigationDefinition,
  DashboardNavigationLink,
  DashboardResearchSectionKey,
  DashboardSectionKey,
} from '../../models/dashboard-home.models';
import {
  DASHBOARD_ABWAB_CONTENT,
  DASHBOARD_LINKING_CONTENT,
  DASHBOARD_MUSHAF_CONTENT,
  DASHBOARD_PHRASE_CONTENT,
  DASHBOARD_WORD_CONTENT,
} from '../../models/dashboard-research.content';

@Component({
  selector: 'qd-dashboard-home',
  standalone: true,
  imports: [
    DashboardHeroComponent,
    DashboardResumeStripComponent,
    DashboardWorkflowRailComponent,
    DashboardMushafPreviewComponent,
    DashboardWordStructureComponent,
    DashboardPhrasePreviewComponent,
    DashboardLinkingFlowComponent,
    DashboardAbwabPreviewComponent,
    DashboardEntrySectionComponent,
    DashboardSectionObserverDirective,
  ],
  templateUrl: './dashboard-home.component.html',
  styleUrl: './dashboard-home.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardHomeComponent {
  private readonly document = inject(DOCUMENT);
  private readonly navigationResume = inject(NavigationResumeService);

  protected readonly hero = DASHBOARD_HERO.content;
  protected readonly mushafTarget = this.navigationResume.targetFor(
    DASHBOARD_HERO.mushafNavigationItem,
  );
  protected readonly resumeItems = this.resolveNavigationLinks(DASHBOARD_RESUME_ITEMS);
  protected readonly workflowSteps = DASHBOARD_WORKFLOW_STEPS;
  protected readonly entryHeading = DASHBOARD_ENTRY_HEADING;
  protected readonly entryItems = this.resolveNavigationLinks(DASHBOARD_ENTRY_ITEMS);
  protected readonly mushafContent = DASHBOARD_MUSHAF_CONTENT;
  protected readonly wordContent = DASHBOARD_WORD_CONTENT;
  protected readonly phraseContent = DASHBOARD_PHRASE_CONTENT;
  protected readonly linkingContent = DASHBOARD_LINKING_CONTENT;
  protected readonly abwabContent = DASHBOARD_ABWAB_CONTENT;
  protected readonly activeSection = signal<DashboardResearchSectionKey>('mushaf');

  protected onSectionActive(key: DashboardSectionKey): void {
    if (key !== 'hero' && key !== 'entry') {
      this.activeSection.set(key);
    }
  }

  protected scrollToSection(key: DashboardResearchSectionKey): void {
    const section = this.document.getElementById(key);
    if (section === null) {
      return;
    }

    const reduceMotion = this.document.defaultView?.matchMedia(
      '(prefers-reduced-motion: reduce)',
    ).matches;
    section.scrollIntoView({ behavior: reduceMotion ? 'auto' : 'smooth', block: 'start' });
    this.activeSection.set(key);
  }

  private resolveNavigationLinks(
    definitions: readonly DashboardNavigationDefinition[],
  ): readonly DashboardNavigationLink[] {
    return definitions.map((definition) => ({
      label: definition.label,
      description: definition.description,
      target: this.navigationResume.targetFor(definition.navigationItem),
    }));
  }
}
