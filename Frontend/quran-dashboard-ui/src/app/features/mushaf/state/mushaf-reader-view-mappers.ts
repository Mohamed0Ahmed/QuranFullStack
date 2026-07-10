import {
  AyahStudyDto,
  AyahStudyViewModel,
  MushafPageDto,
  MushafPageViewModel,
  WordAnalysisDto,
  WordAnalysisViewModel,
} from '../models/mushaf.models';
import { segmentSlotToColor } from './segment-color-palette';

export function toPageViewModel(dto: MushafPageDto): MushafPageViewModel {
  return {
    pageNumber: dto.pageNumber,
    previousPageNumber: dto.previousPageNumber,
    nextPageNumber: dto.nextPageNumber,
    surahs: dto.surahs,
    ayahRange: dto.ayahRange,
    navigation: dto.navigation,
    lines: dto.lines,
    markers: dto.markers,
  };
}

export function toAyahStudyViewModel(dto: AyahStudyDto): AyahStudyViewModel {
  return {
    ayah: dto.ayah,
    selectedSources: dto.selectedSources,
    tafsir: dto.tafsir,
    translation: dto.translation,
    fullI3rab: dto.fullI3rab,
    similaritySummary: dto.similaritySummary,
  };
}

export function toWordAnalysisViewModel(dto: WordAnalysisDto): WordAnalysisViewModel {
  return {
    word: dto.word,
    identity: dto.identity,
    morphology: dto.morphology,
    segments: dto.renderedWordSegments.map((segment) => ({
      segmentLocation: segment.segmentLocation,
      segmentNumber: segment.segmentNumber,
      segmentColorSlot: segment.segmentColorSlot,
      color: segmentSlotToColor(segment.segmentColorSlot),
      segmentKind: segment.segmentKind,
      segmentDisplayText: segment.segmentDisplayText,
      isMissing: segment.displayTextStatus === 'missing',
      segmentPos: segment.segmentPos,
      segmentPosLabel: segment.segmentPosLabel,
      segmentI3rabArabic: segment.segmentI3rabArabic,
      i3rabStatus: segment.i3rabStatus,
    })),
  };
}
