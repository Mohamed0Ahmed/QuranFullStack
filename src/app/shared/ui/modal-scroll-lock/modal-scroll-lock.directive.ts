import { Directive, OnDestroy, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Directive({
  selector: '[qdModalScrollLock]',
  standalone: true,
})
export class ModalScrollLockDirective implements OnInit, OnDestroy {
  private readonly platformId = inject(PLATFORM_ID);
  private previousOverflow = '';

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
  }

  ngOnDestroy(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    document.body.style.overflow = this.previousOverflow;
  }
}
