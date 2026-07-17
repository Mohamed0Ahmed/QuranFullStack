import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { ScrollLockService } from './scroll-lock.service';

describe('ScrollLockService', () => {
  let service: ScrollLockService;

  beforeEach(() => {
    document.body.style.overflow = 'scroll';
    service = TestBed.inject(ScrollLockService);
  });

  afterEach(() => {
    document.body.style.overflow = '';
  });

  it('locks on first acquire and restores the original overflow on last release', () => {
    service.acquire();
    expect(document.body.style.overflow).toBe('hidden');

    service.release();
    expect(document.body.style.overflow).toBe('scroll');
  });

  it('keeps the body locked while any of two simultaneous consumers holds the lock', () => {
    service.acquire(); // responsive drawer
    service.acquire(); // global overlay on top
    expect(document.body.style.overflow).toBe('hidden');

    service.release(); // one layer goes away
    expect(document.body.style.overflow).toBe('hidden');

    service.release(); // last holder releases
    expect(document.body.style.overflow).toBe('scroll');
  });

  it('ignores unbalanced releases', () => {
    service.release();
    expect(document.body.style.overflow).toBe('scroll');

    service.acquire();
    service.release();
    service.release();
    expect(document.body.style.overflow).toBe('scroll');

    service.acquire();
    expect(document.body.style.overflow).toBe('hidden');
    service.release();
    expect(document.body.style.overflow).toBe('scroll');
  });
});
