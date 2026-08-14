import { LinkingAyah } from './linking-ayah.models';

export interface LinkingResolvedSourceRevision {
  ayahs: readonly LinkingAyah[];
  linkingDataRevision: number;
}

export class LinkingDataStaleError extends Error {
  constructor(message: string) {
    super(message);
  }
}

export class LinkingLifecycleError extends Error {
  constructor(
    readonly code: string,
    message: string,
  ) {
    super(message);
  }
}
