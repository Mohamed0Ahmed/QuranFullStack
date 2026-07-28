namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabParentNotFoundException : Exception
{
    public AbwabParentNotFoundException()
        : base("The referenced parent door does not exist or is archived.")
    {
    }
}
