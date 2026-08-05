namespace QuranDashboard.Domain.Access;

public sealed class Permission
{
    private Permission()
    {
    }

    public Permission(
        string code,
        string arabicLabel,
        string englishDescription,
        int displayOrder)
    {
        Code = code;
        ArabicLabel = arabicLabel;
        EnglishDescription = englishDescription;
        DisplayOrder = displayOrder;
    }

    public int Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string ArabicLabel { get; private set; } = string.Empty;
    public string EnglishDescription { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public DateTimeOffset? RetiredAtUtc { get; private set; }

    public ICollection<UserPermission> UserPermissions { get; } = new List<UserPermission>();

    public void UpdateMetadata(
        string arabicLabel,
        string englishDescription,
        int displayOrder)
    {
        ArabicLabel = arabicLabel;
        EnglishDescription = englishDescription;
        DisplayOrder = displayOrder;
    }
}
