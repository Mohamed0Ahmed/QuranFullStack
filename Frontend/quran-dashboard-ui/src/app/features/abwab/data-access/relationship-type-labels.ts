import { RelationshipType } from '../../../core/api/generated/models';
import {
  RELATIONSHIP_TYPES,
  RELATIONSHIP_TYPE_BROADER_NARROWER,
  RELATIONSHIP_TYPE_OPPOSITE,
  RELATIONSHIP_TYPE_SIMILAR,
} from './abwab-relationships.port';

// The single source of the Arabic relationship-type labels, derived from the port's wire constants
// so a label and its wire value cannot drift. Never read these into a class-FIELD initialiser — see
// the getters in abwab-relationships-page.component.ts for why that captures `undefined` under the
// unit-test builder.
export const RELATIONSHIP_TYPE_LABELS: Record<RelationshipType, string> = {
  [RELATIONSHIP_TYPE_SIMILAR]: 'مشابه',
  [RELATIONSHIP_TYPE_OPPOSITE]: 'مقابل',
  [RELATIONSHIP_TYPE_BROADER_NARROWER]: 'أعم / أخص',
};

export const RELATIONSHIP_TYPE_OPTIONS = RELATIONSHIP_TYPES;

export const DIRECTIONAL_RELATIONSHIP_TYPE = RELATIONSHIP_TYPE_BROADER_NARROWER;

// The Broader/Narrower inverse is DERIVED for display only — §7.3 stores one row per relationship
// and never a second, reversed one.
export const BROADER_ENDPOINT_LABEL = 'الأعم';
export const NARROWER_ENDPOINT_LABEL = 'الأخص';
export const MUTUAL_ENDPOINT_LABEL = 'الطرف';

export function relationshipEndpointLabels(isDirectional: boolean): { from: string; to: string } {
  return isDirectional
    ? { from: BROADER_ENDPOINT_LABEL, to: NARROWER_ENDPOINT_LABEL }
    : { from: MUTUAL_ENDPOINT_LABEL, to: MUTUAL_ENDPOINT_LABEL };
}
