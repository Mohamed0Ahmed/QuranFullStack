using System.Threading.Channels;
using QuranDashboard.Infrastructure.Caching.Linking;

namespace QuranDashboard.Infrastructure.Background;

internal sealed class LinkingJobQueueSignal
{
    private readonly Channel<bool> preparedPreflights;
    private readonly Channel<bool> confirmationJobs;

    public LinkingJobQueueSignal(LinkingScalabilityOptions options)
    {
        preparedPreflights = CreateSignal(options.PreflightProcessorConcurrency);
        confirmationJobs = CreateSignal(options.ConfirmationProcessorConcurrency);
    }

    public void NotifyPreparedPreflightQueued() => preparedPreflights.Writer.TryWrite(true);

    public void NotifyConfirmationJobQueued() => confirmationJobs.Writer.TryWrite(true);

    public async ValueTask WaitForPreparedPreflightAsync(CancellationToken cancellationToken) =>
        _ = await preparedPreflights.Reader.ReadAsync(cancellationToken);

    public async ValueTask WaitForConfirmationJobAsync(CancellationToken cancellationToken) =>
        _ = await confirmationJobs.Reader.ReadAsync(cancellationToken);

    private static Channel<bool> CreateSignal(int capacity) =>
        Channel.CreateBounded<bool>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });
}
