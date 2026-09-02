using Autofac;
using CompanyName.MyMeetings.BuildingBlocks.Application.Data;
using CompanyName.MyMeetings.BuildingBlocks.Infrastructure;
using CompanyName.MyMeetings.Modules.Payments.Application.Configuration.Projections;
using CompanyName.MyMeetings.Modules.Payments.Domain.SeedWork;
using CompanyName.MyMeetings.Modules.Payments.Infrastructure.AggregateStore;
using JasperFx.Events;
using Microsoft.Extensions.Logging;
using Polecat;
using Polecat.Serialization;

namespace CompanyName.MyMeetings.Modules.Payments.Infrastructure.Configuration.DataAccess
{
    internal class DataAccessModule : Autofac.Module
    {
        private readonly string _databaseConnectionString;
        private readonly ILoggerFactory _loggerFactory;

        internal DataAccessModule(string databaseConnectionString, ILoggerFactory loggerFactory)
        {
            _databaseConnectionString = databaseConnectionString;
            _loggerFactory = loggerFactory;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SqlConnectionFactory>()
                .As<ISqlConnectionFactory>()
                .WithParameter("connectionString", _databaseConnectionString)
                .InstancePerLifetimeScope();

            var store = DocumentStore.For(opts =>
            {
                opts.Connection(_databaseConnectionString);
                opts.DatabaseSchemaName = DatabaseSchema.Name;
                opts.Events.StreamIdentity = StreamIdentity.AsString;
                ((Serializer)opts.Serializer).Configure(PolecatSerialization.Configure);
                opts.Projections.Subscribe(new PaymentsEventSubscription(), _ => { });
                opts.Events.AddEventTypes(DomainEventTypeMappings.Dictionary.Values);
            });

            builder.RegisterInstance(store)
                .As<IDocumentStore>()
                .SingleInstance();

            builder.Register(context => context.Resolve<IDocumentStore>().LightweightSession())
                .As<IDocumentSession>()
                .InstancePerLifetimeScope();

            builder.RegisterType<PolecatAggregateStore>()
                .As<IAggregateStore>()
                .InstancePerLifetimeScope();

            var applicationAssembly = typeof(IProjector).Assembly;
            builder.RegisterAssemblyTypes(applicationAssembly)
                .Where(type => type.Name.EndsWith("Projector"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope()
                .FindConstructorsWith(new AllConstructorFinder());

            builder.RegisterType<SubscriptionsManager>()
                .As<SubscriptionsManager>()
                .SingleInstance();

            var infrastructureAssembly = ThisAssembly;

            builder.RegisterAssemblyTypes(infrastructureAssembly)
                .Where(type => type.Name.EndsWith("Repository"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope()
                .FindConstructorsWith(new AllConstructorFinder());
        }
    }
}
