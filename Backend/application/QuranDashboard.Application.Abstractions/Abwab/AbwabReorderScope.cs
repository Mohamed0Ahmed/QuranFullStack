namespace QuranDashboard.Application.Abstractions.Abwab;

// System.Text.Json leaves an omitted/unrecognized enum property at its default (0), so Section cannot
// be 0 — that would make an absent scope silently mean Section instead of the caller bug it is
// (plan §6). The controller guards with Enum.IsDefined and refuses a missing/unknown scope with 400.
public enum AbwabReorderScope
{
    Section = 1,
    Global = 2,
}
