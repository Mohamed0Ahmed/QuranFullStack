import type { APIResponse } from '@playwright/test';

interface ApiEnvelope<T> {
  isSuccess: boolean;
  data: T | null;
}

export async function readApiData<T>(
  response: Pick<APIResponse, 'status' | 'text'> & { dispose?: () => Promise<void> },
  operation: string,
  expectedStatus?: number,
): Promise<T> {
  const status = response.status();
  const body = await response.text();
  await response.dispose?.();
  if (expectedStatus === undefined ? status < 200 || status >= 300 : status !== expectedStatus) {
    throw new Error(`${operation} failed with HTTP ${status}; response body omitted.`);
  }

  let envelope: ApiEnvelope<T>;
  try {
    envelope = JSON.parse(body) as ApiEnvelope<T>;
  } catch {
    throw new Error(`${operation} returned invalid JSON; response body omitted.`);
  }
  if (!envelope.isSuccess || envelope.data === null) {
    throw new Error(`${operation} returned an unsuccessful API envelope; response body omitted.`);
  }
  return envelope.data;
}
