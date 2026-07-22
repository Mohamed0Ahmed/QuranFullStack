using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab.Persistence;

namespace QuranDashboard.Tests.Abwab.Kernel._Support;

// Foundation-only fixture descriptors standing in for the future 029+ domain writers, without depending
// on any real workspace type. They exercise the kernel guards structurally (§18.2 step 3).

// A hypothetical Abwab domain write target: audited + soft-deletable. Not a real domain entity and not
// mapped by production migrations; the kernel test context maps it to a throwaway table.
public sealed class KernelFixtureWriteTarget : IAbwabAuditable
{
    public Guid Id { get; set; }

    public string Label { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}

// A writer that carries the generation contract (compliant). Lives in the test assembly, so production
// writer discovery never sees it.
public sealed class FixtureCompliantMutationCommand : IAbwabMutationCommand
{
    public ExpectedTimelineGeneration ExpectedTimelineGeneration => ExpectedTimelineGeneration.Of(0);
}

// A writer that FORGOT the generation contract — the exact mistake the coverage/registry guards catch.
public sealed class FixtureWriterMissingGeneration : IAbwabWriter
{
}

// An actionable read that carries the generation contract (compliant).
public sealed class FixtureCompliantActionableRead : IAbwabActionableRead
{
    public ExpectedTimelineGeneration ExpectedTimelineGeneration => ExpectedTimelineGeneration.Of(0);
}
