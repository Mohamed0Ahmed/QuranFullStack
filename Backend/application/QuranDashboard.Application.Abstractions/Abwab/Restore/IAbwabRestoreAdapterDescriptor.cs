namespace QuranDashboard.Application.Abstractions.Abwab.Restore;

public interface IAbwabRestoreAdapterDescriptor
{
    string PersistedType { get; }

    int SnapshotSchemaVersion { get; }
}
