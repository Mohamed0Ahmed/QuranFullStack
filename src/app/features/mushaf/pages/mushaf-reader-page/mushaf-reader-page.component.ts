import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';

import { MushafPageAreaComponent } from '../../components/mushaf-page-area/mushaf-page-area.component';
import { MUSHAF_URL_KEYS } from '../../models/mushaf.models';
import { MushafReaderFacade } from '../../state/mushaf-reader.facade';

@Component({
  selector: 'qd-mushaf-reader-page',
  standalone: true,
  imports: [CommonModule, MushafPageAreaComponent],
  templateUrl: './mushaf-reader-page.component.html',
  styleUrls: ['./mushaf-reader-page.component.scss'],
})
export class MushafReaderPageComponent implements OnInit {
  protected readonly facade = inject(MushafReaderFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.facade.loadSurahCatalog();

    this.route.queryParamMap.subscribe((params) => {
      const rawPage = Number(params.get(MUSHAF_URL_KEYS.page) ?? '1');
      const pageNumber = Number.isFinite(rawPage)
        ? Math.min(604, Math.max(1, rawPage))
        : 1;
      this.facade.loadPage(pageNumber);
    });
  }

  protected onPageChange(pageNumber: number): void {
    this.navigateToPage(pageNumber);
  }

  protected onSurahJump(surahNumber: number): void {
    const startPage = this.facade.resolveSurahStartPage(surahNumber);
    if (startPage !== null) {
      this.navigateToPage(startPage);
    }
  }

  private navigateToPage(pageNumber: number): void {
    const clamped = Math.min(604, Math.max(1, pageNumber));
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { [MUSHAF_URL_KEYS.page]: clamped },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
