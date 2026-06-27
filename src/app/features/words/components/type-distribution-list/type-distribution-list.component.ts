import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export interface TypeDistributionItem {
  code: string;
  arabicLabel: string;
  englishLabel: string;
  occurrencesCount: number;
  firstSurahNumber: number;
  firstAyahNumber: number;
  firstWordNumber: number;
}

interface TypeDistributionRow extends TypeDistributionItem {
  dominant: boolean;
}

@Component({
  selector: 'qd-type-distribution-list',
  standalone: true,
  templateUrl: './type-distribution-list.component.html',
  styleUrl: './type-distribution-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TypeDistributionListComponent {
  readonly items = input.required<readonly TypeDistributionItem[]>();
  readonly loading = input(false);

  protected readonly sectionLabel = 'توزيع الأنواع';
  protected readonly typeHeader = 'النوع';
  protected readonly countHeader = 'عدد مرات الظهور';
  protected readonly loadingLabel = 'جارٍ تحميل توزيع الأنواع…';
  protected readonly emptyLabel = 'لا توجد أنواع';

  protected readonly rows = computed<readonly TypeDistributionRow[]>(() =>
    this.items().map((item, index) => ({
      ...item,
      dominant: index === 0,
    })),
  );

}
