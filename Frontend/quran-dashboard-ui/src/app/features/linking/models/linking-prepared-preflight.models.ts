import { CreateLinkingPreparedPreflightBody } from '../../../core/api/generated/models/create-linking-prepared-preflight-body';
import { LinkingPreparedPreflightStatusDto } from '../../../core/api/generated/models/linking-prepared-preflight-status-dto';

export type LinkingPreparedPreflightRequest = CreateLinkingPreparedPreflightBody;
export type LinkingPreparedPreflightStatus = LinkingPreparedPreflightStatusDto;

export function isPreparedPreflightReady(status: LinkingPreparedPreflightStatus): boolean {
  return status.status.toLowerCase() === 'ready';
}

export function isPreparedPreflightTerminal(status: LinkingPreparedPreflightStatus): boolean {
  return ['ready', 'stale', 'failed', 'cancelled', 'expired', 'confirmed'].includes(
    status.status.toLowerCase(),
  );
}
