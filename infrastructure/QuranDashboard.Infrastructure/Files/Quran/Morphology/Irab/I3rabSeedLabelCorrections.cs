namespace QuranDashboard.Infrastructure.Files.Quran.Morphology.Irab;

/// <summary>
/// The 21 rule-layer label corrections (FR-011). These are the signatures whose Arabic label the
/// catalogue owns instead of reusing <c>quran_pos_tags.arabic_label</c> — the seed labels were either
/// wrong or imprecise against real corpus usage (coverage report §7). The list is the authoritative
/// set behind the <c>I3RAB-LABEL-REVIEW</c> warning; it counts how many of these corrected rules are
/// present after a run. The corrected Arabic itself lives in <see cref="I3rabRuleCatalogSeedData"/>
/// (the single source of labels) — this file only records which signatures are corrections.
/// </summary>
internal static class I3rabSeedLabelCorrections
{
    internal static IReadOnlyList<string> Signatures { get; } =
    [
        "STEM:T",            // ظرف زمان        (seed: تاء تأنيث)
        "STEM:SUB",          // حرف مصدري       (seed: اسم مبهم)
        "STEM:RES",          // أداة حصر         (seed: حرف ردع)
        "STEM:INTG",         // اسم استفهام      (seed: حرف استفهام)
        "PREFIX:INTG",       // همزة استفهام     (seed: حرف استفهام)
        "STEM:AMD",          // حرف استدراك      (seed: حرف عطف / نفي)
        "STEM:SUP",          // حرف زائد         (rule-layer owned)
        "STEM:PREV",         // ما الكافّة       (seed: حرف تحضيض)
        "STEM:INC",          // حرف ابتداء/استفتاح (seed: حرف ابتداء)
        "STEM:EXL",          // حرف تفصيل        (seed: حرف تعليل)
        "STEM:INT",          // حرف تفسير        (rule-layer owned)
        "STEM:EXH",          // حرف تحضيض        (rule-layer owned)
        "STEM:SUR",          // حرف فجاءة        (seed: إذا الفجائية)
        "STEM:INL",          // حروف مقطّعة (فواتح السور) (seed: قسم)
        "PREFIX:EQ",         // همزة التسوية     (seed: حرف تسوية)
        "SUFFIX:VOC",        // ميم عوض عن حرف النداء (seed: حرف نداء)
        "PREFIX:COM",        // واو المعية       (rule-layer owned)
        "SUFFIX:P",          // لام الجر         (seed: حرف جر)
        "STEM:N:GEN:1S",     // اسم مجرور مضاف إلى ياء المتكلم (seed: اسم مجرور)
        "PREFIX:REM",        // حرف استئناف      (seed: حرف استثناء)
        "PREFIX:IMPV",       // لام الأمر        (seed: فعل أمر)
    ];
}
