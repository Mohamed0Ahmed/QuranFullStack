namespace QuranDashboard.TestArtifacts;

internal static class Program
{
    internal static int Main(string[] args)
    {
        return ArtifactTrustCommand.Execute(args, Console.Out, Console.Error);
    }
}
