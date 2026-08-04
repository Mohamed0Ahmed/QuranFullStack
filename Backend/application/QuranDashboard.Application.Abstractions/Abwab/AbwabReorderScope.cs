namespace QuranDashboard.Application.Abstractions.Abwab;

public enum AbwabReorderScope
{
    // Never 0: Scope is non-nullable, so an omitted scope would deserialize to it and pass Enum.IsDefined.
    Section = 1,
    Global = 2,
}
