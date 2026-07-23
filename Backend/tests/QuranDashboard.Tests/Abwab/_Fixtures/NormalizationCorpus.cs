namespace QuranDashboard.Tests.Abwab._Fixtures;

public static class NormalizationCorpus
{
    public sealed record Case(string Name, string Input, string Expected);

    public static IReadOnlyList<Case> Cases { get; } = BuildCases();

    private static string Cp(int codepoint) => char.ConvertFromUtf32(codepoint);

    private const string B = "ب";
    private const string T = "ت";

    private static IReadOnlyList<Case> BuildCases() =>
    [
        new("MarkRange1_FirstRemoved_U0610", B + Cp(0x0610) + T, B + T),
        new("MarkRange1_LastRemoved_U061A", B + Cp(0x061A) + T, B + T),
        new("MarkRange1_GapBeforePreserved_U060F", B + Cp(0x060F) + T, B + Cp(0x060F) + T),
        new("MarkRange1_GapAfterPreserved_U061B", B + Cp(0x061B) + T, B + Cp(0x061B) + T),

        new("MarkRange2_FirstRemoved_U064B", B + Cp(0x064B) + T, B + T),
        new("MarkRange2_LastRemoved_U065F", B + Cp(0x065F) + T, B + T),
        new("MarkRange2_GapBeforePreserved_U064A_Yeh", B + Cp(0x064A) + T, B + Cp(0x064A) + T),
        new("MarkRange2_GapAfterPreserved_U0660_Digit", B + Cp(0x0660) + T, B + Cp(0x0660) + T),

        new("MarkRange3_SingleRemoved_U0670", B + Cp(0x0670) + T, B + T),
        new("MarkRange3_GapBeforePreserved_U066F", B + Cp(0x066F) + T, B + Cp(0x066F) + T),
        new("MarkRange3_GapAfterIsAlefWasla_MapsNotStrips_U0671", B + Cp(0x0671) + T, B + Cp(0x0627) + T),

        new("MarkRange4_FirstRemoved_U06D6", B + Cp(0x06D6) + T, B + T),
        new("MarkRange4_LastRemoved_U06DC", B + Cp(0x06DC) + T, B + T),
        new("MarkRange4_GapBeforePreserved_U06D5", B + Cp(0x06D5) + T, B + Cp(0x06D5) + T),
        new("MarkRange4_GapAfterPreserved_U06DD", B + Cp(0x06DD) + T, B + Cp(0x06DD) + T),

        new("MarkRange5_FirstRemoved_U06DF", B + Cp(0x06DF) + T, B + T),
        new("MarkRange5_LastRemoved_U06E4", B + Cp(0x06E4) + T, B + T),
        new("MarkRange5_GapBeforePreserved_U06DE", B + Cp(0x06DE) + T, B + Cp(0x06DE) + T),
        new("MarkRange5_GapAfterPreserved_U06E5", B + Cp(0x06E5) + T, B + Cp(0x06E5) + T),

        new("MarkRange6_FirstRemoved_U06E7", B + Cp(0x06E7) + T, B + T),
        new("MarkRange6_LastRemoved_U06E8", B + Cp(0x06E8) + T, B + T),
        new("MarkRange6_GapBeforePreserved_U06E6", B + Cp(0x06E6) + T, B + Cp(0x06E6) + T),

        new("MarkRange7_FirstRemoved_U06EA", B + Cp(0x06EA) + T, B + T),
        new("MarkRange7_LastRemoved_U06ED", B + Cp(0x06ED) + T, B + T),
        new("MarkRange6And7_SharedGapPreserved_U06E9", B + Cp(0x06E9) + T, B + Cp(0x06E9) + T),
        new("MarkRange7_GapAfterPreserved_U06EE", B + Cp(0x06EE) + T, B + Cp(0x06EE) + T),

        new("MarkRange8_FirstRemoved_U0897", B + Cp(0x0897) + T, B + T),
        new("MarkRange8_LastRemoved_U089F", B + Cp(0x089F) + T, B + T),
        new("MarkRange8_GapBeforePreserved_U0896", B + Cp(0x0896) + T, B + Cp(0x0896) + T),
        new("MarkRange8_GapAfterPreserved_U08A0", B + Cp(0x08A0) + T, B + Cp(0x08A0) + T),

        new("MarkRange9_FirstRemoved_U08CA", B + Cp(0x08CA) + T, B + T),
        new("MarkRange9_LastRemoved_U08E1", B + Cp(0x08E1) + T, B + T),
        new("MarkRange9_GapBeforePreserved_U08C9", B + Cp(0x08C9) + T, B + Cp(0x08C9) + T),

        new("MarkRange10_FirstRemoved_U08E3", B + Cp(0x08E3) + T, B + T),
        new("MarkRange10_LastRemoved_U08FF", B + Cp(0x08FF) + T, B + T),
        new("MarkRange9And10_SharedGapPreserved_U08E2", B + Cp(0x08E2) + T, B + Cp(0x08E2) + T),
        new("MarkRange10_GapAfterPreserved_U0900_Devanagari", B + Cp(0x0900) + T, B + Cp(0x0900) + T),

        new("MarkRange11_FirstRemoved_U10EFC_SupplementaryPlane", B + Cp(0x10EFC) + T, B + T),
        new("MarkRange11_LastRemoved_U10EFF_SupplementaryPlane", B + Cp(0x10EFF) + T, B + T),
        new("MarkRange11_GapBeforePreserved_U10EFB_SupplementaryPlane", B + Cp(0x10EFB) + T, B + Cp(0x10EFB) + T),
        new("MarkRange11_GapAfterPreserved_U10F00_OldSogdian", B + Cp(0x10F00) + T, B + Cp(0x10F00) + T),

        new("TatweelRemoved", B + Cp(0x0640) + T, B + T),

        new("AlefHamzaAboveMapsToAlef_U0623", Cp(0x0623), Cp(0x0627)),
        new("AlefHamzaBelowMapsToAlef_U0625", Cp(0x0625), Cp(0x0627)),
        new("AlefMaddaMapsToAlef_U0622", Cp(0x0622), Cp(0x0627)),
        new("AlefWaslaMapsToAlef_U0671", Cp(0x0671), Cp(0x0627)),

        new("DecomposedAlefHamzaAbove_ComposesThenMapsToAlef", Cp(0x0627) + Cp(0x0654), Cp(0x0627)),
        new("DecomposedAlefHamzaBelow_ComposesThenMapsToAlef", Cp(0x0627) + Cp(0x0655), Cp(0x0627)),
        new("DecomposedAlefMadda_ComposesThenMapsToAlef", Cp(0x0627) + Cp(0x0653), Cp(0x0627)),

        new("AlefMaqsuraMapsToYeh_U0649", Cp(0x0649), Cp(0x064A)),

        new("TehMarbutaNotMappedToHeh_U0629", Cp(0x0629), Cp(0x0629)),
        new("TehMarbutaSurvivesWithSandwichLetters", B + Cp(0x0629) + T, B + Cp(0x0629) + T),

        new("LeadingTrailingWhitespaceTrimmed", "  " + B + T + "  ", B + T),
        new("MultipleAsciiSpacesCollapseToOne", B + T + "   " + T + B, B + T + " " + T + B),
        new("NoBreakSpaceCollapsesToAsciiSpace", B + T + Cp(0x00A0) + T + B, B + T + " " + T + B),
        new("TabCollapsesToAsciiSpace", B + T + "\t" + T + B, B + T + " " + T + B),
        new("NewlineCollapsesToAsciiSpace", B + T + "\n" + T + B, B + T + " " + T + B),
        new("IdeographicSpaceCollapsesToAsciiSpace", B + T + Cp(0x3000) + T + B, B + T + " " + T + B),
        new("ThinSpaceCollapsesToAsciiSpace", B + T + Cp(0x2009) + T + B, B + T + " " + T + B),
        new("MixedWhitespaceRunCollapsesToOneSpace", B + T + " \t" + Cp(0x00A0) + T + B, B + T + " " + T + B),

        new(
            "IntegrationExample_SectionWord",
            "  " + Cp(0x0623) + Cp(0x0642) + Cp(0x0633) + Cp(0x0627) + Cp(0x0645) + "  ",
            Cp(0x0627) + Cp(0x0642) + Cp(0x0633) + Cp(0x0627) + Cp(0x0645)),
    ];
}
