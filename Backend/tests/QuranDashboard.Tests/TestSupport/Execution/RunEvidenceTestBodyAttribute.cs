using System.Diagnostics;
using System.Reflection;
using QuranDashboard.TestRuntime;
using Xunit.Sdk;

[assembly: QuranDashboard.Tests.TestSupport.Execution.RunEvidenceTestBody]

namespace QuranDashboard.Tests.TestSupport.Execution;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RunEvidenceTestBodyAttribute : BeforeAfterTestAttribute
{
    private readonly Stopwatch stopwatch = new();

    public override void Before(MethodInfo methodUnderTest)
    {
        stopwatch.Restart();
    }

    public override void After(MethodInfo methodUnderTest)
    {
        stopwatch.Stop();
        RunEvidenceTelemetry.RecordSubPhase("testBody", stopwatch.ElapsedMilliseconds);
    }
}
