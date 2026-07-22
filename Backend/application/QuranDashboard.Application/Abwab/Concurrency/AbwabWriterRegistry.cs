using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Concurrency;

// Records every Abwab writer that has been wired to run through the barrier-gated write executor.
// Populated at composition (stabilization guard); the registry test cross-checks it against the set of
// IAbwabWriter types discovered by reflection and fails if any discovered writer is absent here.
public sealed class AbwabWriterRegistry
{
    private readonly HashSet<Type> _registered = [];

    public void Register(Type writerType)
    {
        ArgumentNullException.ThrowIfNull(writerType);
        if (!typeof(IAbwabWriter).IsAssignableFrom(writerType))
        {
            throw new ArgumentException($"'{writerType.Name}' is not an {nameof(IAbwabWriter)}.", nameof(writerType));
        }

        _registered.Add(writerType);
    }

    public void Register<TWriter>() where TWriter : IAbwabWriter => _registered.Add(typeof(TWriter));

    public IReadOnlySet<Type> RegisteredWriters => _registered;
}
