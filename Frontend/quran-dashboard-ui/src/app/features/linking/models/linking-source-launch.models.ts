import { LinkingManualLinkShape } from './linking-manual-mushaf.models';
import { LinkingSourceDescriptor } from './linking-source.models';

export interface LinkingSourceInitialSelectedWord {
  readonly ayahId: number;
  readonly quranWordId: number;
}

export interface LinkingSourceInitialConfiguration {
  readonly inclusionMode: 'all-except' | 'only';
  readonly ayahOverrideIds: readonly number[];
  readonly selectedWords: readonly LinkingSourceInitialSelectedWord[];
  readonly automaticWordMatchesEnabled: boolean | null;
  readonly manualLinkShape: LinkingManualLinkShape | null;
  readonly descriptions: readonly [];
}

export interface LinkingSourceLaunch {
  readonly source: LinkingSourceDescriptor;
  readonly initialConfiguration: LinkingSourceInitialConfiguration | null;
}

export type LinkingSourceLaunchInput = LinkingSourceDescriptor | LinkingSourceLaunch;

export function createLinkingSourceLaunch(source: LinkingSourceDescriptor): LinkingSourceLaunch {
  return { source, initialConfiguration: null };
}

export function toLinkingSourceLaunch(input: LinkingSourceLaunchInput): LinkingSourceLaunch {
  return 'source' in input ? input : createLinkingSourceLaunch(input);
}
