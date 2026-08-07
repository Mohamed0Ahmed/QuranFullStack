import { EnvironmentProviders } from '@angular/core';
import { provideAuth } from 'angular-auth-oidc-client';

export interface AccessMeContractFixture {
  sub: string;
  email: string;
  displayName: string | null;
  status: 'pending' | 'active' | 'disabled';
  isOwner: boolean;
  permissions: readonly string[];
}

interface AccessMeContractFixtures {
  pending: AccessMeContractFixture;
  readOnly: AccessMeContractFixture;
  exactPermission: AccessMeContractFixture;
  owner: AccessMeContractFixture;
}

export const ACCESS_ME_CONTRACT_FIXTURES: AccessMeContractFixtures = {
  pending: {
    sub: 'test-pending',
    email: 'pending@example.test',
    displayName: null,
    status: 'pending',
    isOwner: false,
    permissions: [],
  },
  readOnly: {
    sub: 'test-read-only',
    email: 'read-only@example.test',
    displayName: 'Read only',
    status: 'active',
    isOwner: false,
    permissions: [],
  },
  exactPermission: {
    sub: 'test-exact-permission',
    email: 'exact@example.test',
    displayName: 'Exact permission',
    status: 'active',
    isOwner: false,
    permissions: ['abwab.doors.create'],
  },
  owner: {
    sub: 'test-owner',
    email: 'owner@example.test',
    displayName: 'Owner',
    status: 'active',
    isOwner: true,
    permissions: [],
  },
};

export function provideAuthTesting(): EnvironmentProviders {
  return provideAuth({
    config: {
      authority: 'https://auth.test',
      redirectUrl: 'https://app.test/callback',
      clientId: 'test-client',
      scope: 'openid',
      responseType: 'code',
    },
  });
}
