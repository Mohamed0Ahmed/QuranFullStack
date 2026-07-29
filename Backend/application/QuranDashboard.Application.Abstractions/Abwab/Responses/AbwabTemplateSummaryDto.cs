namespace QuranDashboard.Application.Abstractions.Abwab.Responses;

// NodeCount counts the root's live descendants and excludes the root itself — the list's
// «N عناصر» chip, matching the design contract's own countNodes().
public sealed record AbwabTemplateSummaryDto(int Id, string Name, int NodeCount);
