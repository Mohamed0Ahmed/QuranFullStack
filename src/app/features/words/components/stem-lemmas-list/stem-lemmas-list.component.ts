import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { buildLemmasDeepLink } from '../../state/lemmas-url-sync';
import { StemLemmaItemDto } from '../../models/stems.models';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';

interface StemLemmaRow {
  lemma: StemLemmaItemDto;
  href: string;
}

@Component({
  selector: 'qd-stem-lemmas-list',
  standalone: true,
  templateUrl: './stem-lemmas-list.component.html',
  styleUrl: './stem-lemmas-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemLemmasListComponent {
  readonly lemmas = input.required<readonly StemLemmaItemDto[]>();
  readonly loading = input(false);

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly lemmaTextHeader = 'الصيغة المعجمية';
  protected readonly buckwalterHeader = 'Buckwalter';
  protected readonly occurrencesHeader = 'عدد مرات الظهور';
  protected readonly loadingLabel = 'جارٍ تحميل الصيغ المعجمية المرتبطة…';
  protected readonly emptyLabel = 'لا توجد صيغ معجمية مرتبطة';
  protected readonly missingBuckwalterLabel = '—';

  protected readonly rows = computed<readonly StemLemmaRow[]>(() =>
    this.lemmas().map((lemma) => ({
      lemma,
      href: deepLinkToHref(
        buildLemmasDeepLink({ lemmaId: lemma.lemmaId, view: 'words', wordView: 'simple' }),
      ),
    })),
  );

}
