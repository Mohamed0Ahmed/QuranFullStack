import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { WordsExplainerComponent } from './words-explainer.component';
import { WORDS_EXPLAINER_CONTENT } from '../../models/words-explainer.content';

const RECORD = WORDS_EXPLAINER_CONTENT.roots;

@Component({
  standalone: true,
  imports: [WordsExplainerComponent],
  template: `
    <qd-words-explainer [content]="content" [expanded]="expanded" (toggled)="onToggled($event)">
      <div data-testid="projected-example">PROJECTED_EXAMPLE</div>
    </qd-words-explainer>
  `,
})
class HostComponent {
  content = RECORD;
  expanded = true;
  readonly toggledEvents: boolean[] = [];
  onToggled(value: boolean): void {
    this.toggledEvents.push(value);
  }
}

describe('WordsExplainerComponent', () => {
  function setup(expanded = true) {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.expanded = expanded;
    fixture.detectChanges();
    return {
      fixture,
      host: fixture.componentInstance,
      root: fixture.nativeElement as HTMLElement,
    };
  }

  it('renders the ordinal (aria-hidden), eyebrow, and tagline from the content record', () => {
    const { root } = setup();

    const ordinal = root.querySelector('.qd-explainer__ordinal') as HTMLElement;
    expect(ordinal.textContent?.trim()).toBe(RECORD.ordinal);
    expect(ordinal.getAttribute('aria-hidden')).toBe('true');
    expect(root.querySelector('.qd-explainer__eyebrow')?.textContent?.trim()).toBe(RECORD.eyebrow);
    expect(root.querySelector('.qd-explainer__tagline')?.textContent?.trim()).toBe(RECORD.tagline);
  });

  it('names the section by its title without duplicating a heading (page <h1> owns the title)', () => {
    const { root } = setup();

    const section = root.querySelector('[data-testid="words-explainer--roots"]') as HTMLElement;
    expect(section.getAttribute('aria-label')).toBe(RECORD.title);
    // The component renders no heading element of its own.
    expect(root.querySelector('h1, h2, h3')).toBeNull();
  });

  it('shows the body, projected example, and benefit callout when expanded', () => {
    const { root } = setup(true);

    expect(root.querySelector('[data-testid="words-explainer-body"]')).not.toBeNull();
    expect(root.querySelector('.qd-explainer__body-text')?.textContent?.trim()).toBe(RECORD.body);
    expect(root.querySelector('[data-testid="projected-example"]')?.textContent).toContain('PROJECTED_EXAMPLE');

    const benefit = root.querySelector('[data-testid="words-explainer-benefit"]') as HTMLElement;
    expect(benefit.textContent).toContain('الفائدة');
    expect(benefit.textContent).toContain(RECORD.benefit);
  });

  it('wires the toggle to aria-expanded and aria-controls of the body', () => {
    const { root } = setup(true);

    const toggle = root.querySelector('[data-testid="words-explainer-toggle--roots"]') as HTMLElement;
    const body = root.querySelector('[data-testid="words-explainer-body"]') as HTMLElement;
    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    expect(toggle.getAttribute('aria-controls')).toBe('words-explainer-body--roots');
    expect(body.id).toBe('words-explainer-body--roots');
  });

  it('hides only the body when collapsed — the tagline and toggle stay visible', () => {
    const { root } = setup(false);

    expect(root.querySelector('[data-testid="words-explainer-body"]')).toBeNull();
    expect(root.querySelector('[data-testid="projected-example"]')).toBeNull();
    expect(root.querySelector('.qd-explainer__tagline')?.textContent?.trim()).toBe(RECORD.tagline);
    const toggle = root.querySelector('[data-testid="words-explainer-toggle--roots"]') as HTMLElement;
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
  });

  it('emits the negated expanded value when the toggle is clicked', () => {
    const { fixture, host, root } = setup(true);

    (root.querySelector('[data-testid="words-explainer-toggle--roots"]') as HTMLButtonElement).click();
    expect(host.toggledEvents).toEqual([false]);

    host.expanded = false;
    fixture.detectChanges();
    (root.querySelector('[data-testid="words-explainer-toggle--roots"]') as HTMLButtonElement).click();
    expect(host.toggledEvents).toEqual([false, true]);
  });
});
