import { expect, type Locator } from '@playwright/test';

type AriaRole = Parameters<Locator['getByRole']>[0];

export async function expectRole(target: Locator, role: AriaRole): Promise<void> {
  await expect(target).toHaveRole(role);
}

export async function expectAccessibleName(target: Locator, name: string | RegExp): Promise<void> {
  await expect(target).toHaveAccessibleName(name);
}
