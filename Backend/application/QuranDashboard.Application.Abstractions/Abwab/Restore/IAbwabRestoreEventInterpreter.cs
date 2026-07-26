namespace QuranDashboard.Application.Abstractions.Abwab.Restore;

// An event-kind interpreter is deliberately NOT an IAbwabRestoreAdapterDescriptor: it owns no
// persisted type and adds no §8 registry entry — it maps one audited event kind onto the adapters
// that already own the rows the event created.
public interface IAbwabRestoreEventInterpreter
{
    string EventKind { get; }

    int InterpreterSchemaVersion { get; }
}
