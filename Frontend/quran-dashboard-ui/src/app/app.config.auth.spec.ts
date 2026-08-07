import { describe, expect, it } from 'vitest';

import { oidcConfig } from './app.config';

describe('OIDC app config', () => {
  it('requests verified-email identity claims through the OIDC email scope', () => {
    expect(oidcConfig.scope?.split(/\s+/)).toContain('email');
  });

  it('opts out of the library auto-navigate so /callback activates and provisioning runs', () => {
    expect(oidcConfig.triggerAuthorizationResultEvent).toBe(true);
  });
});
