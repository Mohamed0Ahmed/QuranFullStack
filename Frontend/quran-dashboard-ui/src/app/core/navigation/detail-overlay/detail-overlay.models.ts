export const DETAIL_OVERLAY_QUERY_KEYS = {
  frame: 'qdDetail',
  open: 'qdDetailOpen',
} as const;

export const DETAIL_OVERLAY_FRAME_VERSION = 'v1' as const;

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

export interface UniqueDetailFrame {
  readonly kind: 'unique';
  readonly mode: UniqueFrameMode;
  readonly id: number;
  readonly view: UniqueFrameView;
  readonly ayahPage: number;
}

export interface RootDetailFrame {
  readonly kind: 'root';
  readonly id: number;
  readonly view: RootFrameView;
  readonly wordView: FrameWordView;
  readonly surahView: FrameSurahView;
  readonly detailPage: number;
}

export interface LemmaDetailFrame {
  readonly kind: 'lemma';
  readonly id: number;
  readonly view: LemmaFrameView;
  readonly wordView: FrameWordView;
  readonly surahView: FrameSurahView;
  readonly detailPage: number;
  readonly typeCode: string | null;
}

export interface StemDetailFrame {
  readonly kind: 'stem';
  readonly id: number;
  readonly view: StemFrameView;
  readonly wordView: FrameWordView;
  readonly surahView: FrameSurahView;
  readonly detailPage: number;
  readonly typeCode: string | null;
}

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

export interface DetailOverlayUrlState {
  readonly visibility: DetailOverlayVisibility;
  readonly stack: readonly DetailFrame[];
}

export const CLOSED_DETAIL_OVERLAY_STATE: DetailOverlayUrlState = {
  visibility: 'closed',
  stack: [],
};

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
