using QuranDashboard.Application.Abstractions.Abwab.Templates;

namespace QuranDashboard.Application.Abwab.Templates;

public sealed class AbwabTemplateWriteHandler(
    TemplateAggregateHandler aggregate,
    TemplateNodeHandler structure,
    TemplateAliasHandler alias,
    TemplateApplicationHandler application) : IAbwabTemplateWritePort
{
    public Task<Guid> AddDoorTemplateAsync(AddDoorTemplateCommand command, CancellationToken cancellationToken) =>
        aggregate.AddAsync(command, cancellationToken);

    public Task EditDoorTemplateAsync(EditDoorTemplateCommand command, CancellationToken cancellationToken) =>
        aggregate.EditAsync(command, cancellationToken);

    public Task DeleteDoorTemplateAsync(DeleteDoorTemplateCommand command, CancellationToken cancellationToken) =>
        aggregate.DeleteAsync(command, cancellationToken);

    public Task RestoreDoorTemplateAsync(RestoreDoorTemplateCommand command, CancellationToken cancellationToken) =>
        aggregate.RestoreAsync(command, cancellationToken);

    public Task<Guid> AddTemplateNodeAsync(AddTemplateNodeCommand command, CancellationToken cancellationToken) =>
        structure.AddAsync(command, cancellationToken);

    public Task EditTemplateNodeAsync(EditTemplateNodeCommand command, CancellationToken cancellationToken) =>
        structure.EditAsync(command, cancellationToken);

    public Task ReparentTemplateNodeAsync(ReparentTemplateNodeCommand command, CancellationToken cancellationToken) =>
        structure.ReparentAsync(command, cancellationToken);

    public Task ReorderTemplateNodesAsync(ReorderTemplateNodesCommand command, CancellationToken cancellationToken) =>
        structure.ReorderAsync(command, cancellationToken);

    public Task RemoveTemplateNodeAsync(RemoveTemplateNodeCommand command, CancellationToken cancellationToken) =>
        structure.RemoveAsync(command, cancellationToken);

    public Task<Guid> AddTemplateNodeAliasAsync(AddTemplateNodeAliasCommand command, CancellationToken cancellationToken) =>
        alias.AddAsync(command, cancellationToken);

    public Task EditTemplateNodeAliasAsync(EditTemplateNodeAliasCommand command, CancellationToken cancellationToken) =>
        alias.EditAsync(command, cancellationToken);

    public Task RemoveTemplateNodeAliasAsync(RemoveTemplateNodeAliasCommand command, CancellationToken cancellationToken) =>
        alias.RemoveAsync(command, cancellationToken);

    public Task RestoreTemplateNodeAliasAsync(RestoreTemplateNodeAliasCommand command, CancellationToken cancellationToken) =>
        alias.RestoreAsync(command, cancellationToken);

    public Task ApplyDoorTemplateAsync(ApplyDoorTemplateCommand command, CancellationToken cancellationToken) =>
        application.ApplyAsync(command, cancellationToken);
}
