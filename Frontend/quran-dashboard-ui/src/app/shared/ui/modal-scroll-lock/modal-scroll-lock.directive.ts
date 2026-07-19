import { Directive, OnDestroy, OnInit, inject } from '@angular/core';

import { ScrollLockService } from './scroll-lock.service';

// Delegates to the reference-counted ScrollLockService so stacked layers (a drawer
// under the global detail overlay) cannot unlock each other's scroll lock.
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
