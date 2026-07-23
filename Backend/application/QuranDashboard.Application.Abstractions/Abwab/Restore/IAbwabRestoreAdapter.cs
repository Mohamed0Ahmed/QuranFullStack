namespace QuranDashboard.Application.Abstractions.Abwab.Restore;

public interface IAbwabRestoreAdapter<TProduct, TSnapshot> : IAbwabRestoreAdapterDescriptor
{
    TSnapshot Capture(TProduct product);

    TProduct Reconstruct(TSnapshot snapshot);
}
