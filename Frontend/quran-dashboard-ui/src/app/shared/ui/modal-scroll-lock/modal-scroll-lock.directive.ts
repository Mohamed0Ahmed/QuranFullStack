import { Directive, OnDestroy, OnInit, inject } from '@angular/core';

import { ScrollLockService } from './scroll-lock.service';

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
