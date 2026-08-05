namespace QuranDashboard.Tests.TestSupport.DependencyInjection;

public sealed class OwnedServiceProviderRegistryTests
{
    [Fact]
    public async Task DisposeAsync_DisposesOwnedProviders_InReverseCreationOrder()
    {
        var disposalOrder = new List<string>();
        var registry = new OwnedServiceProviderRegistry();
        registry.Own(BuildProvider("first", disposalOrder));
        registry.Own(BuildProvider("second", disposalOrder));
        registry.Own(BuildProvider("third", disposalOrder));

        await registry.DisposeAsync();

        disposalOrder.Should().Equal("third", "second", "first");
        registry.DisposalFailures.Should().BeEmpty();
    }

    [Fact]
    public async Task DisposeAsync_ContinuesPastAFailingProvider_AndRecordsTheFailure()
    {
        var disposalOrder = new List<string>();
        var registry = new OwnedServiceProviderRegistry();
        registry.Own(BuildProvider("first", disposalOrder));
        registry.Own(BuildProvider("failing", disposalOrder, throwOnDispose: true));
        registry.Own(BuildProvider("third", disposalOrder));

        await registry.DisposeAsync();

        disposalOrder.Should().Equal(
            "third",
            "failing",
            "first");
        registry.DisposalFailures.Should().ContainSingle()
            .Which.Message.Should().Contain("failing");
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent()
    {
        var disposalOrder = new List<string>();
        var registry = new OwnedServiceProviderRegistry();
        registry.Own(BuildProvider("only", disposalOrder));

        await registry.DisposeAsync();
        await registry.DisposeAsync();

        disposalOrder.Should().Equal("only");
    }

    [Fact]
    public async Task Own_IsRefused_AfterDisposal()
    {
        var disposalOrder = new List<string>();
        var registry = new OwnedServiceProviderRegistry();
        await registry.DisposeAsync();

        var own = () => registry.Own(BuildProvider("late", disposalOrder));

        own.Should().Throw<ObjectDisposedException>();
        disposalOrder.Should().BeEmpty();
    }

    private static ServiceProvider BuildProvider(
        string name,
        IList<string> disposalOrder,
        bool throwOnDispose = false)
    {
        var provider = new ServiceCollection()
            .AddSingleton(_ => new DisposalProbe(name, disposalOrder, throwOnDispose))
            .BuildServiceProvider();

        provider.GetRequiredService<DisposalProbe>();
        return provider;
    }

    private sealed class DisposalProbe(string name, IList<string> disposalOrder, bool throwOnDispose) : IDisposable
    {
        public void Dispose()
        {
            disposalOrder.Add(name);
            if (throwOnDispose)
            {
                throw new InvalidOperationException($"Disposing the '{name}' provider failed on purpose.");
            }
        }
    }
}
