using QuranDashboard.Domain.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;

public static class PosTagSeed
{
    public static IReadOnlyList<PosTag> GetAll() =>
    [
        new PosTag { Code = "N", ArabicLabel = "اسم", EnglishLabel = "Noun", Category = "noun", SortOrder = 1 },
        new PosTag { Code = "V", ArabicLabel = "فعل", EnglishLabel = "Verb", Category = "verb", SortOrder = 2 },
        new PosTag { Code = "PN", ArabicLabel = "اسم علم", EnglishLabel = "Proper Noun", Category = "noun", SortOrder = 3 },
        new PosTag { Code = "ADJ", ArabicLabel = "صفة", EnglishLabel = "Adjective", Category = "noun", SortOrder = 4 },
        new PosTag { Code = "PRON", ArabicLabel = "ضمير", EnglishLabel = "Pronoun", Category = "noun", SortOrder = 5 },
        new PosTag { Code = "P", ArabicLabel = "حرف جر", EnglishLabel = "Preposition", Category = "particle", SortOrder = 6 },
        new PosTag { Code = "CONJ", ArabicLabel = "حرف عطف", EnglishLabel = "Conjunction", Category = "particle", SortOrder = 7 },
        new PosTag { Code = "NEG", ArabicLabel = "حرف نفي", EnglishLabel = "Negation", Category = "particle", SortOrder = 8 },
        new PosTag { Code = "REL", ArabicLabel = "اسم موصول", EnglishLabel = "Relative Pronoun", Category = "noun", SortOrder = 9 },
        new PosTag { Code = "DEM", ArabicLabel = "اسم إشارة", EnglishLabel = "Demonstrative", Category = "noun", SortOrder = 10 },
        new PosTag { Code = "VOC", ArabicLabel = "حرف نداء", EnglishLabel = "Vocative", Category = "particle", SortOrder = 11 },
        new PosTag { Code = "INL", ArabicLabel = "حروف مقطّعة", EnglishLabel = "Quranic Initials", Category = "particle", SortOrder = 12, Description = "Disconnected letters opening certain surahs (الحروف المقطّعة); not an oath particle" },
        new PosTag { Code = "IMPV", ArabicLabel = "لام الأمر", EnglishLabel = "Imperative Lām", Category = "particle", SortOrder = 13, Description = "Imperative lām prefix (لام الأمر); the imperative verb itself is coded V" },
        new PosTag { Code = "PERF", ArabicLabel = "فعل ماض", EnglishLabel = "Perfect", Category = "verb", SortOrder = 14, Description = "Past/perfect verb form" },
        new PosTag { Code = "IMPF", ArabicLabel = "فعل مضارع", EnglishLabel = "Imperfect", Category = "verb", SortOrder = 15, Description = "Present/imperfect verb form" },
        new PosTag { Code = "ACC", ArabicLabel = "حرف نصب", EnglishLabel = "Accusative Particle", Category = "particle", SortOrder = 16 },
        new PosTag { Code = "EMPH", ArabicLabel = "حرف تأكيد", EnglishLabel = "Emphatic", Category = "particle", SortOrder = 17 },
        new PosTag { Code = "REM", ArabicLabel = "حرف استئناف", EnglishLabel = "Resumption", Category = "particle", SortOrder = 18, Description = "Resumption particle (استئناف), not exception" },
        new PosTag { Code = "ANS", ArabicLabel = "حرف جواب", EnglishLabel = "Answer Particle", Category = "particle", SortOrder = 19 },
        new PosTag { Code = "PRO", ArabicLabel = "حرف نهي", EnglishLabel = "Prohibition Particle", Category = "particle", SortOrder = 20 },
        new PosTag { Code = "FUT", ArabicLabel = "حرف استقبال", EnglishLabel = "Future Particle", Category = "particle", SortOrder = 21 },
        new PosTag { Code = "INTG", ArabicLabel = "استفهام", EnglishLabel = "Interrogative", Category = "particle", SortOrder = 22, Description = "Neutral code-level label; PREFIX:INTG (همزة الاستفهام) and STEM:INTG (اسم استفهام) diverge at the rule layer" },
        new PosTag { Code = "COND", ArabicLabel = "حرف شرط", EnglishLabel = "Conditional", Category = "particle", SortOrder = 23 },
        new PosTag { Code = "PREV", ArabicLabel = "ما الكافّة", EnglishLabel = "Preventive", Category = "particle", SortOrder = 24, Description = "Preventive مَا (ما الكافّة), e.g. إنّما" },
        new PosTag { Code = "CAUS", ArabicLabel = "حرف سببية", EnglishLabel = "Causative", Category = "particle", SortOrder = 25, Description = "Causal fā (فاء السببية); appears as a prefix" },
        new PosTag { Code = "AMD", ArabicLabel = "حرف استدراك", EnglishLabel = "Amendment Particle", Category = "particle", SortOrder = 26, Description = "Amendment/استدراك particle (e.g. لكنّ)" },
        new PosTag { Code = "EXL", ArabicLabel = "حرف تفصيل", EnglishLabel = "Explanation", Category = "particle", SortOrder = 27, Description = "Detail/تفصيل particle (e.g. أمّا)" },
        new PosTag { Code = "RES", ArabicLabel = "أداة حصر", EnglishLabel = "Restriction", Category = "particle", SortOrder = 28, Description = "Restriction tool (حصر), not aversion/ردع" },
        new PosTag { Code = "PRP", ArabicLabel = "لام التعليل", EnglishLabel = "Purpose", Category = "particle", SortOrder = 29, Description = "Purpose lām (لام التعليل / لام كي); appears as a prefix" },
        new PosTag { Code = "COM", ArabicLabel = "واو المعية", EnglishLabel = "Comitative", Category = "particle", SortOrder = 30 },
        new PosTag { Code = "T", ArabicLabel = "ظرف زمان", EnglishLabel = "Time Adverb", Category = "noun", SortOrder = 31, Description = "Time adverb (ظرف زمان)" },
        new PosTag { Code = "LOC", ArabicLabel = "ظرف مكان", EnglishLabel = "Locative Adverb", Category = "noun", SortOrder = 32 },
        new PosTag { Code = "TIM", ArabicLabel = "ظرف زمان", EnglishLabel = "Temporal Adverb", Category = "noun", SortOrder = 33 },
        new PosTag { Code = "ABR", ArabicLabel = "مختصر", EnglishLabel = "Abbreviation", Category = "other", SortOrder = 34 },
        new PosTag { Code = "DET", ArabicLabel = "أداة تعريف", EnglishLabel = "Determiner", Category = "particle", SortOrder = 35, Description = "Definite article prefix (ال)" },
        new PosTag { Code = "SUB", ArabicLabel = "حرف مصدري", EnglishLabel = "Subordinating Conjunction", Category = "particle", SortOrder = 36, Description = "Subordinating/مصدري particle (e.g. أنْ المصدرية)" },
        new PosTag { Code = "IMPN", ArabicLabel = "اسم فعل أمر", EnglishLabel = "Imperative Verbal Noun", Category = "noun", SortOrder = 37, Description = "Noun acting as an imperative verb" },
        new PosTag { Code = "AVR", ArabicLabel = "حرف ردع", EnglishLabel = "Aversion", Category = "particle", SortOrder = 38, Description = "Aversion particle (e.g. كلا)" },
        new PosTag { Code = "CERT", ArabicLabel = "حرف تحقيق", EnglishLabel = "Certainty", Category = "particle", SortOrder = 39, Description = "Particle of certainty (e.g. قد)" },
        new PosTag { Code = "CIRC", ArabicLabel = "حرف حال", EnglishLabel = "Circumstantial", Category = "particle", SortOrder = 40, Description = "Circumstantial particle" },
        new PosTag { Code = "EQ", ArabicLabel = "همزة التسوية", EnglishLabel = "Equalization", Category = "particle", SortOrder = 41, Description = "Equalization hamza (همزة التسوية); appears as a prefix" },
        new PosTag { Code = "EXH", ArabicLabel = "حرف تحضيض", EnglishLabel = "Exhortation", Category = "particle", SortOrder = 42, Description = "Exhortation particle" },
        new PosTag { Code = "EXP", ArabicLabel = "أداة استثناء", EnglishLabel = "Exceptive", Category = "particle", SortOrder = 43, Description = "Exceptive particle" },
        new PosTag { Code = "INC", ArabicLabel = "حرف ابتداء/استفتاح", EnglishLabel = "Inceptive", Category = "particle", SortOrder = 44, Description = "Inceptive/opening particle (ابتداء/استفتاح)" },
        new PosTag { Code = "INT", ArabicLabel = "حرف تفسير", EnglishLabel = "Interpretation", Category = "particle", SortOrder = 45, Description = "Particle of interpretation (e.g. أي المفسِّرة)" },
        new PosTag { Code = "RET", ArabicLabel = "حرف إضراب", EnglishLabel = "Retraction", Category = "particle", SortOrder = 46, Description = "Retraction particle (e.g. بل)" },
        new PosTag { Code = "RSLT", ArabicLabel = "الفاء الرابطة لجواب الشرط", EnglishLabel = "Result", Category = "particle", SortOrder = 47, Description = "Result fā linking the apodosis (الفاء الرابطة لجواب الشرط); appears as a prefix" },
        new PosTag { Code = "SUP", ArabicLabel = "حرف زائد", EnglishLabel = "Supplemental", Category = "particle", SortOrder = 48, Description = "Supplemental/extra particle" },
        new PosTag { Code = "SUR", ArabicLabel = "حرف فجاءة", EnglishLabel = "Surprise", Category = "particle", SortOrder = 49, Description = "Surprise particle (فجاءة), e.g. إذا الفجائية" }
    ];
}
