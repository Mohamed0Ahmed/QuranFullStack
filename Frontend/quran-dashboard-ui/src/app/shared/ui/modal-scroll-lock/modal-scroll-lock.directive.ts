import { Directive, OnDestroy, OnInit, inject } from '@angular/core';

import { ScrollLockService } from './scroll-lock.service';

// Ref-counted via ScrollLockService so stacked layers can't unlock each other's scroll lock.
@Directive({
  selector: '[qdModalScrollLock]',
  standalone: true,
})
export class ModalScrollLockDirective implements OnInit, OnDestroy {
  private readonly scrollLock = inject(ScrollLockService);

  ngOnInit(): void {
    this.scrollLock.acquire();
  }

  ngOnDestroy(): void {
    this.scrollLock.release();
  }
}
