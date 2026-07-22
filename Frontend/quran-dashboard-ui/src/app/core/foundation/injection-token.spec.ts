import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { injectionToken } from './injection-token';

describe('injectionToken', () => {
  it('creates a root-provided token that resolves from its factory', () => {
    const TOKEN = injectionToken<number>('qd.test.answer', () => 42);

    TestBed.configureTestingModule({});

    expect(TestBed.inject(TOKEN)).toBe(42);
  });

  it('creates a description-only token that resolves from an explicit provider', () => {
    const TOKEN = injectionToken<string>('qd.test.label');

    TestBed.configureTestingModule({
      providers: [{ provide: TOKEN, useValue: 'مرحبا' }],
    });

    expect(TestBed.inject(TOKEN)).toBe('مرحبا');
  });
});
