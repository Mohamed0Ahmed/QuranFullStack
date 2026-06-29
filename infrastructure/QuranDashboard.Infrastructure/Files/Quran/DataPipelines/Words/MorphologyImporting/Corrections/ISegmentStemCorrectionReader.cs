namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

public interface ISegmentStemCorrectionReader
{
    // Loads, schema-validates, and indexes the embedded segment-stem curated artifact.
    // Fails closed (throws) on a malformed artifact so a bad correction set never imports silently.
    SegmentStemCorrectionLoaded Load();
}
