namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabSectionParentMismatchException : Exception
{
    public AbwabSectionParentMismatchException()
        : base("A nested door's section is its parent's; the stated section disagrees with it.")
    {
    }
}
