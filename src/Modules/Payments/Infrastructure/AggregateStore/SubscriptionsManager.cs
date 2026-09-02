using JasperFx.Events.Daemon;
using Microsoft.Extensions.Logging.Abstractions;
using Polecat;

namespace CompanyName.MyMeetings.Modules.Payments.Infrastructure.AggregateStore;

public class SubscriptionsManager
{
    private readonly DocumentStore _store;
    private IProjectionDaemon _daemon;

    public SubscriptionsManager(IDocumentStore store)
    {
        _store = (DocumentStore)store;
    }

    public void Start()
    {
        _store.Database.ApplyAllConfiguredChangesToDatabaseAsync().GetAwaiter().GetResult();
        _daemon = _store.BuildProjectionDaemonAsync(logger: NullLogger.Instance).GetAwaiter().GetResult();
        _daemon.StartAllAsync().GetAwaiter().GetResult();
    }

    public void Stop()
    {
        _daemon?.StopAllAsync().GetAwaiter().GetResult();
        _daemon?.Dispose();
    }
}
