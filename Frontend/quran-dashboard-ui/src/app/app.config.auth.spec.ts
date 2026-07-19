import { describe, expect, it } from 'vitest';

import { oidcConfig } from './app.config';

describe('OIDC app config', () => {
  it('opts out of the library auto-navigate so /callback activates and provisioning runs', () => {
    expect(oidcConfig.triggerAuthorizationResultEvent).toBe(true);
  });
});
