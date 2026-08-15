import { LinkingPreparedAyahOverlayDto } from '../../../core/api/generated/models/linking-prepared-ayah-overlay-dto';
import { LinkingSourcePageViewBody } from '../../../core/api/generated/models/linking-source-page-view-body';
import { LinkingSourceDescriptor } from './linking-source.models';

export interface LinkingSourcePageRequest {
  source: LinkingSourceDescriptor;
  expectedLinkingDataRevision: number | null;
  expectedSourceViewIdentity: string | null;
  view: LinkingSourcePageViewBody;
  page: number;
  pageSize: number;
  draftGeneration: number;
}

export interface LinkingSourcePage {
  linkingDataRevision: number;
  resolutionIdentity: string;
  sourceViewIdentity: string;
  page: number;
  pageSize: number;
  totalAyahCount: number;
  totalPages: number;
  ayahIds: readonly number[];
  wordIdsByAyahId: Readonly<Record<number, readonly number[]>>;
  matchedWordIdsByAyahId: Readonly<Record<number, readonly number[]>>;
  weight: number;
}

export type LinkingPreparedDetailKind = 'source' | 'merged';

export interface LinkingPreparedDetailRequest {
  linkingDataRevision: number;
  preflightId: string;
  detailKind: LinkingPreparedDetailKind;
  preparedSourceId: number | null;
  filter: string;
  page: number;
  pageSize: number;
  generation: number;
}

export interface LinkingPreparedDetailPage {
  linkingDataRevision: number;
  preflightId: string;
  detailKind: string;
  preparedSourceId: number | null;
  filter: string;
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  ayahIds: readonly number[];
  wordIdsByAyahId: Readonly<Record<number, readonly number[]>>;
  overlaysByAyahId: Readonly<Record<number, readonly LinkingPreparedAyahOverlayDto[]>>;
  weight: number;
}

export interface LinkingPageRange<TPage> {
  pages: readonly TPage[];
  release(): void;
}
