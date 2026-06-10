using QuranDashboard.Domain.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Files.Quran.Morphology;

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
        new PosTag { Code = "INL", ArabicLabel = "قسم", EnglishLabel = "Particle of Oath", Category = "particle", SortOrder = 12 },
        new PosTag { Code = "IMPV", ArabicLabel = "فعل أمر", EnglishLabel = "Imperative", Category = "verb", SortOrder = 13, Description = "Imperative verb form" },
        new PosTag { Code = "PERF", ArabicLabel = "فعل ماض", EnglishLabel = "Perfect", Category = "verb", SortOrder = 14, Description = "Past/perfect verb form" },
        new PosTag { Code = "IMPF", ArabicLabel = "فعل مضارع", EnglishLabel = "Imperfect", Category = "verb", SortOrder = 15, Description = "Present/imperfect verb form" },
        new PosTag { Code = "ACC", ArabicLabel = "حرف نصب", EnglishLabel = "Accusative Particle", Category = "particle", SortOrder = 16 },
        new PosTag { Code = "EMPH", ArabicLabel = "حرف تأكيد", EnglishLabel = "Emphatic", Category = "particle", SortOrder = 17 },
        new PosTag { Code = "REM", ArabicLabel = "حرف استثناء", EnglishLabel = "Exceptive", Category = "particle", SortOrder = 18 },
        new PosTag { Code = "ANS", ArabicLabel = "حرف جواب", EnglishLabel = "Answer Particle", Category = "particle", SortOrder = 19 },
        new PosTag { Code = "PRO", ArabicLabel = "ضمير منفصل", EnglishLabel = "Independent Pronoun", Category = "noun", SortOrder = 20 },
        new PosTag { Code = "FUT", ArabicLabel = "حرف استقبال", EnglishLabel = "Future Particle", Category = "particle", SortOrder = 21 },
        new PosTag { Code = "INTG", ArabicLabel = "حرف استفهام", EnglishLabel = "Interrogative", Category = "particle", SortOrder = 22 },
        new PosTag { Code = "COND", ArabicLabel = "حرف شرط", EnglishLabel = "Conditional", Category = "particle", SortOrder = 23 },
        new PosTag { Code = "PREV", ArabicLabel = "حرف تحضيض", EnglishLabel = "Preventive", Category = "particle", SortOrder = 24 },
        new PosTag { Code = "CAUS", ArabicLabel = "حرف سببية", EnglishLabel = "Causative", Category = "particle", SortOrder = 25 },
        new PosTag { Code = "AMD", ArabicLabel = "حرف عطف / نفي", EnglishLabel = "Amendment Particle", Category = "particle", SortOrder = 26 },
        new PosTag { Code = "EXL", ArabicLabel = "حرف تعليل", EnglishLabel = "Explanation", Category = "particle", SortOrder = 27 },
        new PosTag { Code = "RES", ArabicLabel = "حرف ردع", EnglishLabel = "Restriction", Category = "particle", SortOrder = 28 },
        new PosTag { Code = "PRP", ArabicLabel = "حرف غاية", EnglishLabel = "Purpose", Category = "particle", SortOrder = 29 },
        new PosTag { Code = "COM", ArabicLabel = "واو المعية", EnglishLabel = "Comitative", Category = "particle", SortOrder = 30 },
        new PosTag { Code = "T", ArabicLabel = "تاء تأنيث", EnglishLabel = "Feminine Marker", Category = "other", SortOrder = 31, Description = "Feminine suffix marker" },
        new PosTag { Code = "LOC", ArabicLabel = "ظرف مكان", EnglishLabel = "Locative Adverb", Category = "noun", SortOrder = 32 },
        new PosTag { Code = "TIM", ArabicLabel = "ظرف زمان", EnglishLabel = "Temporal Adverb", Category = "noun", SortOrder = 33 },
        new PosTag { Code = "ABR", ArabicLabel = "مختصر", EnglishLabel = "Abbreviation", Category = "other", SortOrder = 34 }
    ];
}
