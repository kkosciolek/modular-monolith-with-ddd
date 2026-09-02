using Autofac;
using CompanyName.MyMeetings.BuildingBlocks.Domain;
using CompanyName.MyMeetings.Modules.Payments.Application.Configuration.Projections;
using CompanyName.MyMeetings.Modules.Payments.Infrastructure.Configuration;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Polecat;
using Polecat.Subscriptions;

namespace CompanyName.MyMeetings.Modules.Payments.Infrastructure.AggregateStore;

public class PaymentsEventSubscription : SubscriptionBase
{
    public override async Task<IChangeListener> ProcessEventsAsync(
        EventRange page,
        ISubscriptionController controller,
        IDocumentOperations operations,
        CancellationToken cancellationToken)
    {
        using var scope = PaymentsCompositionRoot.BeginLifetimeScope();
        var projectors = scope.Resolve<IList<IProjector>>();

        foreach (var @event in page.Events)
        {
            if (@event.Data is not IDomainEvent domainEvent)
            {
                continue;
            }

            foreach (var projector in projectors)
            {
                await projector.Project(domainEvent);
            }
        }

        return null;
    }
}
