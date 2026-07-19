// Serialized vocabulary of the detail overlay: a typed, versioned frame union carried in
// the URL. Deliberately NOT imported from Words feature models — the URL grammar is a
// shareable contract (old links must keep meaning) while feature models may evolve.

export const DETAIL_OVERLAY_QUERY_KEYS = {
  /** Repeated query key; values are ordered bottom → top of the stack. */
  frame: 'qdDetail',
  /** Present (value `1`) while the dialog is visible; absent when closed. */
  open: 'qdDetailOpen',
} as const;

export const DETAIL_OVERLAY_FRAME_VERSION = 'v1' as const;

/** Hard stack cap: a ninth append is refused, never silently dropped. */
export const DETAIL_OVERLAY_MAX_FRAMES = 8;

export type UniqueFrameMode = 'simple' | 'tashkeel';
export type UniqueFrameView = 'surahs' | 'missing' | 'ayahs';
export type RootFrameView = 'words' | 'ayahs' | 'surahs' | 'lemmas' | 'stems';
export type LemmaFrameView = 'words' | 'ayahs' | 'surahs' | 'stems';
export type StemFrameView = 'words' | 'ayahs' | 'surahs' | 'lemmas';
export type FrameWordView = 'simple' | 'tashkeel';
export type FrameSurahView = 'mentioned' | 'missing';
export type WordTypeFrameCase = 'all' | 'nominative' | 'accusative' | 'genitive' | 'null';
export type WordTypeFrameTense = 'all' | 'past' | 'present' | 'imperative';
export type WordTypeFrameVoice = 'all' | 'active' | 'passive';
export type WordTypeFrameView = 'words' | 'ayahs' | 'surahs';

/** `v1~unique~<simple|tashkeel>~<id>~<view>~<ayahPage>` */
export interface UniqueDetailFrame {
  readonly kind: 'unique';
  readonly mode: UniqueFrameMode;
  readonly id: number;
  readonly view: UniqueFrameView;
  readonly ayahPage: number;
}

/** `v1~root~<id>~<view>~<wordView>~<surahView>~<detailPage>` */
export interface RootDetailFrame {
  readonly kind: 'root';
  readonly id: number;
  readonly view: RootFrameView;
  readonly wordView: FrameWordView;
  readonly surahView: FrameSurahView;
  readonly detailPage: number;
}

/** `v1~lemma~<id>~<view>~<wordView>~<surahView>~<detailPage>~<typeCode|->` */
export interface LemmaDetailFrame {
  readonly kind: 'lemma';
  readonly id: number;
  readonly view: LemmaFrameView;
  readonly wordView: FrameWordView;
  readonly surahView: FrameSurahView;
  readonly detailPage: number;
  readonly typeCode: string | null;
}

/** `v1~stem~<id>~<view>~<wordView>~<surahView>~<detailPage>~<typeCode|->` */
export interface StemDetailFrame {
  readonly kind: 'stem';
  readonly id: number;
  readonly view: StemFrameView;
  readonly wordView: FrameWordView;
  readonly surahView: FrameSurahView;
  readonly detailPage: number;
  readonly typeCode: string | null;
}

/** `v1~wordType~<tashkeelId>~<contextCode>~<case>~<tense>~<voice>~<view>~<detailPage>` */
export interface WordTypeDetailFrame {
  readonly kind: 'wordType';
  readonly tashkeelWordId: number;
  readonly contextCode: string;
  readonly case: WordTypeFrameCase;
  readonly tense: WordTypeFrameTense;
  readonly voice: WordTypeFrameVoice;
  readonly view: WordTypeFrameView;
  readonly detailPage: number;
}

export type DetailFrame =
  | UniqueDetailFrame
  | RootDetailFrame
  | LemmaDetailFrame
  | StemDetailFrame
  | WordTypeDetailFrame;

export type DetailFrameKind = DetailFrame['kind'];

export type DetailOverlayVisibility = 'open' | 'closed';

/** URL-authoritative overlay state: what the current URL says, nothing more. */
export interface DetailOverlayUrlState {
  readonly visibility: DetailOverlayVisibility;
  readonly stack: readonly DetailFrame[];
}

export const CLOSED_DETAIL_OVERLAY_STATE: DetailOverlayUrlState = {
  visibility: 'closed',
  stack: [],
};

// Complete-identity equality (every identity AND view field): never compare by numeric id
// alone, so a push of the identical current top frame is correctly treated as a no-op.
export function detailFramesEqual(a: DetailFrame, b: DetailFrame): boolean {
  if (a.kind !== b.kind) {
    return false;
  }

  switch (a.kind) {
    case 'unique': {
      const other = b as UniqueDetailFrame;
      return a.mode === other.mode && a.id === other.id && a.view === other.view && a.ayahPage === other.ayahPage;
    }
    case 'root': {
      const other = b as RootDetailFrame;
      return (
        a.id === other.id &&
        a.view === other.view &&
        a.wordView === other.wordView &&
        a.surahView === other.surahView &&
        a.detailPage === other.detailPage
      );
    }
    case 'lemma':
    case 'stem': {
      const other = b as LemmaDetailFrame | StemDetailFrame;
      return (
        a.id === other.id &&
        a.view === other.view &&
        a.wordView === other.wordView &&
        a.surahView === other.surahView &&
        a.detailPage === other.detailPage &&
        a.typeCode === other.typeCode
      );
    }
    case 'wordType': {
      const other = b as WordTypeDetailFrame;
      return (
        a.tashkeelWordId === other.tashkeelWordId &&
        a.contextCode === other.contextCode &&
        a.case === other.case &&
        a.tense === other.tense &&
        a.voice === other.voice &&
        a.view === other.view &&
        a.detailPage === other.detailPage
      );
    }
  }
}

export function detailStacksEqual(a: readonly DetailFrame[], b: readonly DetailFrame[]): boolean {
  return a.length === b.length && a.every((frame, index) => detailFramesEqual(frame, b[index]));
}
