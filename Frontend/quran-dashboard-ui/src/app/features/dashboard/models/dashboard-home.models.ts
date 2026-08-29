import type { UrlTree } from '@angular/router';

import type { NavItem } from '../../../core/navigation/nav-items';

export const DASHBOARD_SECTION_KEYS = [
  'hero',
  'mushaf',
  'words',
  'phrases',
  'linking',
  'abwab',
  'entry',
] as const;

export type DashboardSectionKey = (typeof DASHBOARD_SECTION_KEYS)[number];
export type DashboardResearchSectionKey = Exclude<DashboardSectionKey, 'hero' | 'entry'>;
export type DashboardLinkTarget = string | UrlTree;

export interface DashboardRouteAction {
  readonly label: string;
  readonly route: string;
}

export interface DashboardNavigationDefinition {
  readonly label: string;
  readonly description?: string;
  readonly navigationItem: NavItem;
}

export interface DashboardNavigationLink {
  readonly label: string;
  readonly description?: string;
  readonly target: DashboardLinkTarget;
}

export interface DashboardHeroContent {
  readonly sectionKey: Extract<DashboardSectionKey, 'hero'>;
  readonly title: string;
  readonly ayah: string;
  readonly description: string;
  readonly mushafActionLabel: string;
  readonly workflowActionLabel: string;
  readonly searchAction: DashboardRouteAction;
}

export interface DashboardHeroDefinition {
  readonly content: DashboardHeroContent;
  readonly mushafNavigationItem: NavItem;
}

export interface DashboardWorkflowStep {
  readonly key: DashboardResearchSectionKey;
  readonly label: string;
}

export interface DashboardStudyTab {
  readonly key: 'analysis' | 'abwab' | 'sources' | 'similarity';
  readonly label: string;
  readonly description: string;
}

export interface DashboardMushafContent {
  readonly heading: string;
  readonly description: string;
  readonly tabs: readonly DashboardStudyTab[];
  readonly actionLabel: string;
}

export interface DashboardWordAction extends DashboardRouteAction {
  readonly description: string;
}

export interface DashboardWordContent {
  readonly heading: string;
  readonly description: string;
  readonly sequence: readonly DashboardWordAction[];
  readonly overviewAction: DashboardRouteAction;
  readonly typesAction: DashboardWordAction;
}

export interface DashboardPhraseTab extends DashboardRouteAction {
  readonly key: 'repetitions' | 'context' | 'similarity';
  readonly description: string;
  readonly tokens: readonly string[];
}

export interface DashboardPhraseContent {
  readonly heading: string;
  readonly description: string;
  readonly tabs: readonly DashboardPhraseTab[];
}

export interface DashboardLinkingStage {
  readonly label: string;
  readonly description: string;
}

export interface DashboardLinkingContent {
  readonly heading: string;
  readonly description: string;
  readonly stages: readonly DashboardLinkingStage[];
  readonly mushafActionLabel: string;
  readonly similarityAction: DashboardRouteAction;
}

export interface DashboardAbwabContent {
  readonly heading: string;
  readonly description: string;
  readonly branchLabel: string;
  readonly childLabels: readonly string[];
  readonly abwabAction: DashboardRouteAction;
  readonly templatesAction: DashboardRouteAction;
}
