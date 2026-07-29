using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abstractions.Abwab;

// One use case: copy a template subtree into N target doors. Its seam crosses two aggregates by
// design — it reads abwab_template_nodes and writes abwab_doors — which is why it is neither the
// templates writer nor a method on the doors writer.
public interface IAbwabTemplateApplyWriter
{
    // Returns the created ROOT door per target. All-or-nothing: any refusal fails the whole batch
    // before anything is inserted.
    Task<IReadOnlyList<AbwabDoorDto>> ApplyAsync(
        int templateId,
        IReadOnlyList<int> targetDoorIds,
        CancellationToken cancellationToken);
}
