using CompanyName.MyMeetings.BuildingBlocks.Domain;
using CompanyName.MyMeetings.Modules.Payments.Domain.SeedWork;
using Polecat;
using Polecat.Events;

namespace CompanyName.MyMeetings.Modules.Payments.Infrastructure.AggregateStore;

public class PolecatAggregateStore : IAggregateStore
{
    private readonly IDocumentSession _session;
    private readonly List<IDomainEvent> _appendedChanges;
    private readonly List<AggregateToSave> _aggregatesToSave;

    public PolecatAggregateStore(IDocumentSession session)
    {
        _session = session;
        _appendedChanges = [];
        _aggregatesToSave = [];
    }

    public async Task Save()
    {
        foreach (var aggregateToSave in _aggregatesToSave)
        {
            var streamId = GetStreamId(aggregateToSave.Aggregate);
            var events = aggregateToSave.Events.Cast<object>().ToArray();

            if (aggregateToSave.Aggregate.Version < 0)
            {
                _session.Events.StartStream(streamId, events);
            }
            else
            {
                _session.Events.Append(streamId, aggregateToSave.Aggregate.Version + 1, events);
            }
        }

        await _session.SaveChangesAsync();
        _aggregatesToSave.Clear();
    }

    public async Task<T> Load<T>(AggregateId<T> aggregateId)
        where T : AggregateRoot
    {
        var streamId = GetStreamId(aggregateId);
        var stream = await _session.Events.FetchStreamAsync(streamId);
        if (stream is null || stream.Count == 0)
        {
            return null;
        }

        var domainEvents = stream
            .Select(e => e.Data)
            .OfType<IDomainEvent>()
            .ToList();

        if (domainEvents.Count == 0)
        {
            return null;
        }

        var aggregate = (T)Activator.CreateInstance(typeof(T), true);
        aggregate.Load(domainEvents);
        return aggregate;
    }

    public List<IDomainEvent> GetChanges()
    {
        return _appendedChanges;
    }

    public void AppendChanges<T>(T aggregate)
        where T : AggregateRoot
    {
        var domainEvents = aggregate.GetDomainEvents().ToList();
        _appendedChanges.AddRange(domainEvents);
        _aggregatesToSave.Add(new AggregateToSave(aggregate, domainEvents));
    }

    public void ClearChanges()
    {
        _appendedChanges.Clear();
    }

    private sealed class AggregateToSave
    {
        public AggregateToSave(AggregateRoot aggregate, List<IDomainEvent> events)
        {
            Aggregate = aggregate;
            Events = events;
        }

        public AggregateRoot Aggregate { get; }

        public List<IDomainEvent> Events { get; }
    }

    private static string GetStreamId<T>(T aggregate)
        where T : AggregateRoot
    {
        return $"{aggregate.GetType().Name}-{aggregate.Id:N}";
    }

    private static string GetStreamId<T>(AggregateId<T> aggregateId)
        where T : AggregateRoot
        => $"{typeof(T).Name}-{aggregateId.Value:N}";
}
