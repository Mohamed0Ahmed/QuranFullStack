using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Concurrency;

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
