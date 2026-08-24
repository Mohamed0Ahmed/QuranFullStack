export type LinkingWorkspaceAddStatus = 'added' | 'already-present';

export interface LinkingWorkspaceAddResult {
  readonly sourceKey: string;
  readonly status: LinkingWorkspaceAddStatus;
}

export interface LinkingAddFeedback {
  readonly id: number;
  readonly status: LinkingWorkspaceAddStatus;
  readonly message: string;
}
