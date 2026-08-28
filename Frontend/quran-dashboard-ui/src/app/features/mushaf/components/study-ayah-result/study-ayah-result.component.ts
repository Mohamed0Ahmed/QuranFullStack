import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

type StudyAyahResultTestIdKind = 'similar-ayah' | 'mutashabihat-occurrence';

@Component({
  selector: 'li[qdStudyAyahResult]',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './study-ayah-result.component.html',
  styleUrl: './study-ayah-result.component.scss',
  host: {
    class: 'study-ayah-result',
    '[class.study-ayah-result--selected]': 'selected()',
  },
})
export class StudyAyahResultComponent {
  readonly position = input.required<number>();
  readonly surahNameArabic = input.required<string>();
  readonly ayahNumber = input.required<number>();
  readonly pageNumber = input.required<number>();
  readonly displayText = input.required<string>();
  readonly navigateLabel = input.required<string>();
  readonly testIdKind = input.required<StudyAyahResultTestIdKind>();
  readonly selected = input(false);

  readonly ayahNavigate = output<void>();

  protected readonly positionTestId = computed(() => `${this.testIdKind()}-index`);
  protected readonly referenceTestId = computed(() => `${this.testIdKind()}-reference`);
  protected readonly pageTestId = computed(() =>
    this.testIdKind() === 'similar-ayah'
      ? 'similar-ayah-page-context'
      : 'mutashabihat-occurrence-page',
  );
  protected readonly selectedLabelTestId = computed(
    () => `${this.testIdKind()}-selected-label`,
  );
  protected readonly textTestId = computed(() => `${this.testIdKind()}-text`);
}
