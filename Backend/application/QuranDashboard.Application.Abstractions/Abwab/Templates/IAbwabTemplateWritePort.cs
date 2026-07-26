namespace QuranDashboard.Application.Abstractions.Abwab.Templates;

public interface IAbwabTemplateWritePort
{
    Task<Guid> AddDoorTemplateAsync(AddDoorTemplateCommand command, CancellationToken cancellationToken);

    Task EditDoorTemplateAsync(EditDoorTemplateCommand command, CancellationToken cancellationToken);

    Task DeleteDoorTemplateAsync(DeleteDoorTemplateCommand command, CancellationToken cancellationToken);

    Task RestoreDoorTemplateAsync(RestoreDoorTemplateCommand command, CancellationToken cancellationToken);

    Task<Guid> AddTemplateNodeAsync(AddTemplateNodeCommand command, CancellationToken cancellationToken);

    Task EditTemplateNodeAsync(EditTemplateNodeCommand command, CancellationToken cancellationToken);

    Task ReparentTemplateNodeAsync(ReparentTemplateNodeCommand command, CancellationToken cancellationToken);

    Task ReorderTemplateNodesAsync(ReorderTemplateNodesCommand command, CancellationToken cancellationToken);

    Task RemoveTemplateNodeAsync(RemoveTemplateNodeCommand command, CancellationToken cancellationToken);

    Task<Guid> AddTemplateNodeAliasAsync(AddTemplateNodeAliasCommand command, CancellationToken cancellationToken);

    Task EditTemplateNodeAliasAsync(EditTemplateNodeAliasCommand command, CancellationToken cancellationToken);

    Task RemoveTemplateNodeAliasAsync(RemoveTemplateNodeAliasCommand command, CancellationToken cancellationToken);

    Task RestoreTemplateNodeAliasAsync(RestoreTemplateNodeAliasCommand command, CancellationToken cancellationToken);

    Task ApplyDoorTemplateAsync(ApplyDoorTemplateCommand command, CancellationToken cancellationToken);
}
