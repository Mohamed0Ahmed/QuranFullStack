import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { DomSanitizer } from '@angular/platform-browser';

import { SafeHtmlPipe } from './safe-html.pipe';

describe('SafeHtmlPipe', () => {
  it('strips script tags so malicious HTML is not passed through', () => {
    TestBed.configureTestingModule({
      providers: [SafeHtmlPipe],
    });

    const pipe = TestBed.inject(SafeHtmlPipe);
    const malicious = '<p>safe</p><script>alert("xss")</script>';

    const result = pipe.transform(malicious);

    expect(result).toContain('safe');
    expect(result).not.toContain('<script');
    expect(result).not.toContain('alert');
  });

  it('returns an empty string for nullish input', () => {
    TestBed.configureTestingModule({
      providers: [SafeHtmlPipe],
    });

    const pipe = TestBed.inject(SafeHtmlPipe);
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
  });
});
