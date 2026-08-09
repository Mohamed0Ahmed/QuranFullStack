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

  it('isLocked tracks whether any holder still holds the lock, across two simultaneous consumers', () => {
    expect(service.isLocked()).toBe(false);

    service.acquire(); // responsive drawer
    expect(service.isLocked()).toBe(true);

    service.acquire(); // global overlay on top
    expect(service.isLocked()).toBe(true);

    service.release(); // one layer goes away
    expect(service.isLocked()).toBe(true);

    service.release(); // last holder releases
    expect(service.isLocked()).toBe(false);
  });

  describe('handles — the token-identified hold used by nested layers', () => {
    it('unlocks only when the last of three nested holders releases, in any order', () => {
      const drawer = service.hold();
      const modal = service.hold();
      const nestedConfirm = service.hold();
      expect(service.holderCount()).toBe(3);

      nestedConfirm.release();
      expect(document.body.style.overflow).toBe('hidden');

      drawer.release();
      expect(document.body.style.overflow).toBe('hidden');

      modal.release();
      expect(document.body.style.overflow).toBe('scroll');
    });

    // A layer that releases twice (destroy after an explicit close, a double effect run) used to
    // decrement a *different* layer's count and unlock the page under a still-open dialog.
    it('does not unlock early when one holder releases twice while another still holds', () => {
      const drawer = service.hold();
      const modal = service.hold();

      modal.release();
      modal.release();
      modal.release();

      expect(service.holderCount()).toBe(1);
      expect(document.body.style.overflow).toBe('hidden');

      drawer.release();
      expect(document.body.style.overflow).toBe('scroll');
    });

    it('never deadlocks: releasing every handle restores the page even when handles interleave with legacy pairs', () => {
      const modal = service.hold();
      service.acquire();
      const nested = service.hold();

      service.release();
      nested.release();
      modal.release();

      expect(service.holderCount()).toBe(0);
      expect(service.isLocked()).toBe(false);
      expect(document.body.style.overflow).toBe('scroll');
    });
  });
});
