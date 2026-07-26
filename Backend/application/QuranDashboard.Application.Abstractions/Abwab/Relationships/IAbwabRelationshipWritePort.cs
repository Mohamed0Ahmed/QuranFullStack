namespace QuranDashboard.Application.Abstractions.Abwab.Relationships;

public interface IAbwabRelationshipWritePort
{
    Task<Guid> AddAsync(AddRelationshipCommand command, CancellationToken cancellationToken);

    Task EditAsync(EditRelationshipCommand command, CancellationToken cancellationToken);

    Task DeleteAsync(DeleteRelationshipCommand command, CancellationToken cancellationToken);

    Task RestoreAsync(RestoreRelationshipCommand command, CancellationToken cancellationToken);
}
