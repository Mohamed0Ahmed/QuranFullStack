// Moved from features/words/models/words-shared.labels.ts (Slice B2, T1001) when the component
// it backs was promoted to shared/ui/. loading is the sr-only text announced by the skeleton's
// role="status" container. unavailable stands in for the number when the list errored
// (Feature 030, N3 row 6): the stat holds its line instead of unmounting, while the page's own
// error state stays the only place that explains the failure.
export const RESULT_COUNT_LABELS = {
  loading: 'جارٍ التحميل…',
  unavailable: '—',
} as const;
