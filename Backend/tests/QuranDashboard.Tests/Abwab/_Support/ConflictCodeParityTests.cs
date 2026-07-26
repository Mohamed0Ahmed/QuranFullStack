using System.Reflection;
using Microsoft.AspNetCore.Http;
using QuranDashboard.Api.Abwab;
using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Tests.Abwab._Support;

public sealed class ConflictCodeParityTests
{
    private static readonly string[] ExpectedCodes =
    [
        "abwab.section_name_conflict",
        "abwab.section_not_empty",
        "abwab.permanent_default_section",
        "abwab.category_name_conflict",
        "abwab.category_alias_conflict",
        "abwab.category_cycle",
        "abwab.category_overlapping_move",
        "abwab.category_unavailable",
        "abwab.category_reserved_by_pending",
        "abwab.manual_protection",
        "abwab.manual_protection_scope_conflict",
        "abwab.ordinary_protection",
        "abwab.relationship_duplicate",
        "abwab.relationship_cycle",
        "abwab.template_cycle",
        "abwab.template_revision_stale",
        "abwab.stabilization_active",
        "abwab.tree_revision_stale",
        "abwab.timeline_generation_stale",
        "abwab.row_stale",
    ];

    [Fact]
    public void EveryExpectedCode_IsDeclaredExactlyOnce_InAbwabConflictCodes()
    {
        var declaredValues = typeof(AbwabConflictCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        foreach (var code in ExpectedCodes)
        {
            declaredValues.Should().Contain(code);
        }

        declaredValues.Should().OnlyHaveUniqueItems("no code may be declared twice under different names");
    }

    [Fact]
    public void EveryNewWriteConflictCode_MapsToA409_WithTheExactCodeEchoedInErrors()
    {
        var newCodes = new[]
        {
            AbwabConflictCodes.SectionNameConflict,
            AbwabConflictCodes.SectionNotEmpty,
            AbwabConflictCodes.PermanentDefaultSection,
            AbwabConflictCodes.CategoryNameConflict,
            AbwabConflictCodes.CategoryAliasConflict,
            AbwabConflictCodes.CategoryCycle,
            AbwabConflictCodes.CategoryOverlappingMove,
            AbwabConflictCodes.CategoryUnavailable,
            AbwabConflictCodes.CategoryReservedByPending,
            AbwabConflictCodes.ManualProtection,
            AbwabConflictCodes.ManualProtectionScopeConflict,
            AbwabConflictCodes.OrdinaryProtection,
            AbwabConflictCodes.RelationshipDuplicate,
            AbwabConflictCodes.RelationshipCycle,
            AbwabConflictCodes.TemplateCycle,
            AbwabConflictCodes.TemplateRevisionStale,
            AbwabConflictCodes.RowStale,
            AbwabConflictCodes.TreeRevisionStale,
        };

        foreach (var code in newCodes)
        {
            var exception = new AbwabWriteConflictException(code, "message");

            var mapped = AbwabConflictResponses.TryMap(exception, out var statusCode, out var response);

            mapped.Should().BeTrue($"'{code}' must be mapped by AbwabConflictResponses");
            statusCode.Should().Be(StatusCodes.Status409Conflict);
            response.Errors.Should().ContainSingle().Which.Should().Be(code, "the exact code must be echoed, never renamed/remapped");
        }
    }

    [Fact]
    public void StabilizationActive_MapsToA409_ForTheSharedBarrierException()
    {
        var exception = new AbwabStabilizationActiveException(QuranDashboard.Domain.Abwab.Concurrency.AbwabWriteBarrierState.Stabilizing);

        var mapped = AbwabConflictResponses.TryMap(exception, out var statusCode, out var response);

        mapped.Should().BeTrue();
        statusCode.Should().Be(StatusCodes.Status409Conflict);
        response.Errors.Should().ContainSingle().Which.Should().Be(AbwabConflictCodes.StabilizationActive);
    }

    [Fact]
    public void TimelineGenerationStale_MapsToA409_ForTheSharedGenerationException()
    {
        var exception = new AbwabTimelineGenerationStaleException(0, 1);

        var mapped = AbwabConflictResponses.TryMap(exception, out var statusCode, out var response);

        mapped.Should().BeTrue();
        statusCode.Should().Be(StatusCodes.Status409Conflict);
        response.Errors.Should().ContainSingle().Which.Should().Be(AbwabConflictCodes.TimelineGenerationStale);
    }

    [Fact]
    public void NoAbwabConflictCode_IsEverInventedOutsideThisExactSet()
    {
        var declaredValues = typeof(AbwabConflictCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .Where(IsDomainScopedCode)
            .ToList();

        declaredValues.Should().BeEquivalentTo(ExpectedCodes.Where(IsDomainScopedCode));
    }

    private static bool IsDomainScopedCode(string code) =>
        code.StartsWith("abwab.category_", StringComparison.Ordinal) ||
        code.StartsWith("abwab.section_", StringComparison.Ordinal) ||
        code.StartsWith("abwab.manual_protection", StringComparison.Ordinal) ||
        code.StartsWith("abwab.ordinary_protection", StringComparison.Ordinal) ||
        code.StartsWith("abwab.relationship_", StringComparison.Ordinal) ||
        code.StartsWith("abwab.template_", StringComparison.Ordinal) ||
        code.StartsWith("abwab.tree_revision", StringComparison.Ordinal) ||
        code == AbwabConflictCodes.PermanentDefaultSection ||
        code == AbwabConflictCodes.RowStale;
}
