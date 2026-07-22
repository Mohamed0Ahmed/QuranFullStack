using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab.Persistence;

namespace QuranDashboard.Tests.Abwab.Kernel._Support;

public sealed class KernelFixtureWriteTarget : IAbwabAuditable
{
    public Guid Id { get; set; }

    public string Label { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}

public sealed class FixtureCompliantMutationCommand : IAbwabMutationCommand
{
    public ExpectedTimelineGeneration ExpectedTimelineGeneration => ExpectedTimelineGeneration.Of(0);
}

public sealed class FixtureWriterMissingGeneration : IAbwabWriter
{
}

public sealed class FixtureCompliantActionableRead : IAbwabActionableRead
{
    public ExpectedTimelineGeneration ExpectedTimelineGeneration => ExpectedTimelineGeneration.Of(0);
}
