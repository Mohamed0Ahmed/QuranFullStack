namespace QuranDashboard.Application.Abstractions.Linking.DoorLinks;

public enum DoorLinkSelectionMode
{
    Only = 0,
    AllExcept = 1,
}

public sealed record DoorLinkSelection(
    DoorLinkSelectionMode Mode,
    IReadOnlyList<long> UnitIds);

public static class DoorLinkSelectionModeTokens
{
    public const string Only = "only";
    public const string AllExcept = "all_except";

    public static bool TryParse(string? token, out DoorLinkSelectionMode mode)
    {
        mode = token switch
        {
            Only => DoorLinkSelectionMode.Only,
            AllExcept => DoorLinkSelectionMode.AllExcept,
            _ => default,
        };

        return token is Only or AllExcept;
    }
}
