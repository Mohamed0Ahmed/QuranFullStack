export type LinkingFocusOriginKind =
  | 'navbar'
  | 'workspace-row'
  | 'inline-source-action'
  | 'retained-entity-overlay';

export interface LinkingFocusOrigin {
  kind: LinkingFocusOriginKind;
  invoker: HTMLElement | null;
  fallbackSelector: string | null;
}
