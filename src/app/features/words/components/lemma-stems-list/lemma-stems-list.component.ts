import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { buildStemsDeepLink } from '../../state/stems-url-sync';
import { LemmaStemItemDto } from '../../models/lemmas.models';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';

interface LemmaStemRow {
  stem: LemmaStemItemDto;
  href: string;
}

@Component({
  selector: 'qd-lemma-stems-list',
  standalone: true,
  templateUrl: './lemma-stems-list.component.html',
  styleUrl: './lemma-stems-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmaStemsListComponent {
  readonly stems = input.required<readonly LemmaStemItemDto[]>();
  readonly loading = input(false);

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly stemTextHeader = 'الأصل الصرفي';
  protected readonly occurrencesHeader = 'عدد مرات الظهور';
  protected readonly loadingLabel = 'جارٍ تحميل الأصول الصرفية المرتبطة…';
  protected readonly emptyLabel = 'لا توجد أصول صرفية مرتبطة';

  protected readonly rows = computed<readonly LemmaStemRow[]>(() =>
    this.stems().map((stem) => ({
      stem,
      href: deepLinkToHref(
        buildStemsDeepLink({ stemId: stem.stemId, view: 'words', wordView: 'simple' }),
      ),
    })),
  );

}
