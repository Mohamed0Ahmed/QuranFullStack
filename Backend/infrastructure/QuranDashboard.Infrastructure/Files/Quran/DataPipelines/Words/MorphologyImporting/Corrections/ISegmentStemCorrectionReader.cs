namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

public interface ISegmentStemCorrectionReader
{
    // Fails closed (throws) on a malformed artifact so a bad correction set never imports silently.
    SegmentStemCorrectionLoaded Load();
}
