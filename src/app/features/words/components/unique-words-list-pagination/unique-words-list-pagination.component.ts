import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'qd-unique-words-list-pagination',
  standalone: true,
  templateUrl: './unique-words-list-pagination.component.html',
  styleUrl: './unique-words-list-pagination.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UniqueWordsListPaginationComponent {
  readonly currentPage = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly disabled = input(false);

  readonly pageChange = output<number>();

  protected readonly showPagination = computed(() => this.totalCount() > this.pageSize());

  protected readonly lastPage = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize())),
  );
}
