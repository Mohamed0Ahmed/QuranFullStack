import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideRouter, Router, RouterOutlet } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';

import { ABWAB_ROUTES } from './abwab.routes';
import { navLabel } from '../../core/navigation/route-paths';

@Component({
  selector: 'qd-test-host',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
class TestHostComponent {}

describe('ABWAB_ROUTES', () => {
  it('carries the abwab nav label as its route title', () => {
    expect(ABWAB_ROUTES[0].title).toBe(navLabel('abwab'));
  });

  it('renders the abwab page at its root path', async () => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [
        provideRouter([{ path: 'abwab', loadChildren: () => ABWAB_ROUTES }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
      teardown: { destroyAfterEach: true },
    });

    const fixture = TestBed.createComponent(TestHostComponent);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/abwab');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('qd-abwab-page')).toBeTruthy();
  });
});
