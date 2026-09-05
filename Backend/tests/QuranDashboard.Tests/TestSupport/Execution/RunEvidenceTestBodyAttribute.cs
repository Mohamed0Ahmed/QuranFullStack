using System.Diagnostics;
using System.Reflection;
using QuranDashboard.TestRuntime;
using Xunit.Sdk;

[assembly: QuranDashboard.Tests.TestSupport.Execution.RunEvidenceTestBody]

namespace QuranDashboard.Tests.TestSupport.Execution;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RunEvidenceTestBodyAttribute : BeforeAfterTestAttribute
{
    private static readonly AsyncLocal<Stopwatch?> Current = new();

    public override void Before(MethodInfo methodUnderTest)
    {
        Current.Value = Stopwatch.StartNew();
    }

    public override void After(MethodInfo methodUnderTest)
    {
        var stopwatch = Current.Value;
        Current.Value = null;
        if (stopwatch is null)
        {
            return;
        }

        stopwatch.Stop();
        RunEvidenceTelemetry.RecordSubPhase("testBody", stopwatch.ElapsedMilliseconds);
    }
}
