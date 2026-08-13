import { Injectable, computed, inject, signal, untracked } from '@angular/core';
import { Subscription } from 'rxjs';

import { arabicSearchIncludes } from '../../../shared/quran/arabic-search-normalize';
import { LinkingSourceResolver } from '../data-access/linking-source-resolver';
import { LinkingAyah } from '../models/linking-ayah.models';
import { LINKING_LABELS } from '../models/linking.labels';
import { LinkingSourceEditorState } from '../models/linking-workflow.models';
import { LinkingSelection } from '../models/linking-workspace.models';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { applyLinkingSourceConfiguration } from '../utils/apply-linking-source-configuration';
import { selectedLinkingAyahCount } from '../utils/linking-selection';
import { LinkingAccessService } from './linking-access.service';
import { LinkingWorkspaceStore } from './linking-workspace.store';

const INITIAL_EDITOR_STATE: LinkingSourceEditorState = {
  sourceKey: null,
  sourceLabel: null,
  capturedConfigurationRevision: null,
  status: 'idle',
  ayahs: [],
  rawProgress: { loaded: 0, total: null },
  universe: [],
  query: '',
  errorMessage: null,
};

@Injectable({ providedIn: 'root' })
export class LinkingSourceEditorFacade {
  private readonly access = inject(LinkingAccessService);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly resolver = inject(LinkingSourceResolver);
  private readonly editorStateSignal = signal<LinkingSourceEditorState>(INITIAL_EDITOR_STATE);
  private loadSubscription: Subscription | null = null;
  private generation = 0;

  readonly state = this.editorStateSignal.asReadonly();
  readonly selection = computed<LinkingSelection>(() => {
    const sourceKey = this.editorStateSignal().sourceKey;
    return sourceKey === null
      ? { mode: 'all-except', verseKeys: [] }
      : this.workspace.item(sourceKey)?.configuration.ayahInclusion ?? { mode: 'all-except', verseKeys: [] };
  });
  readonly selectedCount = computed(() =>
    selectedLinkingAyahCount(this.selection(), this.editorStateSignal().universe),
  );
  readonly currentItem = computed(() => {
    const sourceKey = this.editorStateSignal().sourceKey;
    return sourceKey === null ? null : this.workspace.item(sourceKey);
  });
  readonly configuredAyahs = computed(() => {
    const item = this.currentItem();
    const ayahs = this.editorStateSignal().ayahs;
    return item === null
      ? ayahs
      : ayahs.map((ayah) => applyLinkingSourceConfiguration(item.configuration, ayah));
  });
  readonly filteredAyahs = computed(() => {
    const query = this.editorStateSignal().query;
    return this.configuredAyahs().filter((ayah) => arabicSearchIncludes(searchText(ayah), query));
  });

  open(sourceKey: string | null): void {
    if (sourceKey === null || !this.access.canUseLinking()) {
      this.close();
      return;
    }
    if (this.editorStateSignal().sourceKey === sourceKey) {
      return;
    }
    const item = untracked(() => this.workspace.item(sourceKey));
    if (item === null) {
      this.close();
      return;
    }
    this.resolve(item.sourceKey, item.source, item.configurationRevision);
  }

  close(): void {
    this.generation += 1;
    this.cancelLoad();
    this.editorStateSignal.set(INITIAL_EDITOR_STATE);
  }

  retry(): void {
    const state = this.editorStateSignal();
    const item = state.sourceKey === null ? null : this.workspace.item(state.sourceKey);
    if (item !== null) {
      this.resolve(item.sourceKey, item.source, item.configurationRevision);
    }
  }

  setQuery(query: string): void {
    this.editorStateSignal.update((state) => ({ ...state, query }));
  }

  toggleAyah(verseKey: string): void {
    const state = this.editorStateSignal();
    if (state.sourceKey !== null && state.status === 'success') {
      this.workspace.toggleSelection(state.sourceKey, verseKey, state.universe);
    }
  }

  selectAll(): void {
    const state = this.editorStateSignal();
    if (state.sourceKey !== null && state.status === 'success') {
      this.workspace.selectAll(state.sourceKey);
    }
  }

  clearAll(): void {
    const state = this.editorStateSignal();
    if (state.sourceKey !== null && state.status === 'success') {
      this.workspace.clearAll(state.sourceKey);
    }
  }

  setManualLinkShape(linkShape: 'grouped' | 'independent'): void {
    const item = this.currentItem();
    if (item?.configuration.kind === 'manual') {
      this.workspace.setManualLinkShape(item.sourceKey, linkShape);
    }
  }

  private resolve(
    sourceKey: string,
    source: LinkingSourceDescriptor,
    configurationRevision: number,
  ): void {
    this.cancelLoad();
    const generation = ++this.generation;
    this.editorStateSignal.set({
      ...INITIAL_EDITOR_STATE,
      sourceKey,
      sourceLabel: source.label,
      capturedConfigurationRevision: configurationRevision,
      status: 'loading',
    });
    try {
      this.loadSubscription = this.resolver.resolve(source, (rawProgress) => {
        if (generation === this.generation) {
          this.editorStateSignal.update((state) => ({ ...state, rawProgress, status: 'loading' }));
        }
      }).subscribe({
        next: (ayahs) => {
          if (generation !== this.generation) {
            return;
          }
          const uniqueAyahs = uniqueAyahsByVerseKey(ayahs);
          const universe = uniqueAyahs.map((ayah) => ayah.verseKey);
          if (this.workspace.item(sourceKey)?.configurationRevision === configurationRevision) {
            this.workspace.reconcileResolvedSource(
              sourceKey,
              uniqueAyahs.map((ayah) => ({ ayahId: ayah.ayahId, verseKey: ayah.verseKey })),
            );
          }
          this.editorStateSignal.update((state) => ({
            ...state,
            status: 'success',
            ayahs: uniqueAyahs,
            universe,
            errorMessage: null,
          }));
        },
        error: (error: unknown) => this.publishError(generation, error, 'error'),
      });
    } catch (error: unknown) {
      this.publishError(generation, error, 'unsupported');
    }
  }

  private publishError(
    generation: number,
    error: unknown,
    status: 'error' | 'unsupported',
  ): void {
    if (generation !== this.generation) {
      return;
    }
    const errorMessage = error instanceof Error ? error.message : LINKING_LABELS.sourceLoadError;
    this.editorStateSignal.update((state) => ({ ...state, status, errorMessage }));
  }

  private cancelLoad(): void {
    this.loadSubscription?.unsubscribe();
    this.loadSubscription = null;
  }
}

function uniqueAyahsByVerseKey(ayahs: readonly LinkingAyah[]): readonly LinkingAyah[] {
  const byVerseKey = new Map<string, LinkingAyah>();
  for (const ayah of ayahs) {
    if (!byVerseKey.has(ayah.verseKey)) {
      byVerseKey.set(ayah.verseKey, ayah);
    }
  }
  return [...byVerseKey.values()];
}

function searchText(ayah: LinkingAyah): string {
  return [
    ayah.verseKey,
    ayah.surahNameArabic ?? '',
    ...ayah.words.filter((word) => !word.isAyahMarker).map((word) => word.textUthmani),
  ].join(' ');
}
