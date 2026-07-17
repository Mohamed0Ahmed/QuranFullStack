import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { AyahCardComponent } from './ayah-card.component';

@Component({
  standalone: true,
  imports: [AyahCardComponent],
  template: `
    <ul>
      <li qdAyahCard data-testid="host-li">
        <header data-testid="projected-meta">SYNTH_META</header>
        <p data-testid="projected-text">SYNTH_TEXT</p>
        <button type="button" data-testid="projected-action">SYNTH_ACTION</button>
      </li>
      <article qdAyahCard data-testid="host-article">SYNTH_ARTICLE_CONTENT</article>
    </ul>
  `,
})
class AyahCardTestHostComponent {}

describe('AyahCardComponent', () => {
  async function createHost() {
    const fixture = TestBed.createComponent(AyahCardTestHostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('projects metadata, text, and action content on the caller-owned semantic wrapper', async () => {
    const root = await createHost();
    const li = root.querySelector('[data-testid="host-li"]')!;

    expect(li.tagName).toBe('LI');
    expect(li.querySelector('[data-testid="projected-meta"]')?.textContent).toContain('SYNTH_META');
    expect(li.querySelector('[data-testid="projected-text"]')?.textContent).toContain('SYNTH_TEXT');
    expect(li.querySelector('[data-testid="projected-action"]')).not.toBeNull();

    const article = root.querySelector('[data-testid="host-article"]')!;
    expect(article.tagName).toBe('ARTICLE');
    expect(article.textContent).toContain('SYNTH_ARTICLE_CONTENT');
  });

  it('marks the host with the qd-ayah-card frame class', async () => {
    const root = await createHost();

    expect(root.querySelector('[data-testid="host-li"]')!.classList.contains('qd-ayah-card')).toBe(true);
    expect(root.querySelector('[data-testid="host-article"]')!.classList.contains('qd-ayah-card')).toBe(true);
  });

  it('exposes no domain, text, or navigation API (presentation-only contract)', () => {
    const definition = (AyahCardComponent as unknown as { ɵcmp: { inputs: object; outputs: object } }).ɵcmp;

    expect(Object.keys(definition.inputs)).toEqual([]);
    expect(Object.keys(definition.outputs)).toEqual([]);
  });
});
